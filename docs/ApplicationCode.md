# Dokumentacja kodu aplikacji — HandlujZTym

## Spis treści

1. [Architektura ogólna](#1-architektura-ogólna)
2. [Warstwa danych — modele DB](#2-warstwa-danych--modele-db)
3. [Warstwa usług — serwisy](#3-warstwa-usług--serwisy)
4. [Kontrolery użytkownika](#4-kontrolery-użytkownika)
5. [Kontrolery administratora](#5-kontrolery-administratora)
6. [ViewModels](#6-viewmodels)
7. [Kluczowe przepływy biznesowe](#7-kluczowe-przepływy-biznesowe)
8. [Uwierzytelnianie i autoryzacja](#8-uwierzytelnianie-i-autoryzacja)
9. [Obsługa plików](#9-obsługa-plików)
10. [Migracje bazy danych](#10-migracje-bazy-danych)

---

## 1. Architektura ogólna

Aplikacja jest zbudowana w oparciu o wzorzec **MVC (Model-View-Controller)** na platformie ASP.NET Core 8.

```
Przeglądarka
    │
    ▼
Kontroler (Controllers/)
    │  czyta/pisze
    ▼
Serwis (Models/System/)     ◄──── interfejsy wstrzykiwane przez DI
    │  czyta/pisze
    ▼
EF Core DbContext (MyDBContext)
    │
    ▼
PostgreSQL
```

**Dependency Injection** jest konfigurowane w `Program.cs`. Wszystkie serwisy są rejestrowane jako `Scoped` (jeden egzemplarz na żądanie HTTP). `HelperService` i `AuthService` są rejestrowane jako `Transient`.

Aplikacja nie korzysta z ASP.NET Identity — uwierzytelnianie jest w pełni własne (patrz sekcja 8).

---

## 2. Warstwa danych — modele DB

Wszystkie modele znajdują się w `Models/DbModels/`. Kontekst EF Core: `Models/MyDBContext.cs`.

### User
Główna encja użytkownika.

| Pole | Typ | Opis |
|---|---|---|
| `Id` | int | Klucz główny |
| `Username` | string | Login (unikalny) |
| `Email` | string | Adres e-mail |
| `FirstName`, `LastName` | string? | Imię i nazwisko |
| `Address` | string? | Adres tekstowy |
| `Longitude`, `Latitude` | double? | Współrzędne geograficzne (geokodowane z adresu) |
| `PhoneNumber` | string? | Telefon kontaktowy |
| `IsAdmin` | bool | Rola administratora |
| `IsBanned` | bool | Konto zablokowane |
| `IsDeleted` | bool | Konto soft-deleted |

**Powiązania:** kolekcje `Listings`, `Reviews`, `SentMessages`, `ReceivedMessages`, `TradeProposalsAsInitiator`, `TradeProposalsAsReceiver`, `Notifications`.

---

### UserAuth
Przechowuje dane uwierzytelniające osobno od encji `User`.

| Pole | Typ | Opis |
|---|---|---|
| `UserId` | int | FK do User |
| `Password` | string | Hash SHA-256 hasła + sól |
| `PasswordSalt` | string | Losowa sól (Base64, 16 bajtów) |

---

### Listing
Ogłoszenie (oferta sprzedaży lub wymiany).

| Pole | Typ | Opis |
|---|---|---|
| `Type` | `ListingType` | `Sale` (sprzedaż) lub `Trade` (wymiana) |
| `Price` | decimal? | Cena — wymagana dla Sale, null dla Trade |
| `StockQuantity` | int | Liczba dostępnych sztuk (≥ 1) |
| `IsSold` | bool | Ustawiany przez `ListingStockHelper` gdy zapas = 0 |
| `IsArchived` | bool | Soft-delete; zarchiwizowane ogłoszenia nie są widoczne w wyszukiwaniu |
| `IsFeatured` | bool | Wyróżnione ogłoszenia pojawiają się na górze listy |
| `NotExchangeable` | bool | Wyklucza ogłoszenie z propozycji wymiany |
| `MinExchangeValue` | decimal? | Minimalna szacowana wartość oferty kupującego przy wymianie |
| `ExchangeDescription` | string? | Opis oczekiwań sprzedającego przy wymianie |
| `Location` | string? | Miasto/lokalizacja |
| `ViewCount` | int | Liczba wyświetleń (nie liczy właściciela) |

**Powiązania:** `Photos` (ListingPhoto → Upload), `Tags` (ListingTag → Tag), `ExchangeAcceptedTags` (ListingExchangeAcceptedTag → Tag), `Reviews`, `ShippingOptions`.

---

### Order
Zamówienie zakupu bezpośredniego (ogłoszenie typu Sale).

| Pole | Typ | Opis |
|---|---|---|
| `ListingId` | int | FK do Listing |
| `BuyerId`, `SellerId` | int | FK do User |
| `Amount` | decimal | Łączna kwota (cena × ilość + dostawa) |
| `Quantity` | int | Liczba sztuk |
| `PayUOrderId` | string | ID zamówienia w systemie PayU |
| `Status` | `OrderStatus` | `Pending` → `Paid` / `Cancelled` |
| `SelectedShippingName` | string? | Nazwa opcji dostawy w momencie zakupu |
| `ShippingCost` | decimal | Koszt dostawy |

---

### TradeProposal
Propozycja wymiany między dwoma użytkownikami.

| Pole | Typ | Opis |
|---|---|---|
| `InitiatorUserId` | int | Strona inicjująca |
| `ReceiverUserId` | int | Strona odbierająca |
| `SubjectListingId` | int | Ogłoszenie, w kontekście którego składana jest propozycja |
| `Status` | `TradeProposalStatus` | `Pending`, `Accepted`, `Rejected`, `Cancelled`, `Superseded` |
| `ParentTradeProposalId` | int? | FK do propozycji nadrzędnej (kontrferty) |
| `RootTradeProposalId` | int? | FK do korzenia łańcucha kontrofert |

**Powiązania:** `Items` (TradeProposalItem), `Messages`, `History` (TradeProposalHistoryEntry).

---

### TradeProposalItem
Pojedynczy element po jednej stronie wymiany.

| Pole | Typ | Opis |
|---|---|---|
| `Side` | `TradeProposalSide` | `Initiator` lub `Receiver` |
| `ListingId` | int? | FK do istniejącego ogłoszenia (opcjonalne) |
| `Quantity` | int | Liczba sztuk |
| `CashAmount` | decimal? | Dopłata gotówkowa po tej stronie |
| `CustomOfferTitle` | string? | Tytuł oferty spoza listy ogłoszeń |
| `CustomOfferEstimatedValue` | decimal? | Szacowana wartość oferty spoza systemu |

---

### TradeOrder
Płatność za dopłatę gotówkową lub dostawę w ramach wymiany.

| Pole | Typ | Opis |
|---|---|---|
| `TradeProposalId` | int | FK do TradeProposal |
| `PayerSide` | `TradeProposalSide` | Kto płaci |
| `CashAmount` | decimal | Kwota dopłaty gotówkowej |
| `ShippingCost` | decimal | Koszt dostawy |
| `TotalAmount` | decimal | Suma |
| `Status` | `OrderStatus` | `Pending` / `Paid` / `Cancelled` |

---

### SellerCard
Karta płatnicza sprzedawcy zapamiętana przez tokenizację PayU.

| Pole | Typ | Opis |
|---|---|---|
| `UserId` | int | FK do User |
| `PayUCardToken` | string | Token karty do kolejnych obciążeń (recurring) |
| `MaskedNumber` | string | Zamaskowany numer karty (np. `****1234`) |
| `Brand` | string | Typ karty (Visa, Mastercard, …) |
| `ExpiryMonth`, `ExpiryYear` | int | Data ważności |
| `IsActive` | bool | Tylko jedna aktywna karta na użytkownika |

---

### SellerPayout
Rekord wypłaty dla sprzedawcy po opłaceniu zamówienia.

| Pole | Typ | Opis |
|---|---|---|
| `SellerId` | int | FK do User |
| `OrderId` | int? | FK do Order |
| `GrossAmount` | decimal | Pełna kwota zamówienia |
| `CommissionAmount` | decimal | Prowizja platformy (5%) |
| `NetAmount` | decimal | Kwota do wypłaty (95%) |
| `Status` | `SellerPayoutStatus` | `Pending`, `Paid`, `Failed`, `NoCard` |
| `PayUPayoutId` | string? | ID wypłaty w PayU |
| `ErrorMessage` | string? | Komunikat błędu przy statusie Failed/NoCard |

---

### CardTokenizationOrder
Tymczasowy rekord żądania tokenizacji karty w PayU.

| Pole | Typ | Opis |
|---|---|---|
| `UserId` | int | FK do User |
| `PayUOrderId` | string | ID zlecenia w PayU |
| `Completed` | bool | Ustawiane na `true` przez webhook po sukcesie |

---

### Message
Wiadomość prywatna między użytkownikami.

| Pole | Typ | Opis |
|---|---|---|
| `SenderId`, `ReceiverId` | int | FK do User |
| `ListingId` | int? | Kontekst ogłoszenia |
| `TicketId` | int? | Kontekst zgłoszenia |
| `TradeProposalId` | int? | Powiązana propozycja wymiany |
| `ReplyToMessageId` | int? | Cytowana wiadomość |
| `IsArchived` | bool | Soft-delete (treść zamieniana na placeholder) |

---

### Review
Opinia kupującego o ogłoszeniu/sprzedawcy.

| Pole | Typ | Opis |
|---|---|---|
| `ListingId` | int | FK do Listing |
| `ReviewerId` | int | FK do User |
| `Rating` | int | Ocena (1–5) |
| `Description` | string? | Treść recenzji |

Jeden recenzent może wystawić tylko jedną opinię danemu ogłoszeniu.

---

### Ticket
Zgłoszenie do obsługi klienta / raport moderacyjny.

| Pole | Typ | Opis |
|---|---|---|
| `UserId` | int | Autor zgłoszenia |
| `Category` | `TicketCategory` | `User_Report`, `Listing_Report`, `Other_Issue` |
| `Status` | `TicketStatus` | `Open`, `In_Progress`, `Resolved`, `Closed` |
| `ReportedUserId` | int? | Zgłaszany użytkownik |
| `ReportedListingId` | int? | Zgłaszane ogłoszenie |
| `AssigneeId` | int? | Admin odpowiedzialny za zgłoszenie |

---

### Notification
Powiadomienie systemowe dla użytkownika.

| Pole | Typ | Opis |
|---|---|---|
| `Kind` | `NotificationKind` | `NewMessage`, `ListingPurchased`, `TradeProposalReceived` |
| `MessageId` | int? | FK do Message |
| `OrderId` | int? | FK do Order |
| `TradeProposalId` | int? | FK do TradeProposal |
| `IsRead` | bool | Czy przeczytano |

---

### Upload
Metadane każdego wgranego pliku.

| Pole | Typ | Opis |
|---|---|---|
| `FileName` | string | Oryginalna nazwa pliku |
| `Extension` | string | Rozszerzenie (.jpg, .png, …) |
| `Url` | string | Ścieżka serwowana statycznie (`/uploads/<guid>.ext`) |
| `SizeBytes` | long | Rozmiar pliku |
| `UploaderId` | int | FK do User |

---

## 3. Warstwa usług — serwisy

Wszystkie serwisy są w `Models/System/`. Każdy serwis jest wstrzykiwany przez interfejs.

### AuthService
**Rejestracja:** `Transient`

Odpowiada za:
- `Validate(login, password)` — weryfikacja loginu i hasła (SHA-256 + sól)
- `GetClaims(user)` — budowanie `ClaimsIdentity` z rolą (`Admin` lub `Client`) i `NameIdentifier = user.Id`
- `HashPassword(password, salt)` — SHA-256 konkatenacji `password + salt`
- `GenerateSalt()` — 16 bajtów z `RandomNumberGenerator`, Base64
- `LogOut(httpContext)` — wylogowanie przez `SignOutAsync`

Hasła są przechowywane w tabeli `UserAuths` (oddzielnie od `Users`), co umożliwia np. reset hasła bez dotykania encji użytkownika.

---

### FileService (`IFileService`)
**Rejestracja:** `Scoped`

Odpowiada za walidację i zapis plików na dysku.

- `ValidateImages(files, maxCount, field)` — sprawdza: maksymalną liczbę plików, rozmiar (< 5 MB), rozszerzenie (.jpg/.jpeg/.png) i `ContentType`
- `ValidateAttachments(files, maxCount, field)` — sprawdza: maksymalną liczbę i rozmiar (< 50 MB)
- `SaveFileAsync(file, uploaderId)` — zapisuje plik do `wwwroot/uploads/<guid>.ext`, tworzy i zwraca encję `Upload` (bez `SaveChanges` — caller jest odpowiedzialny)
- `DeleteFile(upload)` — usuwa plik fizyczny i encję `Upload` z kontekstu

Pliki są serwowane jako statyczne przez middleware `UseStaticFiles`.

---

### NotificationService (`INotificationService`)
**Rejestracja:** `Scoped`

Tworzy rekordy `Notification` w bazie dla trzech zdarzeń:

| Metoda | Zdarzenie | Odbiorca |
|---|---|---|
| `NotifyNewMessageAsync` | Nowa wiadomość prywatna | Odbiorca wiadomości |
| `NotifyListingPurchasedAsync` | Zamówienie opłacone | Sprzedawca |
| `NotifyTradeProposalAsync` | Nowa propozycja wymiany | Odbiorca propozycji |

---

### GeocodingService (`IGeocodingService`)
**Rejestracja:** `Scoped`

Geokoduje adres tekstowy na współrzędne `(Latitude, Longitude)` korzystając z zewnętrznego API. Wynik jest zapisywany w profilu użytkownika (`UserProfile`).

---

### CardFeeService (`ICardFeeService`)
**Rejestracja:** `Scoped`

Obsługuje wszystkie operacje kart płatniczych przez PayU. Konfiguracja z kluczy `PayU:*`.

| Metoda | Opis |
|---|---|
| `CreateTokenizationOrderAsync(userId, customerIp, continueUrl)` | Tworzy zlecenie PayU z `recurring=FIRST` i `payMethods.type=CARD`. Zwraca `redirectUri` do PayU. Tworzy rekord `CardTokenizationOrder` w bazie. |
| `TryChargeListingFeeAsync(sellerUserId, listingTitle)` | Obciąża kartę 0,50 PLN (recurring STANDARD z tokenem karty). Zwraca `(Success, Error?)`. Zwraca `(false, "Brak aktywnej karty")` gdy seller nie ma karty — nie rzuca wyjątku. |
| `TryDispatchPayoutAsync(order)` | Tworzy rekord `SellerPayout`, oblicza `net = gross × 95%`, wysyła żądanie payout do PayU (`/api/v2_1/payouts`). Ustawia status `NoCard` jeśli seller nie ma karty, `Failed` przy błędzie API. |

Stałe: `CommissionRate = 0.05m` (5%), `ListingFeeAmount = 0.50m` PLN.

---

### PayuOrderSyncService (`IPayuOrderSyncService`)
**Rejestracja:** `Scoped`

Synchronizuje statusy zamówień PayU z bazą danych. Punkt wejścia dla webhooka PayU oraz strony powrotu po płatności.

| Metoda | Opis |
|---|---|
| `HandleNotifyAsync(payuOrderId, payuStatus, cardToken?, ...)` | Obsługuje webhook `/Payment/Notify`. Rozgałęzia się na: `Order` (zakup), `TradeOrder` (dopłata do wymiany), `CardTokenizationOrder` (tokenizacja karty). Przy `COMPLETED` + token → zapisuje `SellerCard`. |
| `TryFinalizeOrderFromPayuApiAsync(orderId)` | Odpytuje API PayU o status zamówienia (GET `/api/v2_1/orders/{id}`), jeśli `COMPLETED` — ustawia `Order.Status = Paid`, wywołuje `ListingStockHelper.ApplySale` i `TryDispatchPayoutAsync`. |
| `TryFinalizeTradeOrderFromPayuApiAsync(tradeOrderId)` | Analogicznie dla `TradeOrder`. |
| `SyncPendingOrdersForSellerAsync(sellerUserId)` | Synchronizuje wszystkie `Pending` zamówienia dla sprzedawcy — wywoływane przy wejściu na stronę profilu. |
| `EnsureListingPurchasedNotificationIfNeededAsync(orderId)` | Idempotentnie tworzy powiadomienie `ListingPurchased` jeśli jeszcze nie istnieje. |

---

### HelperService
**Rejestracja:** `Transient`

Narzędzia pomocnicze:
- `MakeSomeTags()` — tworzy domyślne tagi (Elektronika, Odzież, Sport, …) jeśli jeszcze nie istnieją. Wywołanie zostawione w `ListingsController.Create` (GET) — oznaczone `TODO: usunac`.
- `PopulateAvailableTags(model)` — wypełnia `model.AvailableTags` z bazy danych.

---

### ListingStockHelper
**Klasa statyczna** (nie serwis).

| Metoda | Opis |
|---|---|
| `SyncSoldFlag(listing)` | Ustawia `IsSold = (StockQuantity <= 0)` |
| `ApplySale(listing, quantity)` | Odejmuje `quantity` od `StockQuantity`, wywołuje `SyncSoldFlag` |
| `CanSell(listing, quantity)` | `true` gdy `!IsArchived && !IsSold && StockQuantity >= quantity` |
| `IsAvailableForTrade(listing)` | `true` gdy `!IsArchived && !NotExchangeable && StockQuantity > 0 && !IsSold` |

---

## 4. Kontrolery użytkownika

Wszystkie w `Controllers/User/`. Wymagają `[Authorize]` chyba że zaznaczono inaczej.

---

### ListingsController
Zarządzanie ogłoszeniami (publiczna część).

| Akcja | Metoda | URL | Opis |
|---|---|---|---|
| `Index` | GET | `/Listings` | Lista ogłoszeń z filtrowaniem i sortowaniem. Model widoku: `ListingsFilterViewModel`. Wyróżnione (`IsFeatured`) wyświetlane osobno. |
| `Details(id)` | GET | `/Listings/Details/{id}` | Szczegóły ogłoszenia. Inkrementuje `ViewCount` (z pominięciem właściciela). |
| `Create` | GET | `/Listings/Create` | Formularz nowego ogłoszenia. Wypełnia tagi. |
| `Create(model)` | POST | `/Listings/Create` | Zapisuje ogłoszenie, zdjęcia, tagi, opcje dostawy. Po zapisie wywołuje `TryChargeListingFeeAsync`. Wynik opłaty trafia do `TempData`. |
| `Edit(id)` | GET | `/Listings/Edit/{id}` | Formularz edycji. |
| `Edit(id, listing)` | POST | `/Listings/Edit/{id}` | Aktualizacja ogłoszenia. |
| `Delete(id)` | GET | `/Listings/Delete/{id}` | Potwierdzenie usunięcia. |
| `DeleteConfirmed(id)` | POST | `/Listings/Delete/{id}` | Soft-delete: `IsArchived = true`. |

**Filtry wyszukiwania** (`Index`): `searchString` (ILike na tytule i opisie), `location` (ILike), `type` (Sale/Trade), `minPrice`/`maxPrice`, `selectedTagIds` (AND logiczne), `sortBy` (newest/oldest/price_asc/price_desc/most_viewed).

---

### PaymentController
Obsługa płatności PayU dla zakupu bezpośredniego i wymiany.

| Akcja | Metoda | URL | Opis |
|---|---|---|---|
| `Buy(listingId, quantity, shippingOptionId?)` | POST | `/Payment/Buy` | Tworzy `Order`, pobiera token OAuth PayU, tworzy zlecenie PayU i przekierowuje na stronę płatności. |
| `BuyTradeOrder(tradeId, shippingOptionId?)` | POST | `/Payment/BuyTradeOrder` | Tworzy `TradeOrder` dla dopłaty do wymiany (cash + dostawa), przekierowuje na PayU. |
| `Success(orderId)` | GET | `/Payment/Success` | Strona powrotu po płatności za zakup. Wywołuje `TryFinalizeOrderFromPayuApiAsync`. |
| `TradeOrderSuccess(tradeOrderId)` | GET | `/Payment/TradeOrderSuccess` | Strona powrotu po płatności za wymianę. |
| `Notify()` | POST | `/Payment/Notify` | **Webhook PayU** (`[AllowAnonymous]`, `[IgnoreAntiforgeryToken]`). Parsuje JSON, wyciąga `orderId`, `status` i opcjonalne dane karty, deleguje do `HandleNotifyAsync`. Zawsze zwraca `200 OK`. |
| `Cancel()` | GET | `/Payment/Cancel` | Strona anulowania płatności. |

**Webhook `/Payment/Notify`** obsługuje trzy typy zleceń jednym endpointem:
- Zwykłe zamówienia (`Order.PayUOrderId`)
- Zamówienia wymiany (`TradeOrder.PayUOrderId`)
- Tokenizacje kart (`CardTokenizationOrder.PayUOrderId`)

---

### SellerCardController
Zarządzanie kartą płatniczą sprzedawcy.

| Akcja | Metoda | URL | Opis |
|---|---|---|---|
| `Index` | GET | `/SellerCard` | Pokazuje aktywną kartę i historię 50 ostatnich wypłat. |
| `RequestTokenization` | POST | `/SellerCard/RequestTokenization` | Sprawdza czy karta już istnieje, wywołuje `CreateTokenizationOrderAsync`, przekierowuje na PayU. |
| `TokenizationSuccess` | GET | `/SellerCard/TokenizationSuccess` | Strona powrotu z PayU po tokenizacji. Pokazuje czy karta już jest zapisana (webhook mógł jeszcze nie dotrzeć). |
| `RemoveCard` | POST | `/SellerCard/RemoveCard` | Dezaktywuje kartę (`IsActive = false`) — miękkie usunięcie. |

---

### TradeProposalsController
Zarządzanie propozycjami wymiany.

| Akcja | Metoda | URL | Opis |
|---|---|---|---|
| `Compose(listingId, editTradeProposalId?, parentTradeProposalId?)` | GET | `/TradeProposals/Compose` | Formularz nowej/edytowanej propozycji. Ładuje pule ogłoszeń obu stron. Obsługuje tryby: nowa propozycja, edycja istniejącej, kontroferta. |
| `Create(...)` | POST | `/TradeProposals/Create` | Tworzy lub aktualizuje propozycję. Przy kontrofercie oznacza poprzednią jako `Superseded`. Wysyła powiadomienie. Tworzy wiadomość z propozycją w wątku. |
| `Details(id)` | GET | `/TradeProposals/Details/{id}` | Szczegóły propozycji. |
| `Accept(id)` | POST | `/TradeProposals/Accept/{id}` | Akceptacja propozycji (zmiana statusu na `Accepted`). |
| `Reject(id)` | POST | `/TradeProposals/Reject/{id}` | Odrzucenie propozycji. |
| `Cancel(id)` | POST | `/TradeProposals/Cancel/{id}` | Anulowanie własnej propozycji. |
| `Index` | GET | `/TradeProposals` | Lista propozycji zalogowanego użytkownika. |

**Łańcuch kontrofert:** każda kontroferta ma `ParentTradeProposalId`. Korzeń łańcucha (`RootTradeProposalId`) pozwala wyświetlić historię negocjacji. Po złożeniu kontroferty poprzednia propozycja automatycznie przechodzi w stan `Superseded`.

**Elementy wymiany** po każdej stronie mogą zawierać:
- Istniejące ogłoszenia z puli sprzedawcy
- Dopłatę gotówkową (`CashAmount`)
- Własne oferty poza systemem (`CustomOfferTitle` + `CustomOfferEstimatedValue`)

---

### MessagesController
Wiadomości prywatne między użytkownikami.

| Akcja | Metoda | URL | Opis |
|---|---|---|---|
| `Index` | GET | `/Messages` | Lista konwersacji (ostatnia wiadomość z każdego wątku, grupowanie po parze użytkowników + kontekście). |
| `Conversation(userId, listingId?, ticketId?)` | GET | `/Messages/Conversation` | Pełny wątek wiadomości z danym użytkownikiem. Obsługuje kontekst ogłoszenia lub zgłoszenia. |
| `Send(receiverId, content, listingId?, ticketId?, photos?)` | POST | `/Messages/Send` | Wysyła wiadomość (tekst i/lub zdjęcia). Tworzy powiadomienie dla odbiorcy. |

Usunięte wiadomości (`IsArchived = true`) mają treść zamienioną na placeholder `"Ta wiadomość została usunięta."` po stronie widoku.

---

### ReviewsController
Opinie o ogłoszeniach.

| Akcja | Metoda | URL | Opis |
|---|---|---|---|
| `Create(listingId)` | GET | `/Reviews/Create?listingId=...` | Formularz recenzji. Blokuje ocenianie własnych ogłoszeń. |
| `Create(model)` | POST | `/Reviews/Create` | Zapisuje recenzję z zdjęciami. Jeden użytkownik = jedna recenzja na ogłoszenie. |
| `Details(id)` | GET | `/Reviews/Details/{id}` | Szczegóły recenzji. |
| `Edit(id)` | GET/POST | `/Reviews/Edit/{id}` | Edycja recenzji (admin). |
| `Delete(id)` | GET/POST | `/Reviews/Delete/{id}` | Usunięcie recenzji. |

---

### TicketsController
Zgłoszenia użytkownika.

| Akcja | Metoda | URL | Opis |
|---|---|---|---|
| `Index` | GET | `/Tickets` | Lista własnych zgłoszeń zalogowanego użytkownika. |
| `Create` | GET | `/Tickets/Create` | Formularz nowego zgłoszenia. |
| `Create(model)` | POST | `/Tickets/Create` | Zapisuje zgłoszenie z załącznikami. |
| `ReportUser(userId, listingId?)` | GET | `/Tickets/ReportUser` | Formularz raportu na użytkownika, wstępnie uzupełniony. |
| `ReportListing(listingId)` | GET | `/Tickets/ReportListing` | Formularz raportu na ogłoszenie. |
| `Details(id)` | GET | `/Tickets/Details/{id}` | Szczegóły zgłoszenia z załącznikami. |
| `Edit`, `Delete` | GET/POST | — | Tylko dla admina (`[Authorize(Roles="Admin")]`). |

---

### NotificationsController
Zarządzanie powiadomieniami.

Wyświetla listę powiadomień zalogowanego użytkownika, umożliwia oznaczenie jako przeczytane.

---

### UserProfilesController
Publiczny profil użytkownika (oceny, ogłoszenia, historia).

---

### ShippingLabelController
Generowanie etykiety wysyłki po opłaconej transakcji.

---

## 5. Kontrolery administratora

W `Controllers/Admin/`. Wymagają roli `Admin` (lub `Admin,Client` jak zaznaczono).

---

### ListingManageController
**Autoryzacja:** `[Authorize(Roles = "Admin,Client")]`

Zarządzanie ogłoszeniami — dostępne dla adminów (wszystkie ogłoszenia) i zwykłych użytkowników (tylko własne).

| Akcja | Metoda | Opis |
|---|---|---|
| `Index` | GET | Paginowana lista ogłoszeń z filtrami: tekst, tag, pokazuj zarchiwizowane. Admin widzi wszystkie, `Client` tylko swoje. |
| `EditListing(id)` | GET/POST | Edycja ogłoszenia: tytuł, opis, cena, tagi, opcje dostawy, zdjęcia. Admin może edytować każde, Client tylko własne. |
| `ToggleFeature(id)` | POST | Przełącza `IsFeatured`. Tylko Admin. |
| `ArchiveListing(id)` | POST | Przełącza `IsArchived`. Przy archiwizacji kaskadowo archiwizuje powiązane zgłoszenia i wiadomości. |
| `DeleteListing(id)` | POST | Soft-delete z usunięciem fizycznych zdjęć. |

---

### UserManageController
**Autoryzacja:** `[Authorize(Roles = "Admin")]`

Zarządzanie użytkownikami.

| Akcja | Metoda | Opis |
|---|---|---|
| `Index(tab, searchString, pageSize, pageNumber)` | GET | Paginowana lista użytkowników. Zakładki: `Users` (nieadmini), `Admins`. |
| `EditUser(id)` | GET/POST | Edycja danych: login, imię, email, adres, współrzędne, telefon, flagi `IsBanned`/`IsAdmin`/`IsDeleted`. |
| `DeleteUser(id)` | POST | Soft-delete: `IsDeleted = true`, archiwizuje wiadomości i ogłoszenia. |

---

### TicketsManageController
**Autoryzacja:** `[Authorize(Roles = "Admin")]`

Przegląd wszystkich zgłoszeń, zmiana statusu, przypisanie do admina.

---

## 6. ViewModels

Wszystkie w `Models/ViewModels/`.

| ViewModel | Użycie |
|---|---|
| `LoginViewModel` | Formularz logowania (Login, Password) |
| `RegisterViewModel` | Formularz rejestracji |
| `CreateListingViewModel` | Formularz tworzenia ogłoszenia (tytuł, typ, cena, tagi, zdjęcia, opcje dostawy) |
| `EditListingViewModel` | Formularz edycji ogłoszenia |
| `ListingsFilterViewModel` | Model dla widoku listy ogłoszeń — zawiera wyniki `FeaturedResults` i `Results` oraz wszystkie parametry filtrów |
| `BrowseListingsViewModel` | Pojedyncza pozycja w wynikach wyszukiwania (Listing + Seller + AverageRating + ReviewCount) |
| `ComposeTradeViewModel` | Formularz propozycji wymiany — pule ogłoszeń obu stron, kontekst edycji/kontroferty |
| `CreateReviewViewModel` | Formularz recenzji |
| `CreateTicketViewModel` | Formularz zgłoszenia |
| `EditUserViewModel` | Formularz edycji użytkownika (admin) |
| `EditMyProfileViewModel` | Formularz edycji własnego profilu |
| `PaymentViewModel` | Dane dla widoku płatności |
| `ShippingLabelViewModel` | Dane etykiety wysyłki |
| `TradeProposalsIndexViewModel` | Lista propozycji wymiany |
| `ShippingOptionInput` | Element opcji dostawy w formularzu |
| `CustomItemInput` | Element własnej oferty w formularzu wymiany |

---

## 7. Kluczowe przepływy biznesowe

### Zakup ogłoszenia (Sale)

```
Kupujący klika "Kup"
    → POST /Payment/Buy
    → Tworzy Order (Pending)
    → OAuth do PayU → Tworzy zlecenie PayU
    → Redirect na stronę płatności PayU
    → PayU wywołuje POST /Payment/Notify (webhook)
        → HandleNotifyAsync → Order.Status = Paid
        → ListingStockHelper.ApplySale (odejmuje stock)
        → TryDispatchPayoutAsync (SellerPayout + przelew 95%)
        → NotifyListingPurchasedAsync (powiadomienie dla sprzedawcy)
    → Kupujący wraca na /Payment/Success?orderId=...
        → TryFinalizeOrderFromPayuApiAsync (polling backup)
```

---

### Dodawanie karty płatniczej (Tokenizacja)

```
Sprzedawca klika "Dodaj kartę"
    → POST /SellerCard/RequestTokenization
    → CreateTokenizationOrderAsync
        → Tworzy zlecenie PayU (recurring=FIRST, type=CARD)
        → Zapisuje CardTokenizationOrder
        → Zwraca redirectUri
    → Redirect na stronę PayU (formularz karty)
    → PayU wywołuje POST /Payment/Notify (webhook)
        → HandleNotifyAsync + cardToken present
        → Dezaktywuje stare karty
        → Tworzy SellerCard (IsActive=true)
    → Sprzedawca wraca na /SellerCard/TokenizationSuccess
```

---

### Opłata za wystawienie ogłoszenia

```
POST /Listings/Create (model valid)
    → Listings.Add + SaveChangesAsync
    → TryChargeListingFeeAsync(userId, listing.Title)
        → Pobiera aktywną kartę SellerCard
        → Brak karty → (false, "Brak aktywnej karty") — nie blokuje
        → Jest karta → recurring STANDARD, 0,50 PLN
    → TempData["ListingFeeInfo"] lub TempData["ListingFeeWarning"]
    → Redirect na Details
```

---

### Propozycja wymiany

```
Kupujący klika "Zaproponuj wymianę"
    → GET /TradeProposals/Compose?listingId=...
    → Wybiera ogłoszenia z własnej puli + opcjonalne elementy spoza systemu + dopłaty
    → POST /TradeProposals/Create
        → Tworzy TradeProposal (Pending)
        → Tworzy Message z TradeProposalId w wątku
        → NotifyTradeProposalAsync (powiadomienie dla sprzedawcy)

Sprzedawca akceptuje lub składa kontrofertę:
    - Akceptacja → Status = Accepted
        → Kupujący opcjonalnie płaci dopłatę → POST /Payment/BuyTradeOrder
    - Kontroferta → Nowy TradeProposal (Parent = poprzedni)
        → Poprzedni Status = Superseded
```

---

## 8. Uwierzytelnianie i autoryzacja

Aplikacja używa **cookie authentication** (ASP.NET Core Cookies, bez Identity).

**Logowanie:**
1. `AccountController.Login` pobiera login i hasło
2. `AuthService.Validate(login, password)` weryfikuje SHA-256 z solą
3. `AuthService.GetClaims(user)` buduje `ClaimsIdentity`
4. `HttpContext.SignInAsync` wystawia zaszyfrowane cookie

**Claims w tokenie:**
- `ClaimTypes.NameIdentifier` = `user.Id.ToString()` — używany wszędzie jako identyfikator
- `ClaimTypes.Name` = `user.Username`
- `ClaimTypes.Role` = `"Admin"` lub `"Client"`

**Autoryzacja:**
- `[Authorize]` — wymaga zalogowania
- `[Authorize(Roles = "Admin")]` — tylko administratorzy
- `[Authorize(Roles = "Admin,Client")]` — każdy zalogowany użytkownik

**Przekierowanie:** niezalogowani trafiają na `/Account/Login`. Żądania do `/api/*` dostają `401` zamiast przekierowania.

---

## 9. Obsługa plików

Pliki są zapisywane na dysku lokalnym w `wwwroot/uploads/` z nazwą `<Guid>.<ext>`.

**Limity:**
- Zdjęcia: max 5 plików, max 5 MB każde, tylko `.jpg/.jpeg/.png`
- Załączniki zgłoszeń: max 10 plików, max 50 MB każde

**Ścieżka URL:** `/uploads/<guid>.ext` — serwowana przez `UseStaticFiles`.

**W Dockerze** katalog `/app/wwwroot/uploads` jest montowany jako named volume `uploads_data`, aby pliki przetrwały restarty kontenera.

---

## 10. Migracje bazy danych

Migracje są pisane **ręcznie** (nie przez `dotnet-ef migrations add`). Każda migracja ma:
- `<timestamp>_<nazwa>.cs` — metody `Up` i `Down`
- `<timestamp>_<nazwa>.Designer.cs` — snapshot modelu dla EF

Każda migracja używa `IF NOT EXISTS` w SQL-u, dzięki czemu jest idempotentna.

Migracje uruchamiają się **automatycznie przy starcie** aplikacji przez `db.Database.Migrate()` w `Program.cs`.

**Ręczne uruchomienie migracji:**
```bash
export DOTNET_ROOT=/nix/store/1blv644vinali34masnw6g5fjjjaa4y6-dotnet-sdk-8.0.416/share/dotnet
dotnet-ef database update --project ProjektZespolowyGr3/ProjektZespolowyGr3.csproj
```

**Lista migracji (chronologicznie):**

| Timestamp | Nazwa | Zawartość |
|---|---|---|
| 20251111162617 | userAuth | Tabele Users, UserAuths |
| 20251112133501 | conflict_resolve | Poprawki po mergach |
| 20251117203801 | newInit | Listings, Tags, Photos, Messages |
| 20251125135939 | Reviews | Tabela Reviews |
| 20251125143604 | Reviews2 | Poprawki Reviews |
| 20251126135003 | emailVeryfy | Weryfikacja e-mail |
| 20251210113813 | UserProfiles | Tabela UserProfiles |
| 20251210123027 | UserAnonProfile | Anonimizacja profilu |
| 20251216113037 | rename_review_and_ticket_stuffs | Rename Reviews/Tickets |
| 20251216201843 | payment | Tabele Orders, TradeOrders |
| … | … | Kolejne funkcje (TradeProposals, UserCoords, …) |
| 20260506110000 | AddSellerCardSystem | Tabele SellerCards, SellerPayouts, CardTokenizationOrders |
