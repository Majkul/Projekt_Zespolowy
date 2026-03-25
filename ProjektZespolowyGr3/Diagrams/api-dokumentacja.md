# Dokumentacja interfejsu HTTP (stan na marzec 2025)

Aplikacja jest **aplikacją MVC** (widoki HTML, formularze, przekierowania), a nie typowym **REST API JSON**. Poniższa lista opisuje **obsługiwane żądania HTTP** — ścieżki według domyślnego routingu `{controller}/{action}/{id?}` oraz tras z atrybutu `[Route]`.

**Bazowy wzorzec routingu** (z `Program.cs`): `/{ControllerNamebez„Controller"}/{Action}/{opcjonalny id}`

Legenda uwierzytelniania:

| Oznaczenie | Znaczenie |
|------------|-----------|
| (anonimowe) | bez `[Authorize]` |
| Zalogowany | `[Authorize]` — cookie |
| Admin | rola `Admin` |
| Admin lub klient | role `Admin` lub `Client` (np. zarządzanie ogłoszeniami) |


## HomeController (`/`)

| Metoda | Ścieżka | Akcja | Uwierzytelnianie | Opis |
|--------|---------|--------|------------------|------|
| GET | `/` lub `/Home` | `Index` | — | Strona główna |
| GET | `/Home/Privacy` | `Privacy` | — | Polityka prywatności |
| GET/POST | `/Account/Login` | `Login` | — | Logowanie (`returnUrl` opcjonalnie) |
| GET | `/Account/Register` | `Register` | — | Formularz rejestracji |
| POST | `/Account/Register` | `Register` | — | Rejestracja (antiforgery, body: model rejestracji) |
| GET | `/Account/VerifyEmail` | `VerifyEmail` | — | Weryfikacja e-mail (`email`, `token`) |
| GET | `/Account/RegisterConfirmation` | `RegisterConfirmation` | — | Potwierdzenie rejestracji |
| GET | `/Account/CompleteProfile` | `CompleteProfile` | Zalogowany | Uzupełnienie profilu po rejestracji |
| POST | `/Account/CompleteProfile` | `CompleteProfile` | Zalogowany | Zapis profilu |
| POST | `/Account/Logout` | `Logout` | — | Wylogowanie (antiforgery) |
| GET | `/Home/Error` | `Error` | — | Strona błędu |

> Trasy `/Account/...` są mapowane na akcje w **HomeController** przez `[Route("Account/[action]")]`.

---

## ListingsController (`/Listings`)

| Metoda | Ścieżka | Uwierzytelnianie | Opis |
|--------|---------|------------------|------|
| GET | `/Listings`, `/Listings/Index` | — | Lista ogłoszeń (`searchString` opcjonalnie) |
| GET | `/Listings/Details/{id}` | — | Szczegóły ogłoszenia |
| GET | `/Listings/Create` | Zalogowany | Formularz nowego ogłoszenia |
| POST | `/Listings/Create` | Zalogowany | Utworzenie ogłoszenia |
| GET | `/Listings/Edit/{id}` | Zalogowany | Edycja |
| POST | `/Listings/Edit/{id}` | Zalogowany | Zapis edycji |
| GET | `/Listings/Delete/{id}` | Zalogowany | Potwierdzenie usunięcia |
| POST | `/Listings/Delete/{id}` | Zalogowany | Usunięcie (`DeleteConfirmed`) |

---

## UserProfileController (`/UserProfile`)

| Metoda | Ścieżka | Uwierzytelnianie | Opis |
|--------|---------|------------------|------|
| GET | `/UserProfile/Details/{id}` | — | Publiczny profil użytkownika i jego oferty |

---

## MyProfileController (`/MyProfile`)

| Metoda | Ścieżka | Uwierzytelnianie | Opis |
|--------|---------|------------------|------|
| GET | `/MyProfile/Edit` | Zalogowany | Edycja własnego profilu |
| POST | `/MyProfile/Edit` | Zalogowany | Zapis (antiforgery) |
| GET | `/MyProfile/Details` | Zalogowany | Przekierowanie do `/UserProfile/Details/{mojeId}` |

---

## MessagesController (`/Messages`)

| Metoda | Ścieżka | Uwierzytelnianie | Opis |
|--------|---------|------------------|------|
| GET | `/Messages` | Zalogowany | Lista konwersacji |
| GET | `/Messages/Conversation` | Zalogowany | Wątek (`userId`, opcjonalnie `listingId`, `ticketId`) |
| POST | `/Messages/Send` | Zalogowany | Wysłanie wiadomości (`receiverId`, `content`, opcjonalnie `listingId`, `ticketId`; antiforgery) |

---

## NotificationsController (`/Notifications`)

| Metoda | Ścieżka | Uwierzytelnianie | Opis |
|--------|---------|------------------|------|
| GET | `/Notifications`, `/Notifications/Index` | Zalogowany | Lista powiadomień (sync zamówień PayU dla sprzedawcy) |
| GET | `/Notifications/Go/{id}` | Zalogowany | Oznaczenie jako przeczytane + przekierowanie (wiadomość / ogłoszenie / wymiana) |
| POST | `/Notifications/MarkAllRead` | Zalogowany | Wszystkie jako przeczytane (antiforgery) |

---

## TradeProposalsController (`/TradeProposals`)

| Metoda | Ścieżka | Uwierzytelnianie | Opis |
|--------|---------|------------------|------|
| GET | `/TradeProposals/Compose` | Zalogowany | Składanie/edycja wymiany (`listingId`, opcjonalnie `editTradeProposalId`, `parentTradeProposalId`) |
| POST | `/TradeProposals/Create` | Zalogowany | Utworzenie / edycja propozycji (antiforgery; formularz z listami ID i ilościami) |
| POST | `/TradeProposals/Accept/{id}` | Zalogowany | Akceptacja przez odbiorcę (antiforgery) |
| POST | `/TradeProposals/Reject/{id}` | Zalogowany | Odrzucenie (antiforgery) |
| POST | `/TradeProposals/Cancel/{id}` | Zalogowany | Anulowanie przez inicjatora (antiforgery) |
| GET | `/TradeProposals` | Zalogowany | Indeks propozycji |
| GET | `/TradeProposals/Details/{id}` | Zalogowany | Szczegóły + wątek kontrofert |

---

## PaymentController (`/Payment`)

| Metoda | Ścieżka | Uwierzytelnianie | Opis |
|--------|---------|------------------|------|
| POST | `/Payment/Buy` | Zalogowany | Zakup (`listingId`, `quantity`) — przekierowanie do PayU |
| POST | `/Payment/Notify` | **anonimowe** | Webhook PayU (`IgnoreAntiforgeryToken`) — treść JSON w body |
| GET | `/Payment/Success` | Zalogowany | Po płatności (`orderId`) — finalizacja zamówienia |
| GET | `/Payment/Cancel` | Zalogowany | Anulowanie płatności (widok) |

---

## ReviewsController (`/Reviews`)

| Metoda | Ścieżka | Uwierzytelnianie | Opis |
|--------|---------|------------------|------|
| GET | `/Reviews` | Zalogowany | Lista recenzji użytkownika |
| GET | `/Reviews/Details/{id}` | Zalogowany | Szczegóły recenzji |
| GET | `/Reviews/Create` | Zalogowany | Formularz ogólny |
| GET | `/Reviews/Create` | Zalogowany | Formularz dla ogłoszenia (`listingId`) |
| POST | `/Reviews/Create` | Zalogowany | Utworzenie (antiforgery) |
| GET | `/Reviews/Edit/{id}` | Zalogowany | Edycja |
| POST | `/Reviews/Edit/{id}` | Zalogowany | Zapis (antiforgery) |
| GET | `/Reviews/Delete/{id}` | Zalogowany | Potwierdzenie usunięcia |
| POST | `/Reviews/Delete/{id}` | Zalogowany | Usunięcie (`DeleteConfirmed`) |

---

## TicketsController (`/Tickets`)

| Metoda | Ścieżka | Uwierzytelnianie | Opis |
|--------|---------|------------------|------|
| GET | `/Tickets` | Zalogowany | Zgłoszenia użytkownika |
| GET | `/Tickets/Details/{id}` | Zalogowany | Szczegóły zgłoszenia |
| GET | `/Tickets/ReportUser` | Zalogowany | Zgłoszenie użytkownika (`userId`, opcjonalnie `listingId`) |
| GET | `/Tickets/ReportListing/{listingId}` | Zalogowany | Zgłoszenie ogłoszenia |
| GET | `/Tickets/Create` | Zalogowany | Nowe zgłoszenie |
| POST | `/Tickets/Create` | Zalogowany | Utworzenie (antiforgery) |
| GET | `/Tickets/Edit/{id}` | **Admin** | Edycja (panel administracyjny) |
| POST | `/Tickets/Edit/{id}` | **Admin** | Zapis |
| GET | `/Tickets/Delete/{id}` | **Admin** | Potwierdzenie usunięcia |
| POST | `/Tickets/Delete/{id}` | **Admin** | Usunięcie (`DeleteConfirmed`) |

---

## Administracja

Kontrolery w `Controllers/Admin/` — w routingu **nie ma** prefiksu `Admin/` w URL; nazwa kontrolera to pierwszy segment ścieżki.

### UserManageController (`/UserManage`) — tylko Admin

| Metoda | Ścieżka | Opis |
|--------|---------|------|
| GET | `/UserManage/Index` | Lista użytkowników (`tab`, `searchString`, stronicowanie) |
| GET | `/UserManage/EditUser/{id}` | Edycja użytkownika |
| POST | `/UserManage/EditUser/{id}` | Zapis |
| POST | `/UserManage/DeleteUser/{id}` | Usunięcie |

### ListingManageController (`/ListingManage`) — Admin lub Client

| Metoda | Ścieżka | Opis |
|--------|---------|------|
| GET | `/ListingManage/Index` | Lista ogłoszeń (admin: wszystkie; klient: tylko swoje) |
| GET | `/ListingManage/EditListing/{id}` | Edycja |
| POST | `/ListingManage/EditListing/{id}` | Zapis |
| POST | `/ListingManage/DeleteListing/{id}` | Usunięcie |

### TicketsManageController (`/TicketsManage`) — tylko Admin

| Metoda | Ścieżka | Opis |
|--------|---------|------|
| GET | `/TicketsManage/Index` | Panel zgłoszeń (filtry, stronicowanie) |
| GET | `/TicketsManage/Details/{id}` | Szczegóły |
| POST | `/TicketsManage/UpdateStatus` | Zmiana statusu |
| POST | `/TicketsManage/Assign` | Przypisanie do admina |

---

## View Component (nie jest osobnym URL)

| Nazwa | Wywołanie | Opis |
|-------|-----------|------|
| `UnreadNotifications` | `InvokeAsync` z layoutu | Liczba nieprzeczytanych powiadomień dla zalogowanego użytkownika |

---

## Uwagi techniczne

1. **Format odpowiedzi**: dominują `ViewResult` i `RedirectToAction`/`Redirect`; wyjątki to m.in. `BadRequest`, `NotFound`, `Forbid`, `Unauthorized`, oraz `Redirect` (zewnętrzny PayU) przy `/Payment/Buy`.
2. **Antiforgery**: większość akcji POST ma `[ValidateAntiForgeryToken]` — wymaga tokenu z formularza (nie dotyczy np. `/Payment/Notify`).
3. **Brak publicznego OpenAPI**: projekt nie używa Swaggersa; niniejszy plik jest **ręczną dokumentacją** zgodną z kodem kontrolerów.

---

