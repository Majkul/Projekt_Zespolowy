using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjektZespolowyGr3.Migrations
{
    /// <inheritdoc />
    public partial class conflict_resolve : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserAuths_User_UserId",
                table: "UserAuths");

            migrationBuilder.DropTable(
                name: "Listing");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.AddForeignKey(
                name: "FK_ListingPhotos_Listings_ListingId",
                table: "ListingPhotos",
                column: "ListingId",
                principalTable: "Listings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ListingPhotos_Uploads_UploadId",
                table: "ListingPhotos",
                column: "UploadId",
                principalTable: "Uploads",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Listings_Users_SellerId",
                table: "Listings",
                column: "SellerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ListingTags_Listings_ListingId",
                table: "ListingTags",
                column: "ListingId",
                principalTable: "Listings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ListingTags_Tags_TagId",
                table: "ListingTags",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Uploads_Users_UploaderId",
                table: "Uploads",
                column: "UploaderId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAuths_Users_UserId",
                table: "UserAuths",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ListingPhotos_Listings_ListingId",
                table: "ListingPhotos");

            migrationBuilder.DropForeignKey(
                name: "FK_ListingPhotos_Uploads_UploadId",
                table: "ListingPhotos");

            migrationBuilder.DropForeignKey(
                name: "FK_Listings_Users_SellerId",
                table: "Listings");

            migrationBuilder.DropForeignKey(
                name: "FK_ListingTags_Listings_ListingId",
                table: "ListingTags");

            migrationBuilder.DropForeignKey(
                name: "FK_ListingTags_Tags_TagId",
                table: "ListingTags");

            migrationBuilder.DropForeignKey(
                name: "FK_Uploads_Users_UploaderId",
                table: "Uploads");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAuths_Users_UserId",
                table: "UserAuths");

            migrationBuilder.CreateTable(
                name: "Listing",
                columns: table => new
                {
                    SellerId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_Listing_Users_SellerId",
                        column: x => x.SellerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    TempId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.UniqueConstraint("AK_User_TempId", x => x.TempId);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_UserAuths_User_UserId",
                table: "UserAuths",
                column: "UserId",
                principalTable: "User",
                principalColumn: "TempId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
