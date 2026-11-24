using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class AlimentosCatalogoPostgresTests(PostgresEscriturasFixture fixture) : IClassFixture<PostgresEscriturasFixture>
{
    [PostgresFact]
    public async Task CartaFiltraConSqlYComparteStockConLaGestionExistente()
    {
        await using (var db = fixture.CrearContext()) await AlimentosApiTests.Sembrar(db);
        using var app = new OperacionesFactory(fixture); using var publico = app.CreateClient(); using var admin = app.Cliente(OperacionesFactory.AdminId);
        var pagina = await publico.GetFromJsonAsync<JsonElement>("/api/alimentos?busqueda=AGUA&precioMinimo=25&precioMaximo=25");
        Assert.Equal(1, pagina.GetProperty("total").GetInt32());
        var agua = Assert.Single(pagina.GetProperty("elementos").EnumerateArray());
        var id = agua.GetProperty("id").GetGuid();
        Assert.Equal(3, agua.GetProperty("existencia").GetInt64());
        var detalle = await publico.GetFromJsonAsync<JsonElement>("/api/alimentos/ALI-AGUA");
        Assert.Equal(agua.GetRawText(), detalle.GetRawText());
        Assert.Equal(HttpStatusCode.NotFound, (await publico.GetAsync("/api/alimentos/ALI-OCULTO")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await publico.GetAsync("/api/alimentos/ALI-PLAYERA")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await publico.GetAsync("/api/alimentos/ALI-NOVENTA")).StatusCode);
        Assert.Equal(2, (await publico.GetFromJsonAsync<JsonElement>("/api/alimentos?tamanoPagina=1&pagina=2")).GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"/api/inventario/{id}/movimientos",
            new { idOperacion = Guid.NewGuid(), tipo = 1, variacionCantidad = -3, motivo = "Salida de alimentos" })).StatusCode);
        var agotada = await publico.GetFromJsonAsync<JsonElement>("/api/alimentos/ALI-AGUA");
        Assert.Equal(0, agotada.GetProperty("existencia").GetInt64()); Assert.True(agotada.GetProperty("agotado").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, (await admin.PatchAsJsonAsync($"/api/productos/{id}/estado", new { activo = false })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await publico.GetAsync("/api/alimentos/ALI-AGUA")).StatusCode);
        Assert.Equal(1, (await publico.GetFromJsonAsync<JsonElement>("/api/alimentos")).GetProperty("total").GetInt32());
    }
}
