using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Viamatica.Infrastructure.Persistence.Migrations
{
    /// <summary>Crea las funciones de PostgreSQL definidas en Database/functions.sql.</summary>
    public partial class DatabaseFunctions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(Persistence.DatabaseFunctions.Sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(Persistence.DatabaseFunctions.DropSql);
        }
    }
}
