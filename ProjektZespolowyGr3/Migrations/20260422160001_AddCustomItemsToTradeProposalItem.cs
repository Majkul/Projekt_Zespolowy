using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjektZespolowyGr3.Migrations
{
    public partial class AddCustomItemsToTradeProposalItem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""TradeProposalItems"" ADD COLUMN IF NOT EXISTS ""CustomOfferTitle"" text;");
            migrationBuilder.Sql(@"ALTER TABLE ""TradeProposalItems"" ADD COLUMN IF NOT EXISTS ""CustomOfferEstimatedValue"" numeric;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CustomOfferTitle", table: "TradeProposalItems");
            migrationBuilder.DropColumn(name: "CustomOfferEstimatedValue", table: "TradeProposalItems");
        }
    }
}
