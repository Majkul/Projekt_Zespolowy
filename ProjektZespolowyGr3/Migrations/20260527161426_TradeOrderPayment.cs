using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjektZespolowyGr3.Migrations
{
    /// <inheritdoc />
    public partial class TradeOrderPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TradeProposalId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TradeProposalId",
                table: "Orders",
                column: "TradeProposalId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_TradeProposals_TradeProposalId",
                table: "Orders",
                column: "TradeProposalId",
                principalTable: "TradeProposals",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_TradeProposals_TradeProposalId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_TradeProposalId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TradeProposalId",
                table: "Orders");
        }
    }
}
