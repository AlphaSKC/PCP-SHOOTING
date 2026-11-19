using System.Globalization;
using System.Text;

namespace PCP_Shooting.Domain.Services;

public static class CsvReporte
{
    public static byte[] Crear<T>(IReadOnlyList<string> encabezados, IEnumerable<T> filas, Func<T, object?[]> columnas)
    {
        var csv = new StringBuilder();
        csv.Append(string.Join(",", encabezados.Select(Celda))).Append("\r\n");
        foreach (var fila in filas) csv.Append(string.Join(",", columnas(fila).Select(Celda))).Append("\r\n");
        var utf8 = new UTF8Encoding(true);
        return utf8.GetPreamble().Concat(utf8.GetBytes(csv.ToString())).ToArray();
    }

    private static string Celda(object? valor)
    {
        var texto = valor switch
        {
            null => "",
            DateTime fecha => fecha.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture),
            DateOnly fecha => fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            IFormattable numero => numero.ToString(null, CultureInfo.InvariantCulture),
            _ => valor.ToString() ?? ""
        };
        // Los textos introducidos por usuarios no deben ejecutarse como fórmulas al abrir el CSV.
        if (valor is string && (texto.Any(c => c == '\t' || c == '\r') ||
            texto.TrimStart().FirstOrDefault() is '=' or '+' or '-' or '@')) texto = "'" + texto;
        return "\"" + texto.Replace("\"", "\"\"") + "\"";
    }
}
