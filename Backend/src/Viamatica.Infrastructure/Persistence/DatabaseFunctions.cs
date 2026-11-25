using System.Reflection;

namespace Viamatica.Infrastructure.Persistence;

/// <summary>
/// Lee el script <c>Database/functions.sql</c>, incrustado en el ensamblado,
/// para que la migración y el archivo de la entrega sean la misma fuente.
/// </summary>
public static class DatabaseFunctions
{
    public static string Sql
    {
        get
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("Viamatica.Infrastructure.functions.sql")
                ?? throw new InvalidOperationException("No se encontró el recurso functions.sql");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }

    public const string DropSql = """
        DROP FUNCTION IF EXISTS fn_create_turn(varchar, integer, integer);
        DROP FUNCTION IF EXISTS fn_cash_indicators(timestamptz, timestamptz);
        """;
}
