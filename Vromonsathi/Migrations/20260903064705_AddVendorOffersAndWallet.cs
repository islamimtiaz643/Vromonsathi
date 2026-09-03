using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vromonsathi.Migrations
{
    /// <inheritdoc />
    public partial class AddVendorOffersAndWallet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "WalletBalance",
                table: "Users",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "MaxGroupSize",
                table: "TourPackages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "WalletCreditEarned",
                table: "Bookings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WalletCreditUsed",
                table: "Bookings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "PackageLineItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TourPackageId = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageLineItems_TourPackages_TourPackageId",
                        column: x => x.TourPackageId,
                        principalTable: "TourPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VendorPackageOffers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TourPackageId = table.Column<int>(type: "int", nullable: false),
                    VendorProfileId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorPackageOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorPackageOffers_TourPackages_TourPackageId",
                        column: x => x.TourPackageId,
                        principalTable: "TourPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VendorPackageOffers_VendorProfiles_VendorProfileId",
                        column: x => x.VendorProfileId,
                        principalTable: "VendorProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookingAddOns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingId = table.Column<int>(type: "int", nullable: false),
                    VendorPackageOfferId = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingAddOns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingAddOns_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingAddOns_VendorPackageOffers_VendorPackageOfferId",
                        column: x => x.VendorPackageOfferId,
                        principalTable: "VendorPackageOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingAddOns_BookingId",
                table: "BookingAddOns",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingAddOns_VendorPackageOfferId",
                table: "BookingAddOns",
                column: "VendorPackageOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageLineItems_TourPackageId",
                table: "PackageLineItems",
                column: "TourPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorPackageOffers_TourPackageId",
                table: "VendorPackageOffers",
                column: "TourPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorPackageOffers_VendorProfileId",
                table: "VendorPackageOffers",
                column: "VendorProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingAddOns");

            migrationBuilder.DropTable(
                name: "PackageLineItems");

            migrationBuilder.DropTable(
                name: "VendorPackageOffers");

            migrationBuilder.DropColumn(
                name: "WalletBalance",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MaxGroupSize",
                table: "TourPackages");

            migrationBuilder.DropColumn(
                name: "WalletCreditEarned",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "WalletCreditUsed",
                table: "Bookings");
        }
    }
}
