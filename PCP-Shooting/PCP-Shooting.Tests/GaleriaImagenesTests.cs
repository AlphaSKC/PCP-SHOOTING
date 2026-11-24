using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Interfaces;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class GaleriaImagenesTests
{
    [Fact]
    public async Task CargaReintentaPublicaYOcultarRetiraLaPortada()
    {
        using var app = new App(); using var admin = app.Cliente("admin");
        var (album, foto) = await Crear(admin); var id = foto.GetProperty("id").GetGuid(); var op = Guid.NewGuid();
        app.Storage.Fallar = true;
        Assert.Equal(HttpStatusCode.BadGateway, (await Subir(admin, id, op)).StatusCode);
        var carga = await Subir(admin, id, op); Assert.Equal(HttpStatusCode.OK, carga.StatusCode);
        var resultado = await carga.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.OK, (await Subir(admin, id, op)).StatusCode);
        Assert.Equal(2, app.Storage.Intentos);
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/administracion/galeria/fotos/{id}", new { titulo = "Club", textoAlternativo = "Vista", publicada = true, orden = 0, version = resultado.GetProperty("version").GetGuid() })).StatusCode);
        var publico = await admin.GetFromJsonAsync<JsonElement>("/api/galeria/albums");
        Assert.Equal(1, publico.GetProperty("total").GetInt32());
        var fotos = await admin.GetFromJsonAsync<JsonElement>("/api/galeria/fotos");
        Assert.Single(fotos.GetProperty("elementos").EnumerateArray());
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/administracion/galeria/albums/{album.GetProperty("id").GetGuid()}", new { nombre = "Club", publicado = false, orden = 0, version = album.GetProperty("version").GetGuid() })).StatusCode);
        Assert.Equal(0, (await admin.GetFromJsonAsync<JsonElement>("/api/galeria/fotos")).GetProperty("total").GetInt32());
        // El reintento de una operación ya completada no vuelve a publicar ni escribir versiones.
        Assert.Equal(HttpStatusCode.OK, (await Subir(admin, id, op)).StatusCode);
        Assert.Equal(2, app.Storage.Intentos);
    }

    [Fact]
    public async Task CargaViejaNoPisaEdicionPosteriorNiOtroDestino()
    {
        using var app = new App(); using var admin = app.Cliente("admin");
        var (_, foto) = await Crear(admin); var id = foto.GetProperty("id").GetGuid(); var op = Guid.NewGuid();
        app.Storage.Fallar = true; Assert.Equal(HttpStatusCode.BadGateway, (await Subir(admin, id, op)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/administracion/galeria/fotos/{id}", new { titulo = "Cambio", textoAlternativo = "Vista", orden = 0, version = foto.GetProperty("version").GetGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Subir(admin, id, op)).StatusCode);
        var (_, otra) = await Crear(admin);
        Assert.Equal(HttpStatusCode.Conflict, (await Subir(admin, otra.GetProperty("id").GetGuid(), op)).StatusCode);
        Assert.Equal(1, app.Storage.Intentos);
    }

    internal static async Task<(JsonElement Album, JsonElement Foto)> Crear(HttpClient admin)
    {
        var a = await admin.PostAsJsonAsync("/api/administracion/galeria/albums", new { nombre = "Club", publicado = true }); a.EnsureSuccessStatusCode();
        var album = await a.Content.ReadFromJsonAsync<JsonElement>();
        var f = await admin.PostAsJsonAsync($"/api/administracion/galeria/albums/{album.GetProperty("id").GetGuid()}/fotos", new { titulo = "Club", textoAlternativo = "Vista" }); f.EnsureSuccessStatusCode();
        return (album, await f.Content.ReadFromJsonAsync<JsonElement>());
    }
    internal static async Task<HttpResponseMessage> Subir(HttpClient cliente, Guid id, Guid operacion)
    {
        using var form = new MultipartFormDataContent(); form.Add(new StringContent(operacion.ToString()), "idOperacion");
        var archivo = new ByteArrayContent(ImagenesTests.Png); archivo.Headers.ContentType = new("image/png"); form.Add(archivo, "archivo", "foto.png");
        return await cliente.PostAsync($"/api/administracion/galeria/fotos/{id}/imagen", form);
    }
    private sealed class App : IDisposable
    {
        private readonly PerfilesFactory origen = new(); private readonly Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> app;
        public ContenidoImagenesApiTests.StorageSimulado Storage { get; } = new();
        public App() => app = origen.WithWebHostBuilder(b => b.ConfigureServices(s => {
            s.RemoveAll<IImagenStorage>(); s.AddSingleton<IImagenStorage>(Storage);
            s.RemoveAll<ITransaccionesPersistence>(); s.AddScoped<ITransaccionesPersistence, Tx>();
        }));
        public HttpClient Cliente(string actor) { var c = app.CreateClient(); c.DefaultRequestHeaders.Add("X-Identidad-Prueba", actor); c.DefaultRequestHeaders.Authorization = new("Bearer", "prueba"); return c; }
        public void Dispose() { app.Dispose(); origen.Dispose(); }
    }
    private sealed class Tx(PcpShootingDbContext db) : ITransaccionesPersistence
    { public async Task<T> EjecutarAsync<T>(Func<Task<T>> operacion, CancellationToken ct) { var r = await operacion(); await db.SaveChangesAsync(ct); return r; } }
}
