using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjektZespolowyGr3.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── pg_trgm extension for ILIKE trigram search ────────────────────────────
            migrationBuilder.Sql(@"CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            // ── Listings (most heavily queried table) ─────────────────────────────────

            // Default browse sort: ORDER BY CreatedAt DESC
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Listings_CreatedAt""
                ON ""Listings"" (""CreatedAt"" DESC);");

            // Most-viewed sort: ORDER BY ViewCount DESC
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Listings_ViewCount""
                ON ""Listings"" (""ViewCount"" DESC);");

            // Price range filter and price sort
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Listings_Price""
                ON ""Listings"" (""Price"" ASC NULLS LAST);");

            // Trade pool ordering: ORDER BY UpdatedAt DESC
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Listings_UpdatedAt""
                ON ""Listings"" (""UpdatedAt"" DESC);");

            // Sale/Trade type filter
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Listings_Type""
                ON ""Listings"" (""Type"");");

            // Location ILIKE filter
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Listings_Location""
                ON ""Listings"" (""Location"");");

            // Partial index covering the exact WHERE clause used by public browse
            // (.Where(l => !l.IsArchived && !l.IsSold && !l.IsPrivate && l.StockQuantity > 0))
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Listings_PublicBrowse_CreatedAt""
                ON ""Listings"" (""CreatedAt"" DESC)
                WHERE ""IsArchived"" = false AND ""IsSold"" = false AND ""IsPrivate"" = false AND ""StockQuantity"" > 0;");

            // Partial index for the homepage featured query
            // (.Where(l => l.IsFeatured && !l.IsSold && !l.IsArchived))
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Listings_Featured_CreatedAt""
                ON ""Listings"" (""CreatedAt"" DESC)
                WHERE ""IsFeatured"" = true AND ""IsSold"" = false AND ""IsArchived"" = false;");

            // GIN trigram indexes for ILIKE full-text search on Title and Description
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Listings_Title_Trgm""
                ON ""Listings"" USING GIN (""Title"" gin_trgm_ops);");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Listings_Description_Trgm""
                ON ""Listings"" USING GIN (""Description"" gin_trgm_ops);");

            // ── Orders (no BuyerId/SellerId/Status indexes existed) ───────────────────

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Orders_BuyerId""
                ON ""Orders"" (""BuyerId"");");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Orders_SellerId""
                ON ""Orders"" (""SellerId"");");

            // Status filter: PayuOrderSyncService queries WHERE Status = Paid
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Orders_Status""
                ON ""Orders"" (""Status"");");

            // Ordering by CreatedAt DESC
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Orders_CreatedAt""
                ON ""Orders"" (""CreatedAt"" DESC);");

            // PayU webhook lookup: WHERE PayUOrderId = @id
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Orders_PayUOrderId""
                ON ""Orders"" (""PayUOrderId"");");

            // ── Messages ──────────────────────────────────────────────────────────────

            // Conversation ordering: ORDER BY SentAt ASC/DESC
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Messages_SentAt""
                ON ""Messages"" (""SentAt"" DESC);");

            // ── Notifications: queried on every page render ───────────────────────────

            // Compound index: WHERE UserId = @id AND IsRead = false (unread count component)
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Notifications_UserId_IsRead""
                ON ""Notifications"" (""UserId"", ""IsRead"");");

            // ── TradeProposals ────────────────────────────────────────────────────────

            // Status filter: Pending proposals
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_TradeProposals_Status""
                ON ""TradeProposals"" (""Status"");");

            // Ordering for listing pool: ORDER BY UpdatedAt DESC
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_TradeProposals_UpdatedAt""
                ON ""TradeProposals"" (""UpdatedAt"" DESC);");

            // ── Users: auth and registration lookups ──────────────────────────────────

            // Login lookup: WHERE Username = @login (unique)
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Users_Username""
                ON ""Users"" (""Username"");");

            // Registration check: WHERE Email = @email (unique)
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Users_Email""
                ON ""Users"" (""Email"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Listings_CreatedAt"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Listings_ViewCount"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Listings_Price"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Listings_UpdatedAt"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Listings_Type"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Listings_Location"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Listings_PublicBrowse_CreatedAt"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Listings_Featured_CreatedAt"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Listings_Title_Trgm"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Listings_Description_Trgm"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Orders_BuyerId"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Orders_SellerId"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Orders_Status"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Orders_CreatedAt"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Orders_PayUOrderId"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Messages_SentAt"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Notifications_UserId_IsRead"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_TradeProposals_Status"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_TradeProposals_UpdatedAt"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Users_Username"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Users_Email"";");
        }
    }
}
