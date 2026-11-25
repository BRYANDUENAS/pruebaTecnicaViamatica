using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Viamatica.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "menu_items",
                columns: new[] { "menu_item_id", "icon", "label", "order", "route" },
                values: new object[] { 7, "settings", "Cajas y servicios", 4, "/mantenimiento" });

            migrationBuilder.InsertData(
                table: "rol_menus",
                columns: new[] { "menu_item_id", "rol_id" },
                values: new object[] { 7, 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "rol_menus",
                keyColumns: new[] { "menu_item_id", "rol_id" },
                keyValues: new object[] { 7, 1 });

            migrationBuilder.DeleteData(
                table: "menu_items",
                keyColumn: "menu_item_id",
                keyValue: 7);
        }
    }
}
