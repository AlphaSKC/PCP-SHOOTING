using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Domain.Services;
using PCP_Shooting.Persistence.Migrations;
using PCP_Shooting.Persistence.Models;
using PCP_Shooting.Persistence.Services;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class TallasPostgresTests(PostgresEscriturasFixture fixture) : IClassFixture<PostgresEscriturasFixture>
{
    [PostgresFact]
    public async Task MigracionConservaStockAnteriorYPagoDescuentaSoloTallasElegidas()
    {
        await using var db = fixture.CrearContext();
        var esquema = await db.Database.SqlQueryRaw<string>("SELECT current_schema() AS \"Value\"").SingleAsync();
        Assert.StartsWith("pcp_test_", esquema);
        var usuario = new PerfilUsuario { Id = Guid.NewGuid(), Nombre = "Cliente tallas" };
        var anterior = new Producto { Id = Guid.NewGuid(), Sku = "LEGADO", Nombre = "Playera anterior", Categoria = CategoriaProducto.Playera, Precio = 50, EsVendible = true };
        var pedidoAnterior = new Pedido { Id = Guid.NewGuid(), UsuarioId = usuario.Id, CreadoEnUtc = DateTime.UtcNow };
        pedidoAnterior.Detalles.Add(new DetallePedido { Id = Guid.NewGuid(), Producto = anterior, Cantidad = 1, PrecioUnitario = 50 });
        db.PerfilesUsuario.Add(usuario); db.Pedidos.Add(pedidoAnterior);
        db.MovimientosInventario.Add(new MovimientoInventario { Id = Guid.NewGuid(), Producto = anterior,
            VariacionCantidad = 2, Tipo = TipoMovimientoInventario.Entrada, Motivo = "Stock sin talla previo", RegistradoEnUtc = DateTime.UtcNow });
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        // Reconstruir la versión anterior solo dentro del esquema temporal ya comprobado.
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"Productos\" DROP COLUMN \"UsaTallas\"");
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"MovimientosInventario\" DROP COLUMN \"Talla\"");
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"DetallesPedido\" DROP COLUMN \"Talla\"");
        foreach (var comando in db.GetService<IMigrationsSqlGenerator>().Generate(new TallasPlayeras().UpOperations, db.Model))
            await db.Database.ExecuteSqlRawAsync(comando.CommandText);
        Assert.False((await db.Productos.SingleAsync(p => p.Id == anterior.Id)).UsaTallas);
        Assert.Null((await db.DetallesPedido.SingleAsync(d => d.PedidoId == pedidoAnterior.Id)).Talla);
        Assert.Null((await db.MovimientosInventario.SingleAsync(m => m.ProductoId == anterior.Id)).Talla);
        Assert.Equal(2, await db.MovimientosInventario.SumAsync(m => m.VariacionCantidad));

        var servicio = new EscriturasService(db, new TransaccionesPersistence(db));
        var producto = await servicio.GuardarProductoAsync(null, new GuardarProducto { Sku = "TALLAS", Nombre = "Playera negra",
            Categoria = CategoriaProducto.Playera, Precio = 125, EsVendible = true, UsaTallas = true }, default);
        foreach (var (talla,cantidad) in new[] { (TallaPlayera.S,2), (TallaPlayera.M,1), (TallaPlayera.L,3) })
            await servicio.RegistrarMovimientoAsync(producto.Id, new RegistrarMovimiento { IdOperacion = Guid.NewGuid(), Talla = talla,
                Tipo = TipoMovimientoInventario.Entrada, VariacionCantidad = cantidad, Motivo = "Entrada por talla" }, default);
        var datos = new CrearPedido { IdOperacion = Guid.NewGuid(), Partidas = [
            new() { ProductoId = producto.Id, Talla = TallaPlayera.S, Cantidad = 2 }, new() { ProductoId = producto.Id, Talla = TallaPlayera.M, Cantidad = 1 }] };
        var pedido = await servicio.CrearPedidoAsync(usuario.Id, datos, default);
        Assert.Equal(pedido.Id, (await servicio.CrearPedidoAsync(usuario.Id, datos, default)).Id);
        var productos = new ProductosPersistence(db);
        var catalogo = Assert.Single((await productos.ListarCatalogoAsync(new FiltroProductos { Busqueda = "TALLAS" }, default)).Elementos);
        Assert.Equal(6, catalogo.Existencia);
        Assert.Equal(4, catalogo.Tallas!.Count);
        var reporte = await new ReportesService(db, TimeProvider.System).PedidosAsync(new FiltroPedidos { UsuarioId = usuario.Id }, false, default);
        Assert.Contains("Playera negra · M", reporte.Datos.Elementos.Single(p => p.Id == pedido.Id).Productos);
        var pago = await servicio.RegistrarPagoAsync(pedido.Id, new RegistrarPago { IdOperacion = Guid.NewGuid(), ReferenciaExterna = "PAGO-TALLAS" }, default);
        await servicio.ConfirmarPagoAsync(pago.Id, default); await servicio.ConfirmarPagoAsync(pago.Id, default);
        var stock = await new InventarioPersistence(db).ConsultarTallasAsync(producto.Id, default);
        Assert.Equal(0, stock.Single(t => t.Talla == TallaPlayera.S).Existencia);
        Assert.Equal(0, stock.Single(t => t.Talla == TallaPlayera.M).Existencia);
        Assert.Equal(3, stock.Single(t => t.Talla == TallaPlayera.L).Existencia);
        var movimientos = await new InventarioPersistence(db).ConsultarMovimientosAsync(new FiltroMovimientosInventario { ProductoId = producto.Id, Talla = TallaPlayera.S }, default);
        Assert.Equal(2, movimientos.Total);
        Assert.All(movimientos.Elementos, m => Assert.Equal(TallaPlayera.S, m.Talla));
        var pagoAnterior = await servicio.RegistrarPagoAsync(pedidoAnterior.Id, new RegistrarPago { IdOperacion = Guid.NewGuid(), ReferenciaExterna = "PAGO-LEGADO" }, default);
        await servicio.ConfirmarPagoAsync(pagoAnterior.Id, default);
        Assert.Equal(1, await db.MovimientosInventario.Where(m => m.ProductoId == anterior.Id).SumAsync(m => m.VariacionCantidad));
        var detalle = await db.DetallesPedido.FirstAsync(d => d.PedidoId == pedido.Id);
        detalle.Talla = (TallaPlayera)99;
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
