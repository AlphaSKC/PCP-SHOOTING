using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class CatalogoApiTests : IClassFixture<PerfilesFactory>
{
    private readonly PerfilesFactory factory;
    private static readonly Guid DisponibleId = Guid.NewGuid();
    private static readonly Guid AgotadoId = Guid.NewGuid();

    public CatalogoApiTests(PerfilesFactory factory)
    {
        this.factory = factory;
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>();
        if (db.Productos.Any()) return;
        var suficientes = Guid.NewGuid();
        db.Productos.AddRange(
            new Producto { Id = DisponibleId, Sku = "CAT-001", Nombre = "A Camiseta", EsVendible = true, Precio = 10, StockMinimo = 2 },
            new Producto { Id = suficientes, Sku = "CAT-002", Nombre = "B Gorra", EsVendible = true, Precio = 30, StockMinimo = 1 },
            new Producto { Id = Guid.NewGuid(), Sku = "CAT-003", Nombre = "C Ficha", EsVendible = false },
            new Producto { Id = Guid.NewGuid(), Sku = "CAT-004", Nombre = "D Oculto", Activo = false, EsVendible = true, Precio = 20, StockMinimo = 10 },
            new Producto { Id = AgotadoId, Sku = "CAT-005", Nombre = "E Agotado", EsVendible = true, Precio = 11, StockMinimo = 0 },
            new Producto { Id = Guid.NewGuid(), Sku = "CAT-006", Nombre = "F Sin minimo", EsVendible = true, Precio = 12 });
        db.MovimientosInventario.AddRange(
            new MovimientoInventario
            {
                Id = Guid.NewGuid(), ProductoId = DisponibleId, Tipo = TipoMovimientoInventario.Entrada,
                VariacionCantidad = 2, RegistradoEnUtc = DateTime.UtcNow
            },
            new MovimientoInventario
            {
                Id = Guid.NewGuid(), ProductoId = suficientes, Tipo = TipoMovimientoInventario.Entrada,
                VariacionCantidad = 5, RegistradoEnUtc = DateTime.UtcNow
            });
        db.SaveChanges();
    }

    [Fact]
    public async Task CatalogoPublicoPaginaYExcluyeInactivos()
    {
        using var client = factory.CrearCliente(null);
        var pagina = await client.GetFromJsonAsync<PaginaConsulta<ProductoCatalogo>>("/api/productos?tamanoPagina=2&pagina=2&activo=false");
        Assert.Equal(5, pagina!.Total);
        Assert.Equal(new[] { "CAT-003", "CAT-005" }, pagina.Elementos.Select(p => p.Sku));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/productos/CAT-004")).StatusCode);
    }

    [Fact]
    public async Task CatalogoFiltraPorTextoPrecioYDisponibilidadDeVenta()
    {
        using var client = factory.CrearCliente(null);
        var texto = await client.GetFromJsonAsync<PaginaConsulta<ProductoCatalogo>>("/api/productos?busqueda=CAMISETA");
        Assert.Equal(DisponibleId, Assert.Single(texto!.Elementos).Id);
        var precios = await client.GetFromJsonAsync<PaginaConsulta<ProductoCatalogo>>(
            "/api/productos?esVendible=true&precioMinimo=10&precioMaximo=12");
        Assert.Equal(3, precios!.Total);
        var informativos = await client.GetFromJsonAsync<PaginaConsulta<ProductoCatalogo>>("/api/productos?esVendible=false");
        Assert.Null(Assert.Single(informativos!.Elementos).Precio);
    }

    [Fact]
    public async Task CatalogoPaginaMuyAltaConservaTotal()
    {
        using var client = factory.CrearCliente(null);
        var pagina = await client.GetFromJsonAsync<PaginaConsulta<ProductoCatalogo>>("/api/productos?pagina=2147483647&tamanoPagina=100");
        Assert.Equal(5, pagina!.Total);
        Assert.Empty(pagina.Elementos);
    }

    [Fact]
    public async Task AdministradorConsultaLosInactivos()
    {
        using var admin = factory.CrearCliente("admin");
        var pagina = await admin.GetFromJsonAsync<PaginaConsulta<ProductoGestion>>("/api/administracion/productos?activo=false");
        var producto = Assert.Single(pagina!.Elementos);
        Assert.Equal("CAT-004", producto.Sku);
        Assert.False(producto.Activo);
    }

    [Fact]
    public async Task AlertasIncluyenElLimiteYElAgotadoConMinimoCero()
    {
        using var admin = factory.CrearCliente("admin");
        var pagina = await admin.GetFromJsonAsync<PaginaConsulta<AlertaInventario>>("/api/inventario/alertas");
        Assert.Equal(2, pagina!.Total);
        Assert.Equal(AgotadoId, pagina.Elementos[0].ProductoId);
        Assert.Contains(pagina.Elementos, p => p.ProductoId == DisponibleId && p.Existencia == p.StockMinimo);
    }

    [Theory]
    [InlineData("pagina=0")]
    [InlineData("tamanoPagina=101")]
    [InlineData("precioMinimo=-1")]
    [InlineData("precioMinimo=20&precioMaximo=10")]
    public async Task CatalogoRechazaFiltrosInvalidos(string filtro)
    {
        using var client = factory.CrearCliente(null);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/productos?" + filtro)).StatusCode);
    }

    [Theory]
    [InlineData("/api/administracion/productos")]
    [InlineData("/api/inventario/alertas")]
    public async Task ConsultasInternasRechazanClientes(string ruta)
    {
        using var client = factory.CrearCliente("cliente");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(ruta)).StatusCode);
    }

    [Fact]
    public async Task ClienteNoPuedeDesactivarNiCambiarMinimos()
    {
        using var client = factory.CrearCliente("cliente");
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PatchAsJsonAsync($"/api/productos/{DisponibleId}/estado", new { activo = false })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PutAsJsonAsync($"/api/inventario/{DisponibleId}/minimo", new { stockMinimo = 1 })).StatusCode);
    }

    [Fact]
    public async Task AdministradorDebeEnviarElCampoDelEstadoYUnMinimoValido()
    {
        using var admin = factory.CrearCliente("admin");
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.PatchAsJsonAsync($"/api/productos/{DisponibleId}/estado", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.PutAsJsonAsync($"/api/inventario/{DisponibleId}/minimo", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.PutAsJsonAsync($"/api/inventario/{DisponibleId}/minimo", new { stockMinimo = -1 })).StatusCode);
    }
}
