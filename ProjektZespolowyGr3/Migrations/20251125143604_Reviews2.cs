using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjektZespolowyGr3.Migrations
{
    /// <inheritdoc />
    public partial class Reviews2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReviewPhoto_Review_ReviewId",
                table: "ReviewPhoto");

            migrationBuilder.DropForeignKey(
                name: "FK_ReviewPhoto_Uploads_UploadId",
                table: "ReviewPhoto");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReviewPhoto",
                table: "ReviewPhoto");

            migrationBuilder.RenameTable(
                name: "ReviewPhoto",
                newName: "ReviewPhotos");

            migrationBuilder.RenameIndex(
                name: "IX_ReviewPhoto_UploadId",
                table: "ReviewPhotos",
                newName: "IX_ReviewPhotos_UploadId");

            migrationBuilder.RenameIndex(
                name: "IX_ReviewPhoto_ReviewId",
                table: "ReviewPhotos",
                newName: "IX_ReviewPhotos_ReviewId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReviewPhotos",
                table: "ReviewPhotos",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReviewPhotos_Review_ReviewId",
                table: "ReviewPhotos",
                column: "ReviewId",
                principalTable: "Review",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReviewPhotos_Uploads_UploadId",
                table: "ReviewPhotos",
                column: "UploadId",
                principalTable: "Uploads",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReviewPhotos_Review_ReviewId",
                table: "ReviewPhotos");

            migrationBuilder.DropForeignKey(
                name: "FK_ReviewPhotos_Uploads_UploadId",
                table: "ReviewPhotos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReviewPhotos",
                table: "ReviewPhotos");

            migrationBuilder.RenameTable(
                name: "ReviewPhotos",
                newName: "ReviewPhoto");

            migrationBuilder.RenameIndex(
                name: "IX_ReviewPhotos_UploadId",
                table: "ReviewPhoto",
                newName: "IX_ReviewPhoto_UploadId");

            migrationBuilder.RenameIndex(
                name: "IX_ReviewPhotos_ReviewId",
                table: "ReviewPhoto",
                newName: "IX_ReviewPhoto_ReviewId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReviewPhoto",
                table: "ReviewPhoto",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReviewPhoto_Review_ReviewId",
                table: "ReviewPhoto",
                column: "ReviewId",
                principalTable: "Review",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReviewPhoto_Uploads_UploadId",
                table: "ReviewPhoto",
                column: "UploadId",
                principalTable: "Uploads",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
