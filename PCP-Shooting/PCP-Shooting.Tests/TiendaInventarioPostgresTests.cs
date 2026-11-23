using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Domain.Services;
using PCP_Shooting.Persistence.Models;
using PCP_Shooting.Persistence.Services;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class TiendaInventarioPostgresTests(PostgresEscriturasFixture fixture) : IClassFixture<PostgresEscriturasFixture>
{
    [PostgresFact]
    public async Task StockAutorYReportesFuncionanConTransaccionesYConsultasReales()
    {
        var actorId = Guid.NewGuid();
        var acceso = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(
            new ClaimsIdentity(new[] { new Claim("sub", actorId.ToString()) }, "Prueba")) } };
        await using var db = fixture.CrearContext(acceso);
        db.PerfilesUsuario.Add(new PerfilUsuario { Id = actorId, Nombre = "Cliente prueba stock" });
        var producto = new Producto { Id = Guid.NewGuid(), Sku = "STOCK-REAL", Nombre = "Playera de prueba", Precio = 100,
            EsVendible = true, Categoria = CategoriaProducto.Playera };
        db.Productos.Add(producto); await db.SaveChangesAsync();
        var tx = new TransaccionesPersistence(db);
        var servicio = new EscriturasService(db, tx);
        await servicio.RegistrarMovimientoAsync(producto.Id, new RegistrarMovimiento { IdOperacion = Guid.NewGuid(),
            Tipo = TipoMovimientoInventario.Entrada, VariacionCantidad = 2, Motivo = "Entrada de prueba" }, default);
        var stock = Assert.Single((await new ProductosPersistence(db).ListarCatalogoAsync(new FiltroProductos { Busqueda = producto.Sku }, default)).Elementos);
        Assert.Equal(2, stock.Existencia); Assert.False(stock.Agotado);
        var historial = await new InventarioPersistence(db).ConsultarMovimientosAsync(new FiltroMovimientosInventario { ProductoId = producto.Id,
            ActorId = actorId, Tipo = TipoMovimientoInventario.Entrada, DesdeUtc = DateTimeOffset.UtcNow.AddDays(-1) }, default);
        Assert.Equal("Cliente prueba stock", Assert.Single(historial.Elementos).ActorNombre);
        CrearPedido Datos(int cantidad) => new() { IdOperacion = Guid.NewGuid(), Partidas = [new() { ProductoId = producto.Id, Cantidad = cantidad }] };
        var error = await Assert.ThrowsAsync<OperacionException>(() => servicio.CrearPedidoAsync(actorId, Datos(3), default));
        Assert.Equal(409, error.Status);
        var pedido = await servicio.CrearPedidoAsync(actorId, Datos(2), default);
        Assert.Equal(2, await db.MovimientosInventario.SumAsync(m => m.VariacionCantidad));
        var reporte = await new ReportesService(db).PedidosAsync(new FiltroPedidos { UsuarioId = actorId }, true, default);
        var fila = Assert.Single(reporte.Datos.Elementos);
        Assert.Equal("Cliente prueba stock", fila.NombreCliente);
        Assert.Equal("Playera de prueba", Assert.Single(fila.Productos));
        Assert.Equal(pedido.Id.ToString("N")[..8].ToUpperInvariant(), fila.Folio);
        var pago = await servicio.RegistrarPagoAsync(pedido.Id, new RegistrarPago { IdOperacion = Guid.NewGuid(), ReferenciaExterna = "PAGO-STOCK" }, default);
        await servicio.ConfirmarPagoAsync(pago.Id, default);
        await servicio.ConfirmarPagoAsync(pago.Id, default);
        stock = Assert.Single((await new ProductosPersistence(db).ListarCatalogoAsync(new FiltroProductos { Busqueda = producto.Sku }, default)).Elementos);
        Assert.Equal(0, stock.Existencia); Assert.True(stock.Agotado);
        var pagos = await new ReportesService(db).PagosAsync(new FiltroReportePagos { UsuarioId = actorId }, false, default);
        Assert.Equal("Cliente prueba stock", Assert.Single(pagos.Datos.Elementos).NombreCliente);
    }
}
