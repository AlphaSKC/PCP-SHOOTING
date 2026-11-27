using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
namespace PCP_Shooting.Tests;

public sealed class PortadasApiTests
{
    private const string Ruta = "/api/administracion/portadas";
    internal static object Datos(Guid? version = null, bool activa = true, string? imagen = "/img/hero-tirador.webp", string posicion = "50% 30%", string velo = "normal") => new { activa, imagenUrl = imagen, posicion, velo, version };
    [Fact]
    public async Task GuardaReutilizaImagenOcultaYProtegeVersionSinFiltrarMetadatos()
    {
        using var app = new ContenidoImagenesApiTests.App(); using var admin = app.Cliente("admin"); using var publico = app.Cliente(null);
        Assert.Empty((await publico.GetFromJsonAsync<JsonElement>("/api/portadas")).EnumerateArray());
        var respuesta = await admin.PutAsJsonAsync(Ruta + "/inicio", Datos()); Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var guardada = await respuesta.Content.ReadFromJsonAsync<JsonElement>(); var version = guardada.GetProperty("version").GetGuid();
        var datos = Datos(version, activa: false, imagen: null);
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync(Ruta + "/inicio", datos)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync(Ruta + "/inicio", datos)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync(Ruta + "/inicio", Datos())).StatusCode);
        var visible = Assert.Single((await publico.GetFromJsonAsync<JsonElement>("/api/portadas")).EnumerateArray());
        Assert.False(visible.GetProperty("activa").GetBoolean()); Assert.False(visible.TryGetProperty("version", out _));
        var url = "https://itucrxtwphlecugsbauf.supabase.co/storage/v1/object/public/pcp-portadas/portadas/inicio/prueba.png";
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync(Ruta + "/tienda", Datos(imagen: url))).StatusCode);
    }
    [Theory]
    [InlineData("https://evil.example/foto.jpg", "50% 50%", "normal")]
    [InlineData("//evil.example/foto.jpg", "50% 50%", "normal")]
    [InlineData("/img/../privado.png", "50% 50%", "normal")]
    [InlineData("/img/foto.png", "101% 50%", "normal")]
    [InlineData("/img/foto.png", "50% 50%", "oscuro")]
    public async Task ValidaUrlPosicionYVelo(string imagen, string posicion, string velo)
    {
        using var app = new ContenidoImagenesApiTests.App(); using var admin = app.Cliente("admin");
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync(Ruta + "/inicio", Datos(imagen: imagen, posicion: posicion, velo: velo))).StatusCode);
    }
    [Fact]
    public async Task CargaInicialFallaSinPublicarFilaYReintentaConOchoMibYVersion()
    {
        using var app = new ContenidoImagenesApiTests.App(); using var admin = app.Cliente("admin");
        var id = Guid.NewGuid();
        async Task<HttpResponseMessage> Subir() {
            using var form = new MultipartFormDataContent(); form.Add(new StringContent(id.ToString()), "idOperacion");
            var archivo = new ByteArrayContent(ImagenesTests.Png); archivo.Headers.ContentType = new("image/png"); form.Add(archivo,"archivo","foto.png");
            return await admin.PostAsync(Ruta + "/nosotros/imagen",form);
        }
        app.Storage.Fallar = true; Assert.Equal(HttpStatusCode.BadGateway, (await Subir()).StatusCode);
        Assert.Empty((await admin.GetFromJsonAsync<JsonElement>("/api/portadas")).EnumerateArray());
        var respuesta = await Subir(); Assert.Equal(HttpStatusCode.OK,respuesta.StatusCode);
        var imagen = await respuesta.Content.ReadFromJsonAsync<JsonElement>(); Assert.Contains("portadas/nosotros/",imagen.GetProperty("imagenUrl").GetString());
        Assert.Equal(HttpStatusCode.OK,(await Subir()).StatusCode); Assert.Equal(2,app.Storage.Intentos);
        Assert.Equal(HttpStatusCode.OK,(await admin.PutAsJsonAsync(Ruta + "/nosotros",Datos(imagen.GetProperty("version").GetGuid(),false))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await Subir()).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await admin.PutAsJsonAsync(Ruta + "/blog",Datos())).StatusCode);
    }
    [Theory]
    [InlineData(8388608,HttpStatusCode.OK)]
    [InlineData(8388609,HttpStatusCode.RequestEntityTooLarge)]
    public async Task LimiteDeArchivoEsOchoMib(int longitud,HttpStatusCode esperado)
    {
        using var app = new ContenidoImagenesApiTests.App(); using var admin = app.Cliente("admin");
        using var form = new MultipartFormDataContent(); form.Add(new StringContent(Guid.NewGuid().ToString()),"idOperacion");
        var contenido = new byte[longitud]; ImagenesTests.Png.CopyTo(contenido,0);
        Array.Copy(ImagenesTests.Png,ImagenesTests.Png.Length-12,contenido,longitud-12,12);
        var archivo = new ByteArrayContent(contenido); archivo.Headers.ContentType = new("image/png"); form.Add(archivo,"archivo","grande.png");
        Assert.Equal(esperado,(await admin.PostAsync(Ruta + "/inicio/imagen",form)).StatusCode);
    }

    [Theory]
    [InlineData(null,HttpStatusCode.Unauthorized)]
    [InlineData("cliente",HttpStatusCode.Forbidden)]
    [InlineData("recepcion",HttpStatusCode.Forbidden)]
    public async Task SoloAdministradorGestiona(string? actor,HttpStatusCode esperado)
    {
        using var app = new PerfilesFactory(); using var c = app.CrearCliente(actor);
        Assert.Equal(esperado,(await c.GetAsync(Ruta)).StatusCode);
        Assert.Equal(esperado,(await c.PutAsJsonAsync(Ruta + "/inicio",Datos())).StatusCode);
    }
}
