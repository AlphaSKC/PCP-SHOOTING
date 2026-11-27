using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Interfaces;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class ContenidoImagenesApiTests
{
    [Fact]
    public async Task MarcasPublicasFiltranActivasYVersionProtegeEdicion()
    {
        using var app = new App(); using var admin = app.Cliente("admin"); using var publico = app.Cliente(null);
        var creada = await CrearMarca(admin, false);
        var id = creada.GetProperty("id").GetGuid(); var version = creada.GetProperty("version").GetGuid();
        Assert.DoesNotContain((await publico.GetFromJsonAsync<JsonElement>("/api/marcas")).EnumerateArray(), m => m.GetProperty("id").GetGuid() == id);
        Assert.Contains((await admin.GetFromJsonAsync<JsonElement>("/api/administracion/marcas")).EnumerateArray(), m => m.GetProperty("id").GetGuid() == id);
        var datos = new { nombre = "Marca prueba", descripcion = "Equipo", url = "https://ejemplo.test", activo = true, orden = 0, version };
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync("/api/administracion/marcas/" + id, datos)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync("/api/administracion/marcas/" + id, datos)).StatusCode);
        var lista = await publico.GetFromJsonAsync<JsonElement>("/api/marcas");
        Assert.Contains(lista.EnumerateArray(), m => m.GetProperty("id").GetGuid() == id);
        var orden = lista.EnumerateArray().Select(m => m.GetProperty("orden").GetInt32()).ToArray();
        Assert.Equal(orden.OrderBy(o => o), orden);
    }

    [Fact]
    public async Task LogoReintentaSinDuplicarYNoSeRestauraDespuesDeQuitar()
    {
        using var app = new App(); using var admin = app.Cliente("admin");
        var id = (await CrearMarca(admin)).GetProperty("id").GetGuid(); var operacion = Guid.NewGuid();
        var ruta = $"/api/administracion/marcas/{id}/logo";
        var primera = await Subir(admin, ruta, operacion);
        Assert.Equal(HttpStatusCode.OK, primera.StatusCode);
        var logo = (await primera.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("logoUrl").GetString();
        Assert.Contains("marcas/", logo);
        Assert.Equal(HttpStatusCode.OK, (await Subir(admin, ruta, operacion)).StatusCode);
        Assert.Equal(1, app.Storage.Intentos);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync(ruta)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Subir(admin, ruta, operacion)).StatusCode);
        var marca = (await admin.GetFromJsonAsync<JsonElement>("/api/administracion/marcas")).EnumerateArray().Single(m => m.GetProperty("id").GetGuid() == id);
        Assert.Equal(JsonValueKind.Null, marca.GetProperty("logoUrl").ValueKind);
    }

    [Fact]
    public async Task PortadaBlogFalloConservaBorradorYReintentoGuardaVersionSinPublicar()
    {
        using var app = new App(); using var admin = app.Cliente("admin");
        var respuesta = await admin.PostAsJsonAsync("/api/administracion/blog", new { slug = "portada-prueba", titulo = "Prueba", resumen = "Resumen", contenidoMarkdown = "Contenido", estado = "Borrador" });
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var articulo = await respuesta.Content.ReadFromJsonAsync<JsonElement>(); var id = articulo.GetProperty("id").GetGuid();
        var ruta = $"/api/administracion/blog/{id}/portada"; var operacion = Guid.NewGuid(); app.Storage.Fallar = true;
        Assert.Equal(HttpStatusCode.BadGateway, (await Subir(admin, ruta, operacion)).StatusCode);
        var pendiente = await admin.GetFromJsonAsync<JsonElement>("/api/administracion/blog/" + id);
        Assert.Equal(JsonValueKind.Null, pendiente.GetProperty("imagenUrl").ValueKind);
        Assert.Equal(HttpStatusCode.OK, (await Subir(admin, ruta, operacion)).StatusCode);
        var guardado = await admin.GetFromJsonAsync<JsonElement>("/api/administracion/blog/" + id);
        Assert.Contains("blog/", guardado.GetProperty("imagenUrl").GetString());
        Assert.NotEqual(articulo.GetProperty("version").GetGuid(), guardado.GetProperty("version").GetGuid());
        Assert.Equal("Borrador", guardado.GetProperty("estado").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/blog/portada-prueba")).StatusCode);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("cliente", HttpStatusCode.Forbidden)]
    [InlineData("recepcion", HttpStatusCode.Forbidden)]
    public async Task SoloAdministradorGestionaMarcasYPortadas(string? actor, HttpStatusCode esperado)
    {
        using var app = new App(); using var cliente = app.Cliente(actor);
        Assert.Equal(esperado, (await cliente.GetAsync("/api/administracion/marcas")).StatusCode);
        Assert.Equal(esperado, (await Subir(cliente, $"/api/administracion/marcas/{Guid.NewGuid()}/logo", Guid.NewGuid())).StatusCode);
        Assert.Equal(esperado, (await Subir(cliente, $"/api/administracion/blog/{Guid.NewGuid()}/portada", Guid.NewGuid())).StatusCode);
        Assert.Equal(0, app.Storage.Intentos);
    }

    [Fact]
    public async Task RechazaEnlacesYArchivosInvalidosAntesDeStorage()
    {
        using var app = new App(); using var admin = app.Cliente("admin");
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/administracion/marcas", new { nombre = "Marca", url = "http://ejemplo.test" })).StatusCode);
        var id = (await CrearMarca(admin)).GetProperty("id").GetGuid(); var ruta = $"/api/administracion/marcas/{id}/logo";
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, (await Subir(admin, ruta, Guid.NewGuid(), "logo.svg", "image/svg+xml", "<svg/>"u8.ToArray())).StatusCode);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, (await Subir(admin, ruta, Guid.NewGuid(), contenido: new byte[2 * 1024 * 1024 + 1])).StatusCode);
        Assert.Equal(0, app.Storage.Intentos);
    }

    [Fact]
    public async Task CargaPendienteNoPisaEdicionPosteriorNiSeReutilizaEnOtraMarca()
    {
        using var app = new App(); using var admin = app.Cliente("admin");
        var marca = await CrearMarca(admin); var id = marca.GetProperty("id").GetGuid(); var operacion = Guid.NewGuid();
        var ruta = $"/api/administracion/marcas/{id}/logo";
        app.Storage.Fallar = true;
        Assert.Equal(HttpStatusCode.BadGateway, (await Subir(admin,ruta,operacion)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await admin.PutAsJsonAsync("/api/administracion/marcas/"+id,new { nombre="Marca editada",descripcion=(string?)null,url=(string?)null,
            activo=true,orden=5,version=marca.GetProperty("version").GetGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await Subir(admin,ruta,operacion)).StatusCode);
        Assert.Equal(1,app.Storage.Intentos);
        var otra = (await CrearMarca(admin)).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Conflict,(await Subir(admin,$"/api/administracion/marcas/{otra}/logo",operacion)).StatusCode);
        Assert.Equal(1,app.Storage.Intentos);
    }

    private static async Task<JsonElement> CrearMarca(HttpClient admin, bool activo = true)
    {
        var r = await admin.PostAsJsonAsync("/api/administracion/marcas", new { nombre = "Marca prueba", descripcion = "Equipo", url = (string?)null, activo, orden = 5 });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode); return await r.Content.ReadFromJsonAsync<JsonElement>();
    }
    private static async Task<HttpResponseMessage> Subir(HttpClient cliente, string ruta, Guid id, string nombre = "logo.png", string mime = "image/png", byte[]? contenido = null)
    {
        using var form = new MultipartFormDataContent(); form.Add(new StringContent(id.ToString()), "idOperacion");
        var archivo = new ByteArrayContent(contenido ?? ImagenesTests.Png); archivo.Headers.ContentType = new(mime);
        form.Add(archivo, "archivo", nombre); return await cliente.PostAsync(ruta, form);
    }
    internal sealed class App : IDisposable
    {
        private readonly PerfilesFactory origen = new(); private readonly WebApplicationFactory<Program> app;
        public StorageSimulado Storage { get; } = new();
        public App() => app = origen.WithWebHostBuilder(b => b.ConfigureServices(s =>
        {
            s.RemoveAll<IImagenStorage>(); s.AddSingleton<IImagenStorage>(Storage);
            s.RemoveAll<ITransaccionesPersistence>(); s.AddScoped<ITransaccionesPersistence, TxLocal>();
        }));
        public HttpClient Cliente(string? actor)
        {
            var c = app.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
            if (actor is not null) c.DefaultRequestHeaders.Add("X-Identidad-Prueba", actor);
            c.DefaultRequestHeaders.Authorization = new("Bearer", "sesion-prueba"); return c;
        }
        public void Dispose() { app.Dispose(); origen.Dispose(); }
    }
    private sealed class TxLocal(PcpShootingDbContext db) : ITransaccionesPersistence
    { public async Task<T> EjecutarAsync<T>(Func<Task<T>> operacion, CancellationToken ct) { var r = await operacion(); await db.SaveChangesAsync(ct); return r; } }
    internal sealed class StorageSimulado : IImagenStorage
    {
        public int Intentos { get; private set; } public bool Fallar { get; set; }
        public string ObtenerUrlPublica(string ruta) => "https://imagenes.ejemplo.test/" + ruta;
        public Task SubirAsync(string ruta, ImagenValidada imagen, string token, CancellationToken ct)
        { Intentos++; if (Fallar) { Fallar = false; throw new OperacionException(502,"Fallo simulado"); } return Task.CompletedTask; }
    }
}
