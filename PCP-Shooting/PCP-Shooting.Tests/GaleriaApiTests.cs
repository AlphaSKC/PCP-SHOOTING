using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class GaleriaApiTests
{
    [Fact]
    public async Task ReintentarFichaConMismoIdNoDuplicaFotos()
    {
        using var app = new PerfilesFactory(); using var admin = app.CrearCliente("admin");
        var a = await admin.PostAsJsonAsync("/api/administracion/galeria/albums", new { nombre = "Club" });
        var id = (await a.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var datos = new { idOperacion = Guid.NewGuid(), titulo = "Foto", textoAlternativo = "Vista" };
        var primero = await admin.PostAsJsonAsync($"/api/administracion/galeria/albums/{id}/fotos", datos);
        var segundo = await admin.PostAsJsonAsync($"/api/administracion/galeria/albums/{id}/fotos", datos);
        Assert.Equal((await primero.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid(),
            (await segundo.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid());
    }
    [Fact]
    public async Task AdminGestionaAlbumYFotoSinPublicarUnaImagenInexistente()
    {
        using var app = new PerfilesFactory();
        using var admin = app.CrearCliente("admin");
        var crear = await admin.PostAsJsonAsync("/api/administracion/galeria/albums", new { nombre = "Club", orden = 0, publicado = false });
        Assert.Equal(HttpStatusCode.Created, crear.StatusCode);
        var album = await crear.Content.ReadFromJsonAsync<JsonElement>();
        var id = album.GetProperty("id").GetGuid();
        var fotoResponse = await admin.PostAsJsonAsync($"/api/administracion/galeria/albums/{id}/fotos", new { titulo = "Foto", textoAlternativo = "Vista del club", orden = 0, publicada = false });
        Assert.Equal(HttpStatusCode.Created, fotoResponse.StatusCode);
        var foto = await fotoResponse.Content.ReadFromJsonAsync<JsonElement>();
        var fotoId = foto.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync($"/api/administracion/galeria/fotos/{fotoId}", new {
            titulo = "Foto", textoAlternativo = "Vista", orden = 0, publicada = true, version = foto.GetProperty("version").GetGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync($"/api/administracion/galeria/albums/{id}", new { nombre = "Club", orden = 1, publicado = true, version = Guid.NewGuid() })).StatusCode);
        using var publico = app.CrearCliente(null);
        var albums = await publico.GetFromJsonAsync<JsonElement>("/api/galeria/albums");
        Assert.Equal(0, albums.GetProperty("total").GetInt32());
        var fotos = await publico.GetFromJsonAsync<JsonElement>($"/api/galeria/fotos?albumId={id}");
        Assert.Equal(0, fotos.GetProperty("total").GetInt32());
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("cliente", HttpStatusCode.Forbidden)]
    [InlineData("recepcion", HttpStatusCode.Forbidden)]
    public async Task SoloAdminGestiona(string? identidad, HttpStatusCode esperado)
    {
        using var app = new PerfilesFactory();
        using var cliente = app.CrearCliente(identidad);
        Assert.Equal(esperado, (await cliente.GetAsync("/api/administracion/galeria/albums")).StatusCode);
    }
}
