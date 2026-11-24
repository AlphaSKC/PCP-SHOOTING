using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class InventarioGeneralApiTests : IClassFixture<PerfilesFactory>
{
    private readonly PerfilesFactory factory;
    public InventarioGeneralApiTests(PerfilesFactory factory)
    {
        this.factory = factory;
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>();
        if (db.Productos.Any()) return;
        var productos = new[]
        {
            new Producto { Id = Guid.NewGuid(), Sku = "INV-01", Nombre = "A Alimento", Categoria = CategoriaProducto.Alimento, EsVendible = true, Precio = 10, StockMinimo = 3 },
            new Producto { Id = Guid.NewGuid(), Sku = "INV-02", Nombre = "B Playera", Categoria = CategoriaProducto.Playera, EsVendible = true, Precio = 20, StockMinimo = 2 },
            new Producto { Id = Guid.NewGuid(), Sku = "INV-03", Nombre = "C Inactivo", Categoria = CategoriaProducto.Alimento, Activo = false },
            new Producto { Id = Guid.NewGuid(), Sku = "INV-04", Nombre = "D Decoracion", Categoria = CategoriaProducto.ReplicaDecorativa, StockMinimo = 0 },
            new Producto { Id = Guid.NewGuid(), Sku = "INV-05", Nombre = "E Saldo amplio" }
        };
        db.Productos.AddRange(productos);
        void Movimiento(int indice, int cantidad) => db.MovimientosInventario.Add(new MovimientoInventario
        {
            Id = Guid.NewGuid(), ProductoId = productos[indice].Id, VariacionCantidad = cantidad,
            Tipo = cantidad > 0 ? TipoMovimientoInventario.Entrada : TipoMovimientoInventario.Salida,
            RegistradoEnUtc = DateTime.UtcNow
        });
        Movimiento(0, 3);
        Movimiento(1, 8);
        Movimiento(1, -2);
        Movimiento(1, 1);
        Movimiento(4, int.MaxValue);
        Movimiento(4, int.MaxValue);
        Movimiento(4, -1);
        db.SaveChanges();
    }

    [Fact]
    public async Task ListaSaldosIncluyendoCeroYProductosInactivos()
    {
        using var admin = factory.CrearCliente("admin");
        var pagina = await admin.GetFromJsonAsync<PaginaConsulta<ExistenciaProducto>>("/api/inventario");
        Assert.Equal(5, pagina!.Total);
        Assert.Equal(7, pagina.Elementos.Single(p => p.Sku == "INV-02").Existencia);
        var inactivo = pagina.Elementos.Single(p => p.Sku == "INV-03");
        Assert.False(inactivo.Activo);
        Assert.Equal(0, inactivo.Existencia);
        Assert.Null(inactivo.StockMinimo);
        Assert.False(inactivo.BajoMinimo);
        Assert.Equal((long)int.MaxValue * 2 - 1, pagina.Elementos.Single(p => p.Sku == "INV-05").Existencia);
    }

    [Fact]
    public async Task PaginaConOrdenEstableYTotal()
    {
        using var admin = factory.CrearCliente("admin");
        var primera = await admin.GetFromJsonAsync<PaginaConsulta<ExistenciaProducto>>("/api/inventario?tamanoPagina=2&pagina=1");
        var segunda = await admin.GetFromJsonAsync<PaginaConsulta<ExistenciaProducto>>("/api/inventario?tamanoPagina=2&pagina=2");
        Assert.Equal(5, primera!.Total);
        Assert.Equal(5, segunda!.Total);
        Assert.Equal(new[] { "INV-01", "INV-02" }, primera.Elementos.Select(p => p.Sku));
        Assert.Equal(new[] { "INV-03", "INV-04" }, segunda.Elementos.Select(p => p.Sku));
        var fuera = await admin.GetFromJsonAsync<PaginaConsulta<ExistenciaProducto>>("/api/inventario?pagina=2147483647&tamanoPagina=100");
        Assert.Equal(5, fuera!.Total);
        Assert.Empty(fuera.Elementos);
    }

    [Theory]
    [InlineData("busqueda=playera", 1)]
    [InlineData("busqueda=inv-02", 1)]
    [InlineData("categoria=1&activo=false", 1)]
    [InlineData("conExistencias=false", 2)]
    [InlineData("conExistencias=true", 3)]
    [InlineData("bajoMinimo=true", 2)]
    [InlineData("bajoMinimo=false", 3)]
    [InlineData("esVendible=true", 2)]
    public async Task AplicaFiltrosAntesDePaginar(string filtro, int total)
    {
        using var admin = factory.CrearCliente("admin");
        var pagina = await admin.GetFromJsonAsync<PaginaConsulta<ExistenciaProducto>>("/api/inventario?" + filtro);
        Assert.Equal(total, pagina!.Total);
        Assert.Equal(total, pagina.Elementos.Count);
    }

    [Fact]
    public async Task ElMinimoIncluyeIgualdadYCeroConfigurado()
    {
        using var admin = factory.CrearCliente("admin");
        var pagina = await admin.GetFromJsonAsync<PaginaConsulta<ExistenciaProducto>>("/api/inventario?bajoMinimo=true");
        Assert.Contains(pagina!.Elementos, p => p.Sku == "INV-01" && p.Existencia == p.StockMinimo);
        Assert.Contains(pagina.Elementos, p => p.Sku == "INV-04" && p.Existencia == 0 && p.StockMinimo == 0);
    }

    [Theory]
    [InlineData("pagina=0")]
    [InlineData("tamanoPagina=101")]
    [InlineData("categoria=99")]
    [InlineData("activo=invalido")]
    public async Task FiltrosInvalidosDevuelven400(string filtro)
    {
        using var admin = factory.CrearCliente("admin");
        var response = await admin.GetAsync("/api/inventario?" + filtro);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task BusquedaLargaSeRechaza()
    {
        using var admin = factory.CrearCliente("admin");
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.GetAsync("/api/inventario?busqueda=" + new string('a', 151))).StatusCode);
    }
}

public sealed class InventarioRecepcionTests : IClassFixture<JwtBackendFactory>
{
    private readonly JwtBackendFactory factory;
    public InventarioRecepcionTests(JwtBackendFactory factory) => this.factory = factory;

    [Fact]
    public async Task RecepcionSoloConsultaSaldosDeAlimentos()
    {
        using var recepcion = factory.Cliente(factory.Token(JwtBackendFactory.RecepcionId));
        var pagina = await recepcion.GetFromJsonAsync<PaginaConsulta<ExistenciaProducto>>("/api/inventario");
        Assert.Equal(1, pagina!.Total);
        Assert.Equal(CategoriaProducto.Alimento, Assert.Single(pagina.Elementos).Categoria);
        Assert.Equal(HttpStatusCode.Forbidden, (await recepcion.GetAsync("/api/inventario?categoria=2")).StatusCode);
    }

    [Theory]
    [InlineData("cliente", HttpStatusCode.Forbidden)]
    [InlineData("inactivo", HttpStatusCode.Forbidden)]
    [InlineData("sin-token", HttpStatusCode.Unauthorized)]
    public async Task InventarioRequierePersonalActivo(string caso, HttpStatusCode esperado)
    {
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        if (caso != "sin-token")
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",
                factory.Token(caso == "inactivo" ? JwtBackendFactory.InactivoId : JwtBackendFactory.ClienteId));
        Assert.Equal(esperado, (await client.GetAsync("/api/inventario")).StatusCode);
    }
}
