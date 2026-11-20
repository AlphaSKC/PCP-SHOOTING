using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class ApartadosHistorialApiTests
{
    private static object Datos(DisponibilidadTestApp app, Guid id, string hora = "13:00:00") => new
    {
        idOperacion = id, paqueteId = app.PaqueteId, fecha = "2026-11-20", hora,
        lineas = new[] { app.Leer(db => db.LineasTiro.Single(l => l.Numero == 1).Id) }
    };

    [Fact]
    public async Task HistorialSeparaEstadosPaginaYConservaPrivacidadSinGenerarAuditoria()
    {
        using var app = new DisponibilidadTestApp();
        using var cliente = app.ClientePara("cliente");
        using var otro = app.ClientePara("admin");
        var primero = Guid.NewGuid();
        var segundo = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.Created, (await cliente.PostAsJsonAsync("/api/apartados", Datos(app, primero))).StatusCode);
        app.Reloj.Ahora = app.Reloj.Ahora.AddMinutes(2);
        Assert.Equal(HttpStatusCode.Created, (await cliente.PostAsJsonAsync("/api/apartados", Datos(app, segundo, "13:15:00"))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await otro.PostAsJsonAsync("/api/apartados", Datos(app, Guid.NewGuid(), "13:30:00"))).StatusCode);
        app.Reloj.Ahora = DateTimeOffset.Parse("2026-11-20T15:10:00Z");
        var auditoria = app.Leer(db => db.EventosAuditoria.Count());
        var lista = await cliente.GetFromJsonAsync<JsonElement>("/api/apartados?tamanoPagina=1&usuarioId=" + PerfilesFactory.AdminId);
        Assert.Equal(2, lista.GetProperty("total").GetInt32());
        Assert.Equal(app.Reloj.Ahora.UtcDateTime, lista.GetProperty("consultadoEnUtc").GetDateTime());
        Assert.Equal(segundo, Assert.Single(lista.GetProperty("elementos").EnumerateArray()).GetProperty("id").GetGuid());
        var vencidos = await cliente.GetFromJsonAsync<JsonElement>("/api/apartados?estado=Vencido");
        Assert.Equal(primero, Assert.Single(vencidos.GetProperty("elementos").EnumerateArray()).GetProperty("id").GetGuid());
        var vigentes = await cliente.GetFromJsonAsync<JsonElement>("/api/apartados?estado=Vigente");
        Assert.Equal(segundo, Assert.Single(vigentes.GetProperty("elementos").EnumerateArray()).GetProperty("id").GetGuid());
        var lejana = await cliente.GetFromJsonAsync<JsonElement>("/api/apartados?pagina=2147483647&tamanoPagina=100");
        Assert.Equal(2, lejana.GetProperty("total").GetInt32());
        Assert.Empty(lejana.GetProperty("elementos").EnumerateArray());
        Assert.Equal(auditoria, app.Leer(db => db.EventosAuditoria.Count()));
    }

    [Fact]
    public async Task LimiteExactoYCambioPosteriorDePlazoNoAlteranElHistorialNiReactivanId()
    {
        using var app = new DisponibilidadTestApp();
        using var cliente = app.ClientePara("cliente");
        using var otro = app.ClientePara("admin");
        var id = Guid.NewGuid();
        var datos = Datos(app, id);
        var respuesta = await cliente.PostAsJsonAsync("/api/apartados", datos);
        var original = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var vence = new DateTimeOffset(original.GetProperty("venceEnUtc").GetDateTime());
        app.Reloj.Ahora = vence.AddTicks(-1);
        Assert.Equal(1, (await cliente.GetFromJsonAsync<JsonElement>("/api/apartados?estado=Vigente")).GetProperty("total").GetInt32());
        app.Reloj.Ahora = vence;
        Assert.Equal(0, (await cliente.GetFromJsonAsync<JsonElement>("/api/apartados?estado=Vigente")).GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.Created, (await otro.PostAsJsonAsync("/api/apartados", Datos(app, Guid.NewGuid()))).StatusCode);
        app.Modificar(db => db.ConfiguracionOperativa.Single().ApartadoPagoMinutos = 60);
        var reintento = await cliente.PostAsJsonAsync("/api/apartados", datos);
        var expirado = await reintento.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Vencido", expirado.GetProperty("estado").GetString());
        Assert.Equal(vence.UtcDateTime, expirado.GetProperty("venceEnUtc").GetDateTime());
        var disponibilidad = await app.Cliente.GetFromJsonAsync<JsonElement>(app.Ruta() + "&hora=13:00");
        Assert.Equal(14, disponibilidad.GetProperty("horarios")[0].GetProperty("lineasLibres").GetInt32());
        Assert.Equal(2, app.Leer(db => db.Apartados.Count()));
        Assert.Equal(2, app.Leer(db => db.LineasApartado.Count()));
    }

    [Theory]
    [InlineData("estado=99")]
    [InlineData("estado=invalido")]
    [InlineData("pagina=0")]
    [InlineData("tamanoPagina=101")]
    public async Task FiltrosInvalidosDevuelven400(string filtro)
    {
        using var app = new DisponibilidadTestApp();
        using var cliente = app.ClientePara("cliente");
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.GetAsync("/api/apartados?" + filtro)).StatusCode);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("inactivo", HttpStatusCode.Forbidden)]
    public async Task HistorialRequierePerfilActivo(string? identidad, HttpStatusCode esperado)
    {
        using var app = new DisponibilidadTestApp();
        using var cliente = app.ClientePara(identidad);
        Assert.Equal(esperado, (await cliente.GetAsync("/api/apartados")).StatusCode);
    }
}

