using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class AlimentosApiTests
{
    internal static async Task Sembrar(PcpShootingDbContext db)
    {
        var agua = new Producto { Id = Guid.NewGuid(), Sku = "ALI-AGUA", Nombre = "Agua", Categoria = CategoriaProducto.Alimento,
            Activo = true, EsVendible = true, Precio = 25, ImagenUrl = "https://example.com/agua.webp", NotasMarkdown = "- Botella **500 ml**" };
        db.Productos.AddRange(agua,
            new Producto { Id = Guid.NewGuid(), Sku = "ALI-CAFE", Nombre = "Café", Categoria = CategoriaProducto.Alimento, EsVendible = true, Precio = 35 },
            new Producto { Id = Guid.NewGuid(), Sku = "ALI-OCULTO", Nombre = "Oculto", Categoria = CategoriaProducto.Alimento, Activo = false, EsVendible = true, Precio = 10 },
            new Producto { Id = Guid.NewGuid(), Sku = "ALI-NOVENTA", Nombre = "No vendible", Categoria = CategoriaProducto.Alimento },
            new Producto { Id = Guid.NewGuid(), Sku = "ALI-PLAYERA", Nombre = "Playera", Categoria = CategoriaProducto.Playera, EsVendible = true, Precio = 200 },
            new Producto { Id = Guid.NewGuid(), Sku = "ALI-CONSUMIBLE", Nombre = "Consumible", Categoria = CategoriaProducto.Consumible });
        db.MovimientosInventario.Add(new MovimientoInventario { Id = Guid.NewGuid(), ProductoId = agua.Id, Tipo = TipoMovimientoInventario.Entrada, VariacionCantidad = 3, RegistradoEnUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task CartaPublicaReutilizaProductosPrecioImagenNotasYStockSinExponerGestion()
    {
        using var app = new PerfilesFactory();
        using (var scope = app.Services.CreateScope()) await Sembrar(scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>());
        using var c = app.CrearCliente(null);
        var pagina = await c.GetFromJsonAsync<JsonElement>("/api/alimentos");
        Assert.Equal(2, pagina.GetProperty("total").GetInt32());
        var agua = pagina.GetProperty("elementos")[0];
        Assert.Equal("ALI-AGUA", agua.GetProperty("sku").GetString());
        Assert.Equal(25, agua.GetProperty("precio").GetDecimal());
        Assert.Equal(3, agua.GetProperty("existencia").GetInt64());
        Assert.False(agua.GetProperty("agotado").GetBoolean());
        var cafe = pagina.GetProperty("elementos")[1];
        Assert.Equal(0, cafe.GetProperty("existencia").GetInt64()); Assert.True(cafe.GetProperty("agotado").GetBoolean());
        var detalle = await c.GetFromJsonAsync<JsonElement>("/api/alimentos/ALI-AGUA");
        Assert.Equal("https://example.com/agua.webp", detalle.GetProperty("imagenUrl").GetString());
        Assert.Contains("**500 ml**", detalle.GetProperty("notasMarkdown").GetString());
        Assert.Equal(25, detalle.GetProperty("precio").GetDecimal());
        Assert.Equal(3, detalle.GetProperty("existencia").GetInt64());
        Assert.False(detalle.TryGetProperty("stockMinimo", out _)); Assert.False(detalle.TryGetProperty("activo", out _));
        using var revisar = app.Services.CreateScope(); var db = revisar.ServiceProvider.GetRequiredService<PcpShootingDbContext>();
        Assert.Equal(6, db.Productos.Count()); Assert.Single(db.MovimientosInventario);
    }

    [Theory]
    [InlineData("ALI-OCULTO")]
    [InlineData("ALI-NOVENTA")]
    [InlineData("ALI-PLAYERA")]
    [InlineData("ALI-CONSUMIBLE")]
    [InlineData("NO-EXISTE")]
    public async Task DetalleNoFiltraOtrosProductosNiArticulosOcultos(string sku)
    {
        using var app = new PerfilesFactory();
        using (var scope = app.Services.CreateScope()) await Sembrar(scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>());
        using var c = app.CrearCliente(null);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/alimentos/{sku}")).StatusCode);
    }

    [Fact]
    public async Task RecepcionGestionaProductoEInventarioYLaCartaReflejaLosCambios()
    {
        using var origen = new PerfilesFactory();
        using var app = origen.WithWebHostBuilder(b => b.ConfigureServices(s => { s.RemoveAll<ITransaccionesPersistence>(); s.AddScoped<ITransaccionesPersistence, TransaccionLocal>(); }));
        using var recepcion = app.CreateClient(); recepcion.DefaultRequestHeaders.Add("X-Identidad-Prueba", "recepcion"); using var publico = app.CreateClient();
        Assert.Equal(0, (await publico.GetFromJsonAsync<JsonElement>("/api/alimentos")).GetProperty("total").GetInt32());
        var creado = await recepcion.PostAsJsonAsync("/api/productos", new { sku = "ALI-NUEVO", nombre = "Nuevo alimento", categoria = 1, esVendible = true, precio = 40 });
        Assert.Equal(HttpStatusCode.OK, creado.StatusCode);
        var id = (await creado.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await recepcion.PostAsJsonAsync($"/api/inventario/{id}/movimientos",
            new { idOperacion = Guid.NewGuid(), tipo = 0, variacionCantidad = 2, motivo = "Entrada de alimentos" })).StatusCode);
        var detalle = await publico.GetFromJsonAsync<JsonElement>("/api/alimentos/ALI-NUEVO");
        Assert.Equal(id, detalle.GetProperty("id").GetGuid()); Assert.Equal(2, detalle.GetProperty("existencia").GetInt64());
        Assert.Equal(HttpStatusCode.OK, (await recepcion.PatchAsJsonAsync($"/api/productos/{id}/estado", new { activo = false })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await publico.GetAsync("/api/alimentos/ALI-NUEVO")).StatusCode);
        Assert.Equal(0, (await publico.GetFromJsonAsync<JsonElement>("/api/alimentos")).GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task FiltrosYPaginacionOperanSobreLaCartaCompleta()
    {
        using var app = new PerfilesFactory();
        using (var scope = app.Services.CreateScope()) await Sembrar(scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>());
        using var c = app.CrearCliente(null);
        var pagina = await c.GetFromJsonAsync<JsonElement>("/api/alimentos?tamanoPagina=1&pagina=2");
        Assert.Equal(2, pagina.GetProperty("total").GetInt32());
        Assert.Equal("ALI-CAFE", Assert.Single(pagina.GetProperty("elementos").EnumerateArray()).GetProperty("sku").GetString());
        Assert.Equal(1, (await c.GetFromJsonAsync<JsonElement>("/api/alimentos?busqueda=AGUA&precioMinimo=25&precioMaximo=25")).GetProperty("total").GetInt32());
        Assert.Equal(0, (await c.GetFromJsonAsync<JsonElement>("/api/alimentos?pagina=2147483647")).GetProperty("elementos").GetArrayLength());
        foreach (var filtro in new[] { "pagina=0", "tamanoPagina=101", "precioMinimo=-1", "precioMinimo=50&precioMaximo=10" })
            Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync("/api/alimentos?" + filtro)).StatusCode);
    }

    private sealed class TransaccionLocal(PcpShootingDbContext db) : ITransaccionesPersistence
    {
        public async Task<T> EjecutarAsync<T>(Func<Task<T>> operacion, CancellationToken ct)
        {
            var resultado = await operacion(); await db.SaveChangesAsync(ct); return resultado;
        }
    }
}
