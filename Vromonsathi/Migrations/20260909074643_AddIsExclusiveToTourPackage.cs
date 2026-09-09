using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vromonsathi.Migrations
{
    /// <inheritdoc />
    public partial class AddIsExclusiveToTourPackage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsExclusive",
                table: "TourPackages",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsExclusive",
                table: "TourPackages");
        }
    }
}
