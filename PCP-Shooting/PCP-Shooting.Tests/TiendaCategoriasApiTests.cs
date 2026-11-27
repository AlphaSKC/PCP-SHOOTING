using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class TiendaCategoriasApiTests
{
    private const string Ruta = "/api/administracion/tienda/categorias";
    internal static object Datos(string slug = "ropa", Guid? version = null, bool activa = true) =>
        new { nombre = "Ropa", slug, descripcion = "Artículos del club", orden = 0, activa, version };

    [Fact]
    public async Task GestionaCategoriaConVersionYConservaCatalogoPublicoSinBorradoDeAsignadas()
    {
        using var app = new ContenidoImagenesApiTests.App(); using var admin = app.Cliente("admin");
        var creado = await admin.PostAsJsonAsync(Ruta, Datos()); Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
        var categoria = await creado.Content.ReadFromJsonAsync<JsonElement>(); var id = categoria.GetProperty("id").GetGuid();
        Assert.Single((await admin.GetFromJsonAsync<JsonElement>("/api/tienda/categorias")).EnumerateArray());
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(Ruta, Datos())).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(Ruta, Datos("carrito"))).StatusCode);
        var version = categoria.GetProperty("version").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"{Ruta}/{id}", Datos(version: version, activa: false))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync($"{Ruta}/{id}", Datos(version: version))).StatusCode);
        Assert.Empty((await admin.GetFromJsonAsync<JsonElement>("/api/tienda/categorias")).EnumerateArray());
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"{Ruta}/{id}")).StatusCode);
    }

    [Fact]
    public async Task ProductoRelacionaCategoriaYMarcaFiltraPorSlugsYSuCarruselEsIndependiente()
    {
        using var app = new ContenidoImagenesApiTests.App(); using var admin = app.Cliente("admin");
        var categoria = await (await admin.PostAsJsonAsync(Ruta, Datos())).Content.ReadFromJsonAsync<JsonElement>();
        var categoriaId = categoria.GetProperty("id").GetGuid();
        var marca = await (await admin.PostAsJsonAsync("/api/administracion/marcas", new { nombre = "Club Wear", slug = "club-wear", visibleEnCarrusel = false, activo = true })).Content.ReadFromJsonAsync<JsonElement>();
        var marcaId = marca.GetProperty("id").GetGuid();
        var datos = new { sku = "F24-ROPA", nombre = "Playera", categoria = 2, precio = 200, esVendible = true, categoriaTiendaId = categoriaId, marcaId };
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync("/api/productos", datos)).StatusCode);
        var listado = await admin.GetFromJsonAsync<JsonElement>("/api/productos?categoriaTienda=ropa&marca=club-wear");
        var producto = Assert.Single(listado.GetProperty("elementos").EnumerateArray());
        Assert.Equal(categoriaId, producto.GetProperty("categoriaTienda").GetProperty("id").GetGuid());
        Assert.Equal("club-wear", producto.GetProperty("marca").GetProperty("slug").GetString());
        Assert.Equal(marcaId, (await admin.GetFromJsonAsync<JsonElement>("/api/productos/F24-ROPA")).GetProperty("marca").GetProperty("id").GetGuid());
        Assert.Equal(categoriaId, Assert.Single((await admin.GetFromJsonAsync<JsonElement>("/api/administracion/productos?busqueda=F24-ROPA")).GetProperty("elementos").EnumerateArray()).GetProperty("categoriaTiendaId").GetGuid());
        Assert.DoesNotContain((await admin.GetFromJsonAsync<JsonElement>("/api/marcas")).EnumerateArray(), m => m.GetProperty("id").GetGuid() == marcaId);
        Assert.Equal(1, Assert.Single((await admin.GetFromJsonAsync<JsonElement>("/api/tienda/categorias")).EnumerateArray()).GetProperty("cantidadProductos").GetInt32());
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/administracion/marcas/{marcaId}", new { nombre = "Club Wear", slug = "club-wear", activo = false, version = marca.GetProperty("version").GetGuid() })).StatusCode);
        Assert.Equal(JsonValueKind.Null, (await admin.GetFromJsonAsync<JsonElement>("/api/productos/F24-ROPA")).GetProperty("marca").ValueKind);
        Assert.Equal(0, (await admin.GetFromJsonAsync<JsonElement>("/api/productos?marca=club-wear")).GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.OK, (await admin.PatchAsJsonAsync($"/api/productos/{producto.GetProperty("id").GetGuid()}/estado", new { activo = false })).StatusCode);
        Assert.Equal(0, Assert.Single((await admin.GetFromJsonAsync<JsonElement>("/api/tienda/categorias")).EnumerateArray()).GetProperty("cantidadProductos").GetInt32());
        Assert.Equal(1, Assert.Single((await admin.GetFromJsonAsync<JsonElement>(Ruta)).EnumerateArray()).GetProperty("cantidadProductos").GetInt32());
        Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"{Ruta}/{categoriaId}")).StatusCode);
        Assert.Equal(0, (await admin.GetFromJsonAsync<JsonElement>("/api/productos?marca=no-existe")).GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/productos", new { sku = "F24-MALO", nombre = "Otro", categoriaTiendaId = Guid.NewGuid() })).StatusCode);
    }

    [Fact]
    public async Task GeneraSlugsUnicosDesdeNombresYConservaPreferenciaDeCarruselAlEditarSinCampo()
    {
        using var app = new ContenidoImagenesApiTests.App(); using var admin = app.Cliente("admin");
        var uno = await (await admin.PostAsJsonAsync("/api/administracion/marcas", new { nombre = "Ácme & Club", visibleEnCarrusel = false })).Content.ReadFromJsonAsync<JsonElement>();
        var dos = await (await admin.PostAsJsonAsync("/api/administracion/marcas", new { nombre = "Ácme & Club" })).Content.ReadFromJsonAsync<JsonElement>();
        var slug = uno.GetProperty("slug").GetString();
        Assert.StartsWith("acme-club-", slug); Assert.NotEqual(slug, dos.GetProperty("slug").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/api/administracion/marcas", new { nombre = "Otra", slug })).StatusCode);
        var id = uno.GetProperty("id").GetGuid();
        var editado = await admin.PutAsJsonAsync($"/api/administracion/marcas/{id}", new { nombre = "Nuevo nombre", version = uno.GetProperty("version").GetGuid() });
        Assert.Equal(HttpStatusCode.OK, editado.StatusCode);
        var detalle = await editado.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(detalle.GetProperty("visibleEnCarrusel").GetBoolean()); Assert.Equal(slug, detalle.GetProperty("slug").GetString());
    }

    [Fact]
    public async Task ImagenReintentaSinDuplicarYEliminarCategoriaVaciaConservaArchivoPublico()
    {
        using var app = new ContenidoImagenesApiTests.App(); using var admin = app.Cliente("admin");
        var categoria = await (await admin.PostAsJsonAsync(Ruta, Datos())).Content.ReadFromJsonAsync<JsonElement>();
        var id = categoria.GetProperty("id").GetGuid(); var operacion = Guid.NewGuid();
        async Task<HttpResponseMessage> Subir()
        {
            using var form = new MultipartFormDataContent(); form.Add(new StringContent(operacion.ToString()), "idOperacion");
            var archivo = new ByteArrayContent(ImagenesTests.Png); archivo.Headers.ContentType = new("image/png");
            form.Add(archivo, "archivo", "categoria.png"); return await admin.PostAsync($"{Ruta}/{id}/imagen", form);
        }
        app.Storage.Fallar = true;
        Assert.Equal(HttpStatusCode.BadGateway, (await Subir()).StatusCode);
        var primera = await Subir(); Assert.Equal(HttpStatusCode.OK, primera.StatusCode);
        var imagen = await primera.Content.ReadFromJsonAsync<JsonElement>(); Assert.Contains("tienda/", imagen.GetProperty("imagenUrl").GetString());
        Assert.Equal(HttpStatusCode.OK, (await Subir()).StatusCode);
        Assert.Equal(2, app.Storage.Intentos);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"{Ruta}/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Subir()).StatusCode);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("cliente", HttpStatusCode.Forbidden)]
    [InlineData("recepcion", HttpStatusCode.Forbidden)]
    public async Task SoloAdministradorGestiona(string? actor, HttpStatusCode esperado)
    {
        using var app = new PerfilesFactory(); using var c = app.CrearCliente(actor);
        Assert.Equal(esperado, (await c.GetAsync(Ruta)).StatusCode);
        Assert.Equal(esperado, (await c.PostAsJsonAsync(Ruta, Datos())).StatusCode);
    }
}
