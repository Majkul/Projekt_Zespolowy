# ProjektZespolowyGr3.Tests

Projekt testów jednostkowych dla aplikacji ProjektZespolowyGr3.

## Struktura testów

### Services
- **AuthServiceTests** - Testy serwisu autentykacji:
  - Hashowanie haseł
  - Generowanie soli
  - Walidacja użytkowników
  - Sprawdzanie istnienia użytkowników i emaili
  - Tworzenie claims

- **HelperServiceTests** - Testy serwisu pomocniczego:
  - Tworzenie tagów
  - Populacja tagów w ViewModelach

- **EmailServiceTests** - Testy serwisu email:
  - Obsługa brakującej konfiguracji
  - Obsługa nieprawidłowego portu

### Controllers
- **HomeControllerTests** - Testy kontrolera głównego:
  - Strona główna z listą ofert
  - Logowanie (GET/POST)
  - Rejestracja (GET/POST)
  - Walidacja formularzy
  - Sprawdzanie duplikatów użytkowników i emaili

- **MessagesControllerTests** - Testy kontrolera wiadomości:
  - Lista konwersacji
  - Wyświetlanie rozmowy
  - Wysyłanie wiadomości
  - Obsługa nieistniejących użytkowników

- **TicketsControllerTests** - Testy kontrolera zgłoszeń:
  - Lista zgłoszeń użytkownika (tylko własne)
  - Zgłaszanie użytkownika
  - Zgłaszanie oferty
  - Filtrowanie zgłoszeń

- **ReviewsControllerTests** - Testy kontrolera opinii:
  - Tworzenie opinii
  - Walidacja (nie można ocenić własnej oferty)
  - Tworzenie opinii z listingId

- **ListingsControllerTests** - Testy kontrolera ofert:
  - Lista ofert
  - Wyszukiwanie
  - Szczegóły oferty
  - Obsługa nieistniejących ofert

- **UserProfileControllerTests** - Testy kontrolera profilu użytkownika:
  - Wyświetlanie profilu
  - Obliczanie średniej oceny
  - Obsługa nieistniejących użytkowników

- **MyProfileControllerTests** - Testy kontrolera edycji własnego profilu:
  - Edycja profilu (GET/POST)
  - Walidacja emaila (zapobieganie duplikatom)
  - Przekierowanie do profilu publicznego

### Models
- **MessageTests** - Testy modelu wiadomości:
  - Właściwości wymagane
  - Opcjonalne powiązania z Listing i Ticket

- **TicketTests** - Testy modelu zgłoszenia:
  - Właściwości wymagane
  - Opcjonalne powiązania (Assignee, ReportedUser, ReportedListing)

- **ListingTests** - Testy modelu oferty:
  - Właściwości wymagane
  - Obsługa typu Trade (bez ceny)
  - Opcjonalny opis

### ViewModels
- **RegisterViewModelTests** - Testy walidacji ViewModel rejestracji:
  - Wymagane pola
  - Walidacja emaila
  - Poprawność danych

### Integration
- **UserRegistrationFlowTests** - Testy integracyjne przepływu rejestracji:
  - Kompletny przepływ rejestracji
  - Zapobieganie duplikatom użytkowników
  - Zapobieganie duplikatom emaili

### EdgeCases
- **EdgeCaseTests** - Testy przypadków brzegowych:
  - Puste hasła
  - Bardzo długie hasła
  - Znaki specjalne w hasłach
  - Null/empty wartości
  - Obsługa błędów

## Uruchamianie testów

```bash
dotnet test
```

## Uruchamianie testów z pokryciem kodu

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## Technologie

- **xUnit** - Framework testowy
- **Moq** - Mockowanie zależności
- **FluentAssertions** - Asercje czytelne
- **Entity Framework Core InMemory** - Testowa baza danych w pamięci

