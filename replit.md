# ProjektZespolowyGr3 - HandlujZTym

A Polish classified ads / marketplace web application built with ASP.NET Core 8 MVC and PostgreSQL.

## Run & Operate

```
export DOTNET_ROOT=/nix/store/1blv644vinali34masnw6g5fjjjaa4y6-dotnet-sdk-8.0.416/share/dotnet && dotnet run --project ProjektZespolowyGr3/ProjektZespolowyGr3.csproj
```

App listens on `http://0.0.0.0:5000`. Required env vars: `PGHOST`, `PGPORT`, `PGDATABASE`, `PGUSER`, `PGPASSWORD`. PayU config keys: `PayU:BaseUrl`, `PayU:ClientId`, `PayU:ClientSecret`, `PayU:MerchantPosId`, `PayU:NotifyUrl`.

Run migrations: `export DOTNET_ROOT=... && dotnet-ef database update --project ProjektZespolowyGr3/ProjektZespolowyGr3.csproj`

## Stack

- **Framework**: ASP.NET Core 8 MVC (C#)
- **ORM**: Entity Framework Core 9 with Npgsql (PostgreSQL)
- **Database**: Replit PostgreSQL
- **Auth**: Cookie-based authentication (custom, not ASP.NET Identity)
- **Email**: MailKit (SMTP)
- **Payments**: PayU integration (orders + recurring card charges + payouts)

## Where things live

```
ProjektZespolowyGr3/
  Controllers/User/         ListingsController, PaymentController, SellerCardController, …
  Controllers/Admin/        ListingManageController, …
  Models/DbModels/          EF entity models (Listing, Order, SellerCard, SellerPayout, …)
  Models/System/            Services: CardFeeService, PayuOrderSyncService, AuthService, …
  Models/ViewModels/        View-specific models (ListingsFilterViewModel, …)
  Views/                    Razor views
  Migrations/               EF Core migrations (hand-written .cs + .Designer.cs)
  Program.cs                App entry point & DI registrations
  appsettings.json          Email/SMTP config
```

## Architecture decisions

- Migrations are hand-written with raw `IF NOT EXISTS` SQL — no auto-generated `dotnet-ef migrations add`.
- Cookie auth is fully custom (no ASP.NET Identity), using `ClaimTypes.NameIdentifier` for user ID.
- `IPayuOrderSyncService.HandleNotifyAsync` now accepts optional card token params so the PayU Notify webhook handles both regular orders and card tokenization in one endpoint.
- Listing fee (0.50 PLN) is charged via `ICardFeeService.TryChargeListingFeeAsync` immediately after `SaveChangesAsync` in `ListingsController.Create`; failure is non-blocking (shown as a warning).
- Payout (95% of sale) is dispatched via `ICardFeeService.TryDispatchPayoutAsync` from `PayuOrderSyncService` when order status becomes Paid.
- `PendingModelChangesWarning` is suppressed in `Program.cs` to tolerate snapshot drift.

## Product

- Browse & post listings (sale or trade), with photos, tags, location, shipping options
- Register, log in, manage profile (including geocoded address)
- Leave reviews on sellers; message sellers directly
- Trade proposals with custom/unlisted exchange items
- Create support tickets
- **Credit card system**: sellers attach a card via PayU tokenization, are charged 0.50 PLN per listing creation, receive 95% of sale as automatic payout after buyer pays
- Admin panel: manage users, listings (archive/restore/feature), view reports

## User preferences

_None recorded yet._

## Gotchas

- `DOTNET_ROOT` must be set to the exact Nix store path for EF tools and `dotnet run` to work.
- Forward headers are configured for Replit proxy (mTLS); HTTPS redirect is disabled.
- `ListingsController` uses `ListingsFilterViewModel` (not `IEnumerable<BrowseListingsViewModel>`) — the Index view depends on this type.
- `CardFeeService` lives in `ProjektZespolowyGr3.Models.System` namespace — use `global::System.Net.HttpStatusCode` to avoid collision.

## Pointers

- Payment flow: `Controllers/User/PaymentController.cs` + `Models/System/PayuOrderSyncService.cs`
- Card fee flow: `Models/System/ICardFeeService.cs` + `Models/System/CardFeeService.cs`
- New DB models: `Models/DbModels/SellerCard.cs`, `SellerPayout.cs`, `CardTokenizationOrder.cs`
- Migration: `Migrations/20260506110000_AddSellerCardSystem.cs`
