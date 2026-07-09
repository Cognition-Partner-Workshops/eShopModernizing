using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStockAndDiscount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CatalogItemsStock",
                columns: table => new
                {
                    StockId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CatalogItemId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AvailableStock = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogItemsStock", x => x.StockId);
                });

            migrationBuilder.CreateTable(
                name: "DiscountItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Size = table.Column<double>(type: "float", nullable: false),
                    Start = table.Column<DateTime>(type: "datetime2", nullable: false),
                    End = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountItem", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "CatalogItemsStock",
                columns: new[] { "StockId", "AvailableStock", "CatalogItemId", "Date" },
                values: new object[,]
                {
                    { 1, 100, 1, new DateTime(2017, 9, 20, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 2, 120, 1, new DateTime(2017, 9, 21, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 3, 80, 1, new DateTime(2017, 9, 22, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 4, 45, 2, new DateTime(2017, 9, 20, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 5, 65, 4, new DateTime(2017, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 6, 22, 5, new DateTime(2017, 9, 28, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.InsertData(
                table: "DiscountItem",
                columns: new[] { "Id", "End", "Size", "Start" },
                values: new object[,]
                {
                    { 1, new DateTime(2017, 9, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), 0.29999999999999999, new DateTime(2017, 9, 18, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 2, new DateTime(2017, 9, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), 0.25, new DateTime(2017, 9, 22, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 3, new DateTime(2017, 9, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), 0.10000000000000001, new DateTime(2017, 9, 27, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 4, new DateTime(2017, 10, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), 0.5, new DateTime(2017, 10, 5, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 5, new DateTime(2017, 11, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 0.29999999999999999, new DateTime(2017, 11, 13, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 6, new DateTime(2017, 12, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 0.25, new DateTime(2017, 12, 20, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatalogItemsStock");

            migrationBuilder.DropTable(
                name: "DiscountItem");
        }
    }
}
