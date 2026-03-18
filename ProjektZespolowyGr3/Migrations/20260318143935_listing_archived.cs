using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjektZespolowyGr3.Migrations
{
    /// <inheritdoc />
    public partial class listing_archived : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Listings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Listings");
        }
    }
}
