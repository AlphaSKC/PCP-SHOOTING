using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Domain.Services;
using PCP_Shooting.Persistence.Services;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class ImagenesTests
{
    public static byte[] Png => Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aQr8AAAAASUVORK5CYII=");

    [Fact]
    public async Task ReconocePngYCalculaHuellaConExtensionMayuscula()
    {
        var imagen = await ValidadorImagen.ValidarAsync(new MemoryStream(Png), "pixel.PNG", "image/png", default);
        Assert.Equal(".png", imagen.Extension);
        Assert.Equal("image/png", imagen.TipoContenido);
        Assert.Equal(64, imagen.Sha256.Length);
        Assert.Equal(Png, imagen.Contenido);
    }

    [Theory]
    [InlineData("vacio", 400)]
    [InlineData("grande", 413)]
    [InlineData("extension", 415)]
    [InlineData("mime", 415)]
    [InlineData("svg", 415)]
    [InlineData("truncado", 415)]
    public async Task RechazaArchivosInvalidos(string caso, int status)
    {
        var contenido = caso switch
        {
            "vacio" => Array.Empty<byte>(),
            "grande" => new byte[ValidadorImagen.MaximoBytes + 1],
            "svg" => Encoding.UTF8.GetBytes("<svg xmlns='http://www.w3.org/2000/svg'></svg>"),
            "truncado" => Png[..20],
            _ => Png
        };
        var nombre = caso == "extension" ? "pixel.jpg" : caso == "svg" ? "pixel.svg" : "pixel.png";
        var mime = caso == "mime" ? "image/jpeg" : "image/png";
        var error = await Assert.ThrowsAsync<OperacionException>(() =>
            ValidadorImagen.ValidarAsync(new MemoryStream(contenido), nombre, mime, default));
        Assert.Equal(status, error.Status);
    }

    [Fact]
    public async Task StorageEnviaBytesTokenYNoSobrescribe()
    {
        var imagen = await ValidadorImagen.ValidarAsync(new MemoryStream(Png), "pixel.png", "image/png", default);
        var storage = Crear(async request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/storage/v1/object/pcp-productos/productos/uno/imagen.png", request.RequestUri!.AbsolutePath);
            Assert.Equal("sesion-prueba", request.Headers.Authorization!.Parameter);
            Assert.Equal("false", Assert.Single(request.Headers.GetValues("x-upsert")));
            Assert.Equal("image/png", request.Content!.Headers.ContentType!.MediaType);
            Assert.Equal(Png, await request.Content.ReadAsByteArrayAsync());
            return new HttpResponseMessage(HttpStatusCode.Created);
        });
        await storage.SubirAsync("productos/uno/imagen.png", imagen, "sesion-prueba", default);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DuplicadoSeVerificaPorHuella(bool diferente)
    {
        var imagen = await ValidadorImagen.ValidarAsync(new MemoryStream(Png), "pixel.png", "image/png", default);
        var peticiones = 0;
        var storage = Crear(request =>
        {
            peticiones++;
            if (request.Method == HttpMethod.Post)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(diferente ? new byte[] { 1, 2, 3 } : Png)
            });
        });
        if (diferente)
            Assert.Equal(409, (await Assert.ThrowsAsync<OperacionException>(() =>
                storage.SubirAsync("productos/uno/imagen.png", imagen, "prueba", default))).Status);
        else
            await storage.SubirAsync("productos/uno/imagen.png", imagen, "prueba", default);
        Assert.Equal(2, peticiones);
    }

    [Theory]
    [InlineData(401, 401)]
    [InlineData(403, 403)]
    [InlineData(500, 502)]
    public async Task FallosDeStorageDevuelvenErroresControlados(int remoto, int esperado)
    {
        var imagen = await ValidadorImagen.ValidarAsync(new MemoryStream(Png), "pixel.png", "image/png", default);
        var storage = Crear(_ => Task.FromResult(new HttpResponseMessage((HttpStatusCode)remoto)));
        Assert.Equal(esperado, (await Assert.ThrowsAsync<OperacionException>(() =>
            storage.SubirAsync("productos/uno/imagen.png", imagen, "prueba", default))).Status);
    }

    [Fact]
    public async Task SwaggerDescribeElFormularioDeSubida()
    {
        using var factory = new JwtBackendFactory { EnvironmentName = "Development" };
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("/api/productos/{id}/imagen", json);
        Assert.Contains("multipart/form-data", json);
    }
    [Fact]
    public async Task ProductoRelacionaVariasImagenesYConservaLaPrincipal()
    {
        var options = new DbContextOptionsBuilder<PcpShootingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new PcpShootingDbContext(options);
        var producto = new Producto
        {
            Id = Guid.NewGuid(), Sku = "GALERIA-PRUEBA", Nombre = "Playera",
            ImagenUrl = "https://example.com/principal.png"
        };
        foreach (var nombre in new[] { "principal.png", "detalle.png" })
            producto.Imagenes.Add(new ImagenProducto
            {
                Id = Guid.NewGuid(), CreadorId = Guid.NewGuid(),
                Ruta = "productos/" + producto.Id + "/" + nombre,
                Sha256 = new string('a', 64), TipoContenido = "image/png", Tamano = 68,
                Disponible = true, CreadaEnUtc = DateTime.UtcNow, SubidaEnUtc = DateTime.UtcNow
            });
        db.Productos.Add(producto);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var guardado = await db.Productos.Include(p => p.Imagenes).SingleAsync(p => p.Id == producto.Id);
        Assert.Equal(2, guardado.Imagenes.Count);
        Assert.All(guardado.Imagenes, imagen =>
        {
            Assert.Equal(guardado.Id, imagen.ProductoId);
            Assert.Same(guardado, imagen.Producto);
        });
        Assert.Equal("https://example.com/principal.png", guardado.ImagenUrl);
    }

    private static SupabaseImagenStorage Crear(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Supabase:Url"] = "https://prueba.supabase.co",
            ["Supabase:PublishableKey"] = "sb_publishable_prueba"
        }).Build();
        return new SupabaseImagenStorage(new HttpClient(new Transporte(handler)), config);
    }

    private sealed class Transporte(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => handler(request);
    }
}
