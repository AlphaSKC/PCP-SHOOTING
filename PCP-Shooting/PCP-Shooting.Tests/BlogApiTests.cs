using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PCP_Shooting.Persistence;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class BlogApiTests
{
    private const string Gestion = "/api/administracion/blog";
    private static object Datos(string slug, string estado = "Borrador", DateTimeOffset? fecha = null, Guid? version = null,
        string titulo = "Noticias del club", string? imagen = "https://example.com/portada.jpg") => new
        { slug, titulo, resumen = "Resumen del artículo", contenidoMarkdown = "## Noticias\n\n- Una **novedad**", categoria = "Club", destacado = true,
            imagenUrl = imagen, estado, publicarEnUtc = fecha, version };

    private static async Task<JsonElement> Crear(HttpClient admin, object datos)
    {
        var response = await admin.PostAsJsonAsync(Gestion, datos);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        return (await response.Content.ReadFromJsonAsync<JsonElement>());
    }

    [Fact]
    public async Task BorradorSoloEsVisibleParaAdministracionYPublicarLoHacePublico()
    {
        using var factory = new PerfilesFactory();
        using var admin = factory.CrearCliente("admin");
        using var publico = factory.CrearCliente(null);
        var articulo = await Crear(admin, Datos("noticias"));
        var id = articulo.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await publico.GetAsync("/api/blog/noticias")).StatusCode);
        var listado = await publico.GetFromJsonAsync<JsonElement>("/api/blog");
        Assert.Equal(0, listado.GetProperty("total").GetInt32());
        var editar = await admin.PutAsJsonAsync(Gestion + "/" + id, Datos("noticias", "Publicado", version: articulo.GetProperty("version").GetGuid()));
        Assert.Equal(HttpStatusCode.OK, editar.StatusCode);
        var detalle = await publico.GetFromJsonAsync<JsonElement>("/api/blog/noticias");
        Assert.Equal("Noticias del club", detalle.GetProperty("titulo").GetString());
        Assert.Contains("**novedad**", detalle.GetProperty("contenidoMarkdown").GetString());
        Assert.False(detalle.TryGetProperty("version", out _));
        Assert.False(detalle.TryGetProperty("autorId", out _));
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>();
        Assert.Equal(2, db.EventosAuditoria.Count(e => e.EntidadId == id));
    }

    [Fact]
    public async Task ProgramadosNoSeFiltranHastaSuFechaYArchivadosNoSePublican()
    {
        using var factory = new PerfilesFactory();
        using var admin = factory.CrearCliente("admin");
        using var publico = factory.CrearCliente(null);
        var articulo = await Crear(admin, Datos("programado", "Programado", DateTimeOffset.UtcNow.AddDays(1)));
        var id = articulo.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await publico.GetAsync("/api/blog/programado")).StatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>();
            var tipo = db.Model.GetEntityTypes().Single(e => e.ClrType.Name == "ArticuloBlog").ClrType;
            var guardado = await db.FindAsync(tipo, id);
            db.Entry(guardado!).Property("PublicarEnUtc").CurrentValue = DateTime.UtcNow.AddDays(-1);
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.OK, (await publico.GetAsync("/api/blog/programado")).StatusCode);
        var gestion = await admin.GetFromJsonAsync<JsonElement>(Gestion + "/" + id);
        Assert.Equal("Publicado", gestion.GetProperty("estado").GetString());
        var archivo = await admin.PutAsJsonAsync(Gestion + "/" + id,
            Datos("programado", "Archivado", version: gestion.GetProperty("version").GetGuid()));
        Assert.Equal(HttpStatusCode.OK, archivo.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await publico.GetAsync("/api/blog/programado")).StatusCode);
    }

    [Fact]
    public async Task SlugDuplicadoYVersionAntiguaProducenConflicto()
    {
        using var factory = new PerfilesFactory();
        using var admin = factory.CrearCliente("admin");
        var articulo = await Crear(admin, Datos("duplicado"));
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(Gestion, Datos("duplicado"))).StatusCode);
        var ruta = Gestion + "/" + articulo.GetProperty("id").GetGuid();
        var datos = Datos("duplicado", version: articulo.GetProperty("version").GetGuid());
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync(ruta, datos)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync(ruta, datos)).StatusCode);
    }

    [Fact]
    public async Task ListadoPublicoFiltraYPaginaSinContenidoCompleto()
    {
        using var factory = new PerfilesFactory();
        using var admin = factory.CrearCliente("admin");
        using var publico = factory.CrearCliente(null);
        await Crear(admin, Datos("uno", "Publicado"));
        await Crear(admin, Datos("dos", "Publicado"));
        await Crear(admin, Datos("tres"));
        var pagina = await publico.GetFromJsonAsync<JsonElement>("/api/blog?busqueda=NOTICIAS&categoria=Club&destacado=true&tamanoPagina=1");
        Assert.Equal(2, pagina.GetProperty("total").GetInt32());
        var fila = Assert.Single(pagina.GetProperty("elementos").EnumerateArray());
        Assert.False(fila.TryGetProperty("contenidoMarkdown", out _));
        var lejana = await publico.GetFromJsonAsync<JsonElement>("/api/blog?pagina=2147483647&tamanoPagina=100");
        Assert.Empty(lejana.GetProperty("elementos").EnumerateArray());
        var borradores = await admin.GetFromJsonAsync<JsonElement>(Gestion + "?estado=Borrador");
        Assert.Equal(1, borradores.GetProperty("total").GetInt32());
    }

    [Theory]
    [InlineData("MAYUSCULAS", "Borrador", "Titulo", null)]
    [InlineData("invalido--slug", "Borrador", "Titulo", null)]
    [InlineData("valido", "Borrador", "   ", null)]
    [InlineData("valido", "Programado", "Titulo", null)]
    [InlineData("valido", "Borrador", "Titulo", "javascript:alert(1)")]
    [InlineData("valido", "OtroEstado", "Titulo", null)]
    public async Task RechazaDatosInvalidos(string slug, string estado, string titulo, string? imagen)
    {
        using var factory = new PerfilesFactory();
        using var admin = factory.CrearCliente("admin");
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(Gestion, Datos(slug, estado, titulo: titulo, imagen: imagen))).StatusCode);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("cliente", HttpStatusCode.Forbidden)]
    [InlineData("inactivo", HttpStatusCode.Forbidden)]
    public async Task GestionRestringida(string? identidad, HttpStatusCode esperado)
    {
        using var factory = new PerfilesFactory();
        using var cliente = factory.CrearCliente(identidad);
        Assert.Equal(esperado, (await cliente.GetAsync(Gestion)).StatusCode);
        Assert.Equal(esperado, (await cliente.PostAsJsonAsync(Gestion, Datos("sin-acceso"))).StatusCode);
        Assert.Equal(esperado, (await cliente.PutAsJsonAsync(Gestion + "/" + Guid.NewGuid(), Datos("sin-acceso"))).StatusCode);
    }

    [Fact]
    public async Task RecepcionNoGestionaContenido()
    {
        using var factory = new JwtBackendFactory();
        using var cliente = factory.Cliente(factory.Token(JwtBackendFactory.RecepcionId));
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PostAsJsonAsync(Gestion, Datos("sin-acceso"))).StatusCode);
    }
}
