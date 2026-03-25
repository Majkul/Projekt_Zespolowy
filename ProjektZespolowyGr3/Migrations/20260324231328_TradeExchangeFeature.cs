using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ProjektZespolowyGr3.Migrations
{
    /// <inheritdoc />
    public partial class TradeExchangeFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReplyToMessageId",
                table: "Messages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TradeProposalId",
                table: "Messages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExchangeDescription",
                table: "Listings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinExchangeValue",
                table: "Listings",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotExchangeable",
                table: "Listings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ListingExchangeAcceptedTags",
                columns: table => new
                {
                    ListingId = table.Column<int>(type: "integer", nullable: false),
                    TagId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingExchangeAcceptedTags", x => new { x.ListingId, x.TagId });
                    table.ForeignKey(
                        name: "FK_ListingExchangeAcceptedTags_Listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ListingExchangeAcceptedTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TradeProposals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InitiatorUserId = table.Column<int>(type: "integer", nullable: false),
                    ReceiverUserId = table.Column<int>(type: "integer", nullable: false),
                    SubjectListingId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ParentTradeProposalId = table.Column<int>(type: "integer", nullable: true),
                    RootTradeProposalId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeProposals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradeProposals_Listings_SubjectListingId",
                        column: x => x.SubjectListingId,
                        principalTable: "Listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TradeProposals_TradeProposals_ParentTradeProposalId",
                        column: x => x.ParentTradeProposalId,
                        principalTable: "TradeProposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TradeProposals_TradeProposals_RootTradeProposalId",
                        column: x => x.RootTradeProposalId,
                        principalTable: "TradeProposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TradeProposals_Users_InitiatorUserId",
                        column: x => x.InitiatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TradeProposals_Users_ReceiverUserId",
                        column: x => x.ReceiverUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TradeProposalHistoryEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TradeProposalId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeProposalHistoryEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradeProposalHistoryEntries_TradeProposals_TradeProposalId",
                        column: x => x.TradeProposalId,
                        principalTable: "TradeProposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TradeProposalHistoryEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TradeProposalItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TradeProposalId = table.Column<int>(type: "integer", nullable: false),
                    Side = table.Column<int>(type: "integer", nullable: false),
                    ListingId = table.Column<int>(type: "integer", nullable: true),
                    CashAmount = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeProposalItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradeProposalItems_Listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TradeProposalItems_TradeProposals_TradeProposalId",
                        column: x => x.TradeProposalId,
                        principalTable: "TradeProposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ReplyToMessageId",
                table: "Messages",
                column: "ReplyToMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_TradeProposalId",
                table: "Messages",
                column: "TradeProposalId");

            migrationBuilder.CreateIndex(
                name: "IX_ListingExchangeAcceptedTags_TagId",
                table: "ListingExchangeAcceptedTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeProposalHistoryEntries_TradeProposalId",
                table: "TradeProposalHistoryEntries",
                column: "TradeProposalId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeProposalHistoryEntries_UserId",
                table: "TradeProposalHistoryEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeProposalItems_ListingId",
                table: "TradeProposalItems",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeProposalItems_TradeProposalId",
                table: "TradeProposalItems",
                column: "TradeProposalId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeProposals_InitiatorUserId",
                table: "TradeProposals",
                column: "InitiatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeProposals_ParentTradeProposalId",
                table: "TradeProposals",
                column: "ParentTradeProposalId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeProposals_ReceiverUserId",
                table: "TradeProposals",
                column: "ReceiverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeProposals_RootTradeProposalId",
                table: "TradeProposals",
                column: "RootTradeProposalId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeProposals_SubjectListingId",
                table: "TradeProposals",
                column: "SubjectListingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Messages_ReplyToMessageId",
                table: "Messages",
                column: "ReplyToMessageId",
                principalTable: "Messages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_TradeProposals_TradeProposalId",
                table: "Messages",
                column: "TradeProposalId",
                principalTable: "TradeProposals",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Messages_ReplyToMessageId",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_TradeProposals_TradeProposalId",
                table: "Messages");

            migrationBuilder.DropTable(
                name: "ListingExchangeAcceptedTags");

            migrationBuilder.DropTable(
                name: "TradeProposalHistoryEntries");

            migrationBuilder.DropTable(
                name: "TradeProposalItems");

            migrationBuilder.DropTable(
                name: "TradeProposals");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ReplyToMessageId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_TradeProposalId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ReplyToMessageId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "TradeProposalId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ExchangeDescription",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "MinExchangeValue",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "NotExchangeable",
                table: "Listings");
        }
    }
}
