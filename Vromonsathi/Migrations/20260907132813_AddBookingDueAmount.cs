using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vromonsathi.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingDueAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DueAmount",
                table: "Bookings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DuePaid",
                table: "Bookings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DueAmount",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "DuePaid",
                table: "Bookings");
        }
    }
}
