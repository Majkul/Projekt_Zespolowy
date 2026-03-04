# ProjektZespolowyGr3 - HandlujZTym

A Polish classified ads / marketplace web application built with ASP.NET Core 8 MVC and PostgreSQL.

## Project Overview

**HandlujZTym** ("Trade With This") is a marketplace platform where users can:
- Browse and post listings/offers
- Register, log in, and manage their profile
- Leave reviews on sellers
- Message sellers directly
- Create support tickets
- Make payments (PayU integration)
- Admin panel for user and listing management

## Tech Stack

- **Framework**: ASP.NET Core 8 MVC (C#)
- **ORM**: Entity Framework Core 9 with Npgsql (PostgreSQL)
- **Database**: Replit PostgreSQL (accessed via PGHOST, PGPORT, PGUSER, PGPASSWORD, PGDATABASE env vars)
- **Auth**: Cookie-based authentication (custom, not ASP.NET Identity)
- **Email**: MailKit (SMTP)
- **Payments**: PayU integration

## Project Structure

```
ProjektZespolowyGr3/
  Controllers/         # MVC controllers
    Admin/             # Admin-only controllers
    User/              # User-specific controllers
  Models/
    DbModels/          # Entity Framework models
    System/            # Services (Auth, Email, Helper)
    ViewModels/        # View-specific models
  Views/               # Razor views
  Migrations/          # EF Core migrations
  Program.cs           # App entry point
  appsettings.json     # App config (email settings)
```

## Running the App

The workflow command is:
```
export DOTNET_ROOT=/nix/store/1blv644vinali34masnw6g5fjjjaa4y6-dotnet-sdk-8.0.416/share/dotnet && dotnet run --project ProjektZespolowyGr3/ProjektZespolowyGr3.csproj
```

App listens on `http://0.0.0.0:5000`.

## Database

- Uses Replit's built-in PostgreSQL
- Connection string is built from env vars in `Program.cs`
- Migrations are managed with EF Core (`dotnet-ef` tool)
- Run migrations: `export DOTNET_ROOT=... && dotnet-ef database update --project ProjektZespolowyGr3/ProjektZespolowyGr3.csproj`

## Configuration

- `appsettings.json`: Email/SMTP settings (fill in for email functionality)
- Database connection is constructed from `PGHOST`, `PGPORT`, `PGDATABASE`, `PGUSER`, `PGPASSWORD` environment variables

## Notes

- The `DOTNET_ROOT` must be set to the actual .NET 8 SDK path for EF tools and deployment to work
- Forward headers are configured for Replit proxy support
- HTTPS redirect is disabled in development (Replit handles TLS at proxy level)
