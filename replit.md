# ProjektZespolowyGr3 - HandlujZTym

A Polish classified ads / marketplace web application built with ASP.NET Core 8 MVC and PostgreSQL.

## Run & Operate

**Replit (development):**
```
export DOTNET_ROOT=/nix/store/1blv644vinali34masnw6g5fjjjaa4y6-dotnet-sdk-8.0.416/share/dotnet && dotnet run --project ProjektZespolowyGr3/ProjektZespolowyGr3.csproj
```

**Docker (production):**
```bash
cp docker/.env.example docker/.env   # fill in all values
cd docker && docker compose up -d
```

App listens on `http://0.0.0.0:5000`. Required env vars: `PGHOST`, `PGPORT`, `PGDATABASE`, `PGUSER`, `PGPASSWORD`. PayU config keys: `PayU__BaseUrl`, `PayU__ClientId`, `PayU__ClientSecret`, `PayU__MerchantPosId`, `PayU__NotifyUrl`. Email: `EmailSettings__SmtpServer`, `EmailSettings__Port`, `EmailSettings__SenderEmail`, `EmailSettings__SenderName`, `EmailSettings__Password`.

Run migrations: `export DOTNET_ROOT=... && dotnet-ef database update --project ProjektZespolowyGr3/ProjektZespolowyGr3.csproj`

## Stack

- **Framework**: ASP.NET Core 8 MVC (C#)
- **ORM**: Entity Framework Core 9 with Npgsql (PostgreSQL)
- **Database**: Replit PostgreSQL (dev) / containerised PostgreSQL 16-alpine (prod)
- **Auth**: Cookie-based authentication (custom, not ASP.NET Identity)
- **Email**: MailKit (SMTP)
- **Payments**: PayU integration (orders + recurring card charges + payouts)
- **Container**: Docker multi-stage build (sdk:8.0 → aspnet:8.0) + Nginx reverse proxy

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
  appsettings.json          Base config (overridden by appsettings.Production.json in container)
docker/
  docker-compose.yaml       Services: app, nginx, pgdatabase, pgadmin
  .env.example              Template — copy to .env and fill in secrets (never commit .env)
  nginx/nginx.conf          HTTPS reverse proxy config (TLS + security headers)
  nginx/nginx-http.conf     HTTP-only config for local/staging without certs
  nginx/certs/              TLS certificate files (fullchain.pem + privkey.pem) — gitignored
docs/
  ApplicationCode.md        Full code documentation (architecture, models, services, controllers)
  Diagrams.md               15 Mermaid diagrams (ERD, state machines, sequence diagrams)
Dockerfile                  Multi-stage Docker build
.dockerignore               Docker build context exclusions
```

## Architecture decisions

- Migrations are hand-written with raw `IF NOT EXISTS` SQL — no auto-generated `dotnet-ef migrations add`.
- Cookie auth is fully custom (no ASP.NET Identity), using `ClaimTypes.NameIdentifier` for user ID.
- `IPayuOrderSyncService.HandleNotifyAsync` now accepts optional card token params so the PayU Notify webhook handles both regular orders and card tokenization in one endpoint.
- Listing fee (0.50 PLN) is charged via `ICardFeeService.TryChargeListingFeeAsync` immediately after `SaveChangesAsync` in `ListingsController.Create`; failure is non-blocking (shown as a warning).
- Payout (95% of sale) is dispatched via `ICardFeeService.TryDispatchPayoutAsync` from `PayuOrderSyncService` when order status becomes Paid.
- `PendingModelChangesWarning` is suppressed in `Program.cs` to tolerate snapshot drift.
- Docker app container runs as non-root user (`appuser`) for security.
- Nginx sits in front of the app container: handles TLS termination, sets `X-Forwarded-*` headers (consumed by `UseForwardedHeaders`), caches `/uploads/` responses for 7 days.

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
- Forward headers are configured for Replit proxy (mTLS); HTTPS redirect is disabled in app — Nginx handles it.
- `ListingsController` uses `ListingsFilterViewModel` (not `IEnumerable<BrowseListingsViewModel>`) — the Index view depends on this type.
- `CardFeeService` lives in `ProjektZespolowyGr3.Models.System` namespace — use `global::System.Net.HttpStatusCode` to avoid collision.
- Docker env vars use `__` as separator for nested config keys (e.g. `PayU__BaseUrl` maps to `PayU:BaseUrl`).
- For local Docker without TLS certs, swap `nginx/nginx.conf` for `nginx/nginx-http.conf` in `docker-compose.yaml`.
- `uploads_data` volume persists user-uploaded files across container restarts.

## Pointers

- Payment flow: `Controllers/User/PaymentController.cs` + `Models/System/PayuOrderSyncService.cs`
- Card fee flow: `Models/System/ICardFeeService.cs` + `Models/System/CardFeeService.cs`
- New DB models: `Models/DbModels/SellerCard.cs`, `SellerPayout.cs`, `CardTokenizationOrder.cs`
- Migration: `Migrations/20260506110000_AddSellerCardSystem.cs`
- Docker env template: `docker/.env.example`
