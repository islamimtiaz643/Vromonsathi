using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vromonsathi.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingCancellationNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancellationNote",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationNote",
                table: "Bookings");
        }
    }
}
