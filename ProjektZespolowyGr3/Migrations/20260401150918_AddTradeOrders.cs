using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ProjektZespolowyGr3.Migrations
{
    /// <inheritdoc />
    public partial class AddTradeOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS ""TradeOrders"" (
    ""Id"" serial PRIMARY KEY,
    ""TradeProposalId"" integer NOT NULL,
    ""PayerUserId"" integer NOT NULL,
    ""ReceiverUserId"" integer NOT NULL,
    ""PayerSide"" integer NOT NULL,
    ""CashAmount"" numeric NOT NULL DEFAULT 0,
    ""ShippingCost"" numeric NOT NULL DEFAULT 0,
    ""TotalAmount"" numeric NOT NULL DEFAULT 0,
    ""SelectedShippingName"" character varying(100) NULL,
    ""PayUOrderId"" text NOT NULL DEFAULT '',
    ""Status"" integer NOT NULL DEFAULT 0,
    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT ""FK_TradeOrders_TradeProposals_TradeProposalId""
        FOREIGN KEY (""TradeProposalId"")
        REFERENCES ""TradeProposals"" (""Id"")
        ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS ""IX_TradeOrders_TradeProposalId"" ON ""TradeOrders"" (""TradeProposalId"");
");

            migrationBuilder.AlterColumn<decimal>(
                name: "ShippingCost",
                table: "Orders",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldDefaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""TradeOrders"";");

            migrationBuilder.AlterColumn<decimal>(
                name: "ShippingCost",
                table: "Orders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric");
        }
    }
}
