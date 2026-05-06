using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjektZespolowyGr3.Migrations
{
    /// <inheritdoc />
    public partial class ViewCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE ""Listings"" ADD COLUMN IF NOT EXISTS ""ViewCount"" integer NOT NULL DEFAULT 0;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "Listings");
        }
    }
}
