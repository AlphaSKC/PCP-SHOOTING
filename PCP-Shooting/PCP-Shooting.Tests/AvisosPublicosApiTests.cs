using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class AvisosPublicosApiTests
{
    [Fact]
    public async Task VigenciaIncluyeInicioExcluyeFinYDesactivarOcultaSinBorrar()
    {
        using var origen = new PerfilesFactory();
        var reloj = new RelojDisponibilidad(DateTimeOffset.Parse("2026-11-24T15:00:00Z"));
        using var app = origen.WithWebHostBuilder(b => b.ConfigureServices(s => { s.RemoveAll<TimeProvider>(); s.AddSingleton<TimeProvider>(reloj); }));
        using var admin = app.CreateClient(); admin.DefaultRequestHeaders.Add("X-Identidad-Prueba", "admin");
        using var publico = app.CreateClient();
        var inicio = reloj.Ahora.AddMinutes(10); var fin = inicio.AddHours(1);
        var creado = await admin.PostAsJsonAsync("/api/administracion/avisos-publicos", new { titulo = "Mantenimiento", texto = "Información del club", activo = true, orden = 0, inicioUtc = inicio, finUtc = fin });
        Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
        var aviso = await creado.Content.ReadFromJsonAsync<JsonElement>(); var id = aviso.GetProperty("id").GetGuid();
        Assert.Equal("Programado", aviso.GetProperty("estado").GetString());
        Assert.Equal(0, (await publico.GetFromJsonAsync<JsonElement>("/api/avisos-publicos")).GetProperty("total").GetInt32());
        reloj.Ahora = inicio;
        var visible = (await publico.GetFromJsonAsync<JsonElement>("/api/avisos-publicos")).GetProperty("elementos");
        Assert.Equal(id, Assert.Single(visible.EnumerateArray()).GetProperty("id").GetGuid());
        Assert.False(visible[0].TryGetProperty("version", out _));
        reloj.Ahora = fin;
        Assert.Equal(0, (await publico.GetFromJsonAsync<JsonElement>("/api/avisos-publicos")).GetProperty("total").GetInt32());
        var vencido = await admin.GetFromJsonAsync<JsonElement>($"/api/administracion/avisos-publicos/{id}");
        Assert.Equal("Vencido", vencido.GetProperty("estado").GetString());
        var datos = new { titulo = "Mantenimiento", texto = "Información del club", activo = false, orden = 0, inicioUtc = inicio, finUtc = fin, version = vencido.GetProperty("version").GetGuid() };
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/administracion/avisos-publicos/{id}", datos)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync($"/api/administracion/avisos-publicos/{id}", datos)).StatusCode);
        Assert.Equal("Inactivo", (await admin.GetFromJsonAsync<JsonElement>($"/api/administracion/avisos-publicos/{id}")).GetProperty("estado").GetString());
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("cliente", HttpStatusCode.Forbidden)]
    [InlineData("recepcion", HttpStatusCode.Forbidden)]
    public async Task SoloAdministradorGestiona(string? actor, HttpStatusCode esperado)
    {
        using var app = new PerfilesFactory(); using var c = app.CrearCliente(actor);
        Assert.Equal(esperado, (await c.GetAsync("/api/administracion/avisos-publicos")).StatusCode);
        Assert.Equal(esperado, (await c.PostAsJsonAsync("/api/administracion/avisos-publicos", new { texto = "Aviso", activo = false, orden = 0, inicioUtc = (DateTimeOffset?)null, finUtc = (DateTimeOffset?)null })).StatusCode);
    }

    [Fact]
    public async Task SinFechasPublicaEnOrdenYPaginaConservandoTotal()
    {
        using var app = new PerfilesFactory(); using var admin = app.CrearCliente("admin"); using var publico = app.CreateClient();
        var ruta = "/api/administracion/avisos-publicos";
        foreach (var orden in new[] { 8, 1 })
            Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync(ruta,
                new { texto = $"Aviso {orden}", activo = true, orden, inicioUtc = (DateTimeOffset?)null, finUtc = (DateTimeOffset?)null })).StatusCode);
        var pagina = await publico.GetFromJsonAsync<JsonElement>("/api/avisos-publicos?tamanoPagina=1");
        Assert.Equal(2, pagina.GetProperty("total").GetInt32());
        var primero = Assert.Single(pagina.GetProperty("elementos").EnumerateArray());
        Assert.Equal("Aviso 1", primero.GetProperty("texto").GetString());
        var id = primero.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync($"{ruta}/{id}",
            new { texto = "Cambio", activo = true, orden = 0, inicioUtc = (DateTimeOffset?)null, finUtc = (DateTimeOffset?)null })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"{ruta}/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await publico.GetAsync($"/api/avisos-publicos/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await publico.GetAsync("/api/avisos-publicos?tamanoPagina=101")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync($"{ruta}?estado=999")).StatusCode);
        Assert.Equal(0, (await publico.GetFromJsonAsync<JsonElement>("/api/avisos-publicos?pagina=2147483647")).GetProperty("elementos").GetArrayLength());
    }

    [Fact]
    public async Task RechazaFechasInvertidasTextoVacioYOrdenNegativo()
    {
        using var app = new PerfilesFactory(); using var admin = app.CrearCliente("admin");
        var ruta = "/api/administracion/avisos-publicos";
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(ruta, new { texto = "Aviso", activo = true, orden = 0, inicioUtc = "2026-11-25T10:00:00Z", finUtc = "2026-11-24T10:00:00Z" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(ruta, new { texto = " ", activo = false, orden = -1, inicioUtc = (string?)null, finUtc = (string?)null })).StatusCode);
    }
}
