using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class ReplicasDecorativasApiTests : IClassFixture<PerfilesFactory>
{
    private readonly PerfilesFactory factory;
    private const string Notas = "- Pieza decorativa **no funcional**\n- Uso en ambientación";

    public ReplicasDecorativasApiTests(PerfilesFactory factory)
    {
        this.factory = factory;
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>();
        if (db.Productos.Any(p => p.Sku == "DECORACION-TEST")) return;
        db.Productos.Add(new Producto
        {
            Id = Guid.NewGuid(), Sku = "DECORACION-TEST", Nombre = "Escultura decorativa",
            Categoria = CategoriaProducto.ReplicaDecorativa, ImagenUrl = "https://example.com/escultura.jpg",
            NotasMarkdown = Notas, EsVendible = false,
            EspecificacionesTecnicas = System.Text.Json.JsonSerializer.SerializeToElement(new
            {
                material = "Resina", altoCm = 30, anchoCm = 60
            })
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task CatalogoPublicoFiltraLasPiezasDecorativas()
    {
        using var client = factory.CrearCliente(null);
        var pagina = await client.GetFromJsonAsync<PaginaConsulta<ProductoCatalogo>>("/api/productos?categoria=3");
        var producto = Assert.Single(pagina!.Elementos);
        Assert.Equal(CategoriaProducto.ReplicaDecorativa, producto.Categoria);
        Assert.Equal(Notas, producto.NotasMarkdown);
        Assert.Equal("Resina", producto.EspecificacionesTecnicas!.Value.GetProperty("material").GetString());
        Assert.Null(producto.Precio);
    }

    [Fact]
    public async Task FichaYAdministracionConservanLaCategoriaYLaImagen()
    {
        using var publico = factory.CrearCliente(null);
        var ficha = await publico.GetFromJsonAsync<FichaProducto>("/api/productos/DECORACION-TEST");
        Assert.Equal(CategoriaProducto.ReplicaDecorativa, ficha!.Categoria);
        Assert.Equal("https://example.com/escultura.jpg", ficha.ImagenUrl);
        using var admin = factory.CrearCliente("admin");
        var pagina = await admin.GetFromJsonAsync<PaginaConsulta<ProductoGestion>>("/api/administracion/productos?categoria=3");
        Assert.Equal(ficha.Id, Assert.Single(pagina!.Elementos).Id);
    }
}
