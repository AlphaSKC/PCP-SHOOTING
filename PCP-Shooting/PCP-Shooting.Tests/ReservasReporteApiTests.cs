using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class ReservasReporteApiTests
{
    [Fact]
    public async Task ReporteFiltraPorFechaDeActividadYCsvExportaElConjuntoCompleto()
    {
        using var app = new DisponibilidadTestApp();
        using var admin = app.ClientePara("admin");
        for (var numero = 1; numero <= 3; numero++)
        {
            var respuesta = await admin.PostAsJsonAsync("/api/reservas/mostrador", new
            {
                idOperacion = Guid.NewGuid(), clienteId = PerfilesFactory.ClienteId, paqueteId = app.PaqueteId,
                fecha = "2026-11-20", hora = "11:00:00",
                participantes = new[] { new { lineaId = app.Leer(db => db.LineasTiro.Single(l => l.Numero == numero).Id),
                    nombre = "Participante", nacimiento = "2000-01-01" } }
            });
            Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        }
        app.Modificar(db =>
        {
            var reservas = db.ReservasWeb.OrderBy(r => r.Id).ToArray();
            reservas[0].PagoConfirmadoEnUtc = app.Reloj.Ahora.UtcDateTime;
            reservas[1].VencePagoEnUtc = app.Reloj.Ahora.UtcDateTime;
            reservas[2].VencePagoEnUtc = app.Reloj.Ahora.UtcDateTime;
            reservas[2].Pagos.Add(new PagoReserva { Id = Guid.NewGuid(), Importe = 100m, Estado = EstadoPago.Rechazado,
                CreadoEnUtc = app.Reloj.Ahora.UtcDateTime, ConciliacionEnCurso = true });
        });
        var filtro = $"usuarioId={PerfilesFactory.ClienteId}&desdeUtc=2026-11-20T16:00:00Z&hastaUtc=2026-11-20T16:15:00Z";
        var reporte = await admin.GetFromJsonAsync<JsonElement>("/api/reportes/reservas?" + filtro + "&tamanoPagina=1");
        Assert.Equal(3, reporte.GetProperty("datos").GetProperty("total").GetInt32());
        Assert.Single(reporte.GetProperty("datos").GetProperty("elementos").EnumerateArray());
        Assert.Equal(100m, reporte.GetProperty("importeConfirmado").GetDecimal());
        foreach (var estado in new[] { "Pagada", "Vencida", "PendientePago" })
        {
            var filtrado = await admin.GetFromJsonAsync<JsonElement>("/api/reportes/reservas?" + filtro + "&estado=" + estado);
            var fila = Assert.Single(filtrado.GetProperty("datos").GetProperty("elementos").EnumerateArray());
            Assert.Equal(estado, fila.GetProperty("estado").GetString());
        }
        var csv = await admin.GetAsync("/api/reportes/reservas?" + filtro + "&formato=csv&tamanoPagina=1&pagina=999");
        Assert.Equal(HttpStatusCode.OK, csv.StatusCode);
        Assert.Equal("text/csv", csv.Content.Headers.ContentType!.MediaType);
        var texto = await csv.Content.ReadAsStringAsync();
        Assert.Contains("NombreCliente", texto);
        Assert.Contains("Cliente de prueba", texto);
        Assert.Equal(4, texto.TrimEnd().Split("\r\n").Length);
        var fuera = await admin.GetFromJsonAsync<JsonElement>("/api/reportes/reservas?desdeUtc=2026-11-20T15:00:00Z&hastaUtc=2026-11-20T16:00:00Z");
        Assert.Equal(0, fuera.GetProperty("datos").GetProperty("total").GetInt32());
    }

    [Theory]
    [InlineData("estado=99")]
    [InlineData("pagina=0")]
    [InlineData("desdeUtc=2026-11-21T00:00:00Z&hastaUtc=2026-11-20T00:00:00Z")]
    [InlineData("formato=pdf")]
    public async Task RechazaFiltrosInvalidos(string filtro)
    {
        using var app = new DisponibilidadTestApp();
        using var admin = app.ClientePara("admin");
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/api/reportes/reservas?" + filtro)).StatusCode);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("cliente", HttpStatusCode.Forbidden)]
    [InlineData("recepcion", HttpStatusCode.Forbidden)]
    public async Task SoloAdministradorPuedeConsultar(string? identidad, HttpStatusCode esperado)
    {
        using var app = new DisponibilidadTestApp();
        using var cliente = app.ClientePara(identidad);
        Assert.Equal(esperado, (await cliente.GetAsync("/api/reportes/reservas")).StatusCode);
    }
}
