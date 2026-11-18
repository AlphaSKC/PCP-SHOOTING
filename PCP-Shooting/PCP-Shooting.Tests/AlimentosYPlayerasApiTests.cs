using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class AlimentosYPlayerasApiTests : IClassFixture<PerfilesFactory>
{
    private readonly PerfilesFactory factory;
    private const string Notas = "- Talla **M**\n- Color negro\n\nNotas adicionales de ejemplo.";

    public AlimentosYPlayerasApiTests(PerfilesFactory factory)
    {
        this.factory = factory;
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>();
        if (db.Productos.Any()) return;
        db.Productos.AddRange(
            new Producto
            {
                Id = Guid.NewGuid(), Sku = "ALIMENTO-TEST", Nombre = "Alimento de ejemplo",
                Categoria = CategoriaProducto.Alimento, EsVendible = false
            },
            new Producto
            {
                Id = Guid.NewGuid(), Sku = "PLAYERA-TEST-M", Nombre = "Playera de ejemplo",
                Categoria = CategoriaProducto.Playera, EsVendible = true, Precio = 180,
                ImagenUrl = "https://example.com/playera.jpg", NotasMarkdown = Notas
            });
        db.SaveChanges();
    }

    [Theory]
    [InlineData(1, "ALIMENTO-TEST")]
    [InlineData(2, "PLAYERA-TEST-M")]
    public async Task CatalogoPublicoFiltraPorCategoria(int categoria, string sku)
    {
        using var client = factory.CrearCliente(null);
        var resultado = await client.GetFromJsonAsync<PaginaConsulta<ProductoCatalogo>>("/api/productos?categoria=" + categoria);
        Assert.Equal(sku, Assert.Single(resultado!.Elementos).Sku);
    }

    [Fact]
    public async Task FichaConservaImagenYMarkdownExacto()
    {
        using var client = factory.CrearCliente(null);
        var ficha = await client.GetFromJsonAsync<FichaProducto>("/api/productos/PLAYERA-TEST-M");
        Assert.Equal(CategoriaProducto.Playera, ficha!.Categoria);
        Assert.Equal("https://example.com/playera.jpg", ficha.ImagenUrl);
        Assert.Equal(Notas, ficha.NotasMarkdown);
    }

    [Fact]
    public async Task CatalogoAdministrativoIncluyeDatosDeLaPlayera()
    {
        using var admin = factory.CrearCliente("admin");
        var resultado = await admin.GetFromJsonAsync<PaginaConsulta<ProductoGestion>>("/api/administracion/productos?categoria=2");
        var producto = Assert.Single(resultado!.Elementos);
        Assert.Equal(Notas, producto.NotasMarkdown);
        Assert.Equal("https://example.com/playera.jpg", producto.ImagenUrl);
    }

    [Fact]
    public async Task CategoriaDesconocidaSeRechaza()
    {
        using var client = factory.CrearCliente(null);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/productos?categoria=99")).StatusCode);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:image/png;base64,AAA")]
    [InlineData("ftp://example.com/imagen.jpg")]
    public async Task ImagenRequiereUrlHttpOhttps(string imagen)
    {
        using var admin = factory.CrearCliente("admin");
        var response = await admin.PostAsJsonAsync("/api/productos", new
        {
            sku = "IMAGEN-INVALIDA", nombre = "Playera", categoria = 2, imagenUrl = imagen
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LimiteDeNotasSeValida()
    {
        using var admin = factory.CrearCliente("admin");
        var response = await admin.PostAsJsonAsync("/api/productos", new
        {
            sku = "NOTAS-INVALIDAS", nombre = "Playera", categoria = 2,
            notasMarkdown = new string('x', 10001)
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
