using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eShop.Catalog.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogHiLoSequences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateSequence(
                name: "catalog_brand_hilo",
                schema: "dbo",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "catalog_hilo",
                schema: "dbo",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "catalog_type_hilo",
                schema: "dbo",
                incrementBy: 10);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSequence(
                name: "catalog_brand_hilo",
                schema: "dbo");

            migrationBuilder.DropSequence(
                name: "catalog_hilo",
                schema: "dbo");

            migrationBuilder.DropSequence(
                name: "catalog_type_hilo",
                schema: "dbo");
        }
    }
}
