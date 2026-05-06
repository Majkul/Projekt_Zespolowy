using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjektZespolowyGr3.Migrations
{
    public partial class AddSellerCardSystem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS ""SellerCards"" (
    ""Id"" serial PRIMARY KEY,
    ""UserId"" int NOT NULL REFERENCES ""Users""(""Id"") ON DELETE CASCADE,
    ""PayUCardToken"" text NOT NULL DEFAULT '',
    ""MaskedNumber"" text NOT NULL DEFAULT '',
    ""Brand"" text NOT NULL DEFAULT '',
    ""ExpiryMonth"" int NOT NULL DEFAULT 0,
    ""ExpiryYear"" int NOT NULL DEFAULT 0,
    ""IsActive"" boolean NOT NULL DEFAULT true,
    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS ""SellerPayouts"" (
    ""Id"" serial PRIMARY KEY,
    ""SellerId"" int NOT NULL REFERENCES ""Users""(""Id"") ON DELETE RESTRICT,
    ""OrderId"" int REFERENCES ""Orders""(""Id"") ON DELETE SET NULL,
    ""GrossAmount"" numeric NOT NULL DEFAULT 0,
    ""CommissionAmount"" numeric NOT NULL DEFAULT 0,
    ""NetAmount"" numeric NOT NULL DEFAULT 0,
    ""Status"" int NOT NULL DEFAULT 0,
    ""PayUPayoutId"" text,
    ""ErrorMessage"" text,
    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT now(),
    ""ProcessedAt"" timestamp with time zone
);

CREATE TABLE IF NOT EXISTS ""CardTokenizationOrders"" (
    ""Id"" serial PRIMARY KEY,
    ""UserId"" int NOT NULL REFERENCES ""Users""(""Id"") ON DELETE CASCADE,
    ""PayUOrderId"" text NOT NULL DEFAULT '',
    ""Completed"" boolean NOT NULL DEFAULT false,
    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT now()
);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TABLE IF EXISTS ""CardTokenizationOrders"";
DROP TABLE IF EXISTS ""SellerPayouts"";
DROP TABLE IF EXISTS ""SellerCards"";
            ");
        }
    }
}
