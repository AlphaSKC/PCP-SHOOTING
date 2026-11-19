using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class ReportesLimitesTests
{
    [Fact]
    public async Task ExportacionExcesivaSeRechazaSinEntregarArchivoTruncado()
    {
        using var factory = new PerfilesFactory();
        using var admin = factory.CrearCliente("admin");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>();
            db.Productos.AddRange(Enumerable.Range(0, 10001).Select(i => new Producto { Id = Guid.NewGuid(), Sku = "LIM-" + i, Nombre = "Producto" }));
            await db.SaveChangesAsync();
        }
        var respuesta = await admin.GetAsync("/api/reportes/inventario?formato=csv");
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType!.MediaType);
        Assert.Contains("10000", await respuesta.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("pedidos")]
    [InlineData("pagos")]
    [InlineData("membresias")]
    [InlineData("inventario")]
    public async Task ReporteVacioEntregaJsonVacioOCsvSoloConEncabezados(string tipo)
    {
        using var factory = new PerfilesFactory();
        using var admin = factory.CrearCliente("admin");
        using var json = JsonDocument.Parse(await admin.GetStringAsync($"/api/reportes/{tipo}"));
        Assert.Equal(0, json.RootElement.GetProperty("datos").GetProperty("total").GetInt32());
        Assert.Empty(json.RootElement.GetProperty("datos").GetProperty("elementos").EnumerateArray());
        if (tipo is "pagos" or "membresias") Assert.Equal(0, json.RootElement.GetProperty("importeConfirmado").GetDecimal());
        var csv = await admin.GetStringAsync($"/api/reportes/{tipo}?formato=csv");
        Assert.Single(csv.TrimEnd().Split("\r\n"));
    }
}
