using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PCP_Shooting.Persistence;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class EventosApiTests
{
    private const string Gestion = "/api/administracion/eventos";
    internal static object Datos(string slug, string estado = "Borrador", Guid? version = null,
        string inicio = "2027-01-10T10:00:00-05:00", string fin = "2027-01-10T12:00:00-05:00",
        string? imagen = "https://example.com/evento.webp") => new {
            slug, titulo = "Encuentro del club", resumen = "Información del encuentro", descripcionMarkdown = "## Programa\n\n- Bienvenida",
            ubicacion = "PCP Shooting", inicioUtc = inicio, finUtc = fin, imagenUrl = imagen, estado, version
        };

    [Fact]
    public async Task BorradorPublicacionEdicionYArchivoConservanPrivacidadVersionYAuditoria()
    {
        using var app = new PerfilesFactory(); using var admin = app.CrearCliente("admin"); using var publico = app.CrearCliente(null);
        var creado = await admin.PostAsJsonAsync(Gestion, Datos("encuentro"));
        Assert.True(creado.StatusCode == HttpStatusCode.Created, await creado.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync(creado.Headers.Location)).StatusCode);
        var evento = await creado.Content.ReadFromJsonAsync<JsonElement>(); var id = evento.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await publico.GetAsync("/api/eventos/encuentro")).StatusCode);
        Assert.Equal(0, (await publico.GetFromJsonAsync<JsonElement>("/api/eventos")).GetProperty("total").GetInt32());
        var datos = Datos("encuentro", "Publicado", evento.GetProperty("version").GetGuid());
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"{Gestion}/{id}", datos)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync($"{Gestion}/{id}", datos)).StatusCode);
        var detalle = await publico.GetFromJsonAsync<JsonElement>("/api/eventos/encuentro");
        Assert.Equal(DateTime.Parse("2027-01-10T15:00:00Z").ToUniversalTime(), detalle.GetProperty("inicioUtc").GetDateTime());
        Assert.Contains("Bienvenida", detalle.GetProperty("descripcionMarkdown").GetString());
        Assert.False(detalle.TryGetProperty("version", out _)); Assert.False(detalle.TryGetProperty("creadoEnUtc", out _));
        Assert.False(detalle.TryGetProperty("cupo", out _)); Assert.False(detalle.TryGetProperty("precio", out _));
        var actual = await admin.GetFromJsonAsync<JsonElement>($"{Gestion}/{id}");
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"{Gestion}/{id}", Datos("encuentro", "Archivado", actual.GetProperty("version").GetGuid()))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await publico.GetAsync("/api/eventos/encuentro")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"{Gestion}/{id}")).StatusCode);
        using var scope = app.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>();
        Assert.Equal(3, db.EventosAuditoria.Count(e => e.EntidadId == id));
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("cliente", HttpStatusCode.Forbidden)]
    [InlineData("recepcion", HttpStatusCode.Forbidden)]
    [InlineData("inactivo", HttpStatusCode.Forbidden)]
    public async Task SoloAdministradorActivoGestiona(string? actor, HttpStatusCode esperado)
    {
        using var app = new PerfilesFactory(); using var c = app.CrearCliente(actor);
        Assert.Equal(esperado, (await c.GetAsync(Gestion)).StatusCode);
        Assert.Equal(esperado, (await c.PostAsJsonAsync(Gestion, Datos("privado"))).StatusCode);
        Assert.Equal(esperado, (await c.PutAsJsonAsync($"{Gestion}/{Guid.NewGuid()}", Datos("privado"))).StatusCode);
    }

    [Fact]
    public async Task ValidaFechasImagenSlugVersionYDuplicados()
    {
        using var app = new PerfilesFactory(); using var admin = app.CrearCliente("admin");
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(Gestion, Datos("malo", fin: "2027-01-10T10:00:00-05:00"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(Gestion, Datos("malo", imagen: "javascript:alert(1)"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(Gestion, Datos("Slug Malo"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(Gestion, Datos("malo", estado: "Inexistente"))).StatusCode);
        var creado = await admin.PostAsJsonAsync(Gestion, Datos("unico", imagen: null));
        Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(Gestion, Datos("unico"))).StatusCode);
        var id = (await creado.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync($"{Gestion}/{id}", Datos("unico"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"{Gestion}/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task ProximosIncluyeEnCursoYExcluyeElInstanteFinalSinOcultarHistorico()
    {
        using var origen = new PerfilesFactory();
        var reloj = new RelojDisponibilidad(DateTimeOffset.Parse("2027-01-10T15:00:00Z"));
        using var app = origen.WithWebHostBuilder(b => b.ConfigureServices(s => { s.RemoveAll<TimeProvider>(); s.AddSingleton<TimeProvider>(reloj); }));
        using var admin = app.CreateClient(); admin.DefaultRequestHeaders.Add("X-Identidad-Prueba", "admin"); using var publico = app.CreateClient();
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync(Gestion, Datos("limites", "Publicado"))).StatusCode);
        Assert.Equal(1, (await publico.GetFromJsonAsync<JsonElement>("/api/eventos?proximos=true")).GetProperty("total").GetInt32());
        reloj.Ahora = DateTimeOffset.Parse("2027-01-10T17:00:00Z");
        Assert.Equal(0, (await publico.GetFromJsonAsync<JsonElement>("/api/eventos?proximos=true")).GetProperty("total").GetInt32());
        Assert.Equal(1, (await publico.GetFromJsonAsync<JsonElement>("/api/eventos?proximos=false")).GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.OK, (await publico.GetAsync("/api/eventos/limites")).StatusCode);
    }

    [Fact]
    public async Task PublicoFiltraInicioBuscaYPaginaSinMostrarBorradores()
    {
        using var app = new PerfilesFactory(); using var admin = app.CrearCliente("admin"); using var publico = app.CrearCliente(null);
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync(Gestion, Datos("enero", "Publicado"))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync(Gestion, Datos("febrero", "Publicado", inicio: "2027-02-10T15:00:00Z", fin: "2027-02-10T17:00:00Z"))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync(Gestion, Datos("oculto"))).StatusCode);
        var pagina = await publico.GetFromJsonAsync<JsonElement>("/api/eventos?tamanoPagina=1&busqueda=ENCUENTRO");
        Assert.Equal(2, pagina.GetProperty("total").GetInt32());
        var primero = Assert.Single(pagina.GetProperty("elementos").EnumerateArray());
        Assert.Equal("enero", primero.GetProperty("slug").GetString());
        Assert.False(primero.TryGetProperty("descripcionMarkdown", out _));
        var filtrado = await publico.GetFromJsonAsync<JsonElement>("/api/eventos?desdeUtc=2027-02-10T15:00:00Z&hastaUtc=2027-02-11T00:00:00Z");
        Assert.Equal(1, filtrado.GetProperty("total").GetInt32());
        Assert.Equal(0, (await publico.GetFromJsonAsync<JsonElement>("/api/eventos?hastaUtc=2027-01-10T15:00:00Z")).GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest, (await publico.GetAsync("/api/eventos?desdeUtc=2027-02-01T00:00:00Z&hastaUtc=2027-01-01T00:00:00Z")).StatusCode);
        Assert.Equal(1, (await admin.GetFromJsonAsync<JsonElement>($"{Gestion}?estado=Borrador")).GetProperty("total").GetInt32());
    }
}
