using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjektZespolowyGr3.Migrations
{
    /// <inheritdoc />
    public partial class SoftDeleteEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "TicketAttachments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TicketAttachments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "MessagePhotos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "MessagePhotos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Listings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Listings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "TicketAttachments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TicketAttachments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "MessagePhotos");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "MessagePhotos");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Listings");
        }
    }
}
