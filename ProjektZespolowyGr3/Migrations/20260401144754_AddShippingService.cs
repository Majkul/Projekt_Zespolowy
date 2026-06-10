using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ProjektZespolowyGr3.Migrations
{
    /// <inheritdoc />
    public partial class AddShippingService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""ListingShippingOptions"" (
                    ""Id"" serial NOT NULL PRIMARY KEY,
                    ""ListingId"" integer NOT NULL,
                    ""Name"" character varying(100) NOT NULL,
                    ""Price"" numeric NOT NULL DEFAULT 0,
                    CONSTRAINT ""FK_ListingShippingOptions_Listings_ListingId""
                        FOREIGN KEY (""ListingId"") REFERENCES ""Listings""(""Id"") ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS ""IX_ListingShippingOptions_ListingId""
                    ON ""ListingShippingOptions""(""ListingId"");

                ALTER TABLE ""Orders""
                    ADD COLUMN IF NOT EXISTS ""SelectedShippingName"" character varying(100) NULL;
                ALTER TABLE ""Orders""
                    ADD COLUMN IF NOT EXISTS ""ShippingCost"" numeric NOT NULL DEFAULT 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS ""ListingShippingOptions"";
                ALTER TABLE ""Orders"" DROP COLUMN IF EXISTS ""SelectedShippingName"";
                ALTER TABLE ""Orders"" DROP COLUMN IF EXISTS ""ShippingCost"";
            ");
        }
    }
}
