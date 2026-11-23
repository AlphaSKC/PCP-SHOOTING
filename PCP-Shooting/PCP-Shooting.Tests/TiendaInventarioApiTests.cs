using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class TiendaInventarioApiTests
{
    [Fact]
    public async Task F08F11CatalogoAgotadoYCreacionNoPermiteSuperarStockSinDescontarlo()
    {
        using var app = new MercadoPagoApiTests.AppPago();
        var producto = Sembrar(app, 2);
        using var cliente = app.Cliente("cliente");
        var lista = await cliente.GetFromJsonAsync<JsonElement>("/api/productos?busqueda=F08");
        Assert.Equal(2, lista.GetProperty("elementos")[0].GetProperty("existencia").GetInt64());
        Assert.False(lista.GetProperty("elementos")[0].GetProperty("agotado").GetBoolean());
        object Pedido(Guid id, int cantidad) => new { idOperacion = id, partidas = new[] { new { productoId = producto, cantidad } } };
        Assert.Equal(HttpStatusCode.Conflict, (await cliente.PostAsJsonAsync("/api/pedidos", Pedido(Guid.NewGuid(), 3))).StatusCode);
        Assert.Equal(0, app.Leer(db => db.Pedidos.Count()));
        var id = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsJsonAsync("/api/pedidos", Pedido(id, 2))).StatusCode);
        Assert.Equal(2, app.Leer(db => db.MovimientosInventario.Sum(m => m.VariacionCantidad)));
        app.Leer(db => { db.MovimientosInventario.Add(new MovimientoInventario { Id = Guid.NewGuid(), ProductoId = producto,
            VariacionCantidad = -2, Tipo = TipoMovimientoInventario.Salida, Motivo = "Venta en local", RegistradoEnUtc = DateTime.UtcNow }); return db.SaveChanges(); });
        // Un reintento devuelve el pedido existente; no se convierte en una creación nueva.
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsJsonAsync("/api/pedidos", Pedido(id, 2))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await cliente.PostAsJsonAsync("/api/pedidos", Pedido(Guid.NewGuid(), 1))).StatusCode);
        lista = await cliente.GetFromJsonAsync<JsonElement>("/api/productos?busqueda=F08");
        Assert.Equal(0, lista.GetProperty("elementos")[0].GetProperty("existencia").GetInt64());
        Assert.True(lista.GetProperty("elementos")[0].GetProperty("agotado").GetBoolean());
    }

    [Fact]
    public async Task F13HistorialGlobalFiltraAutorYPaginaSinDarAccesoARecepcion()
    {
        using var app = new MercadoPagoApiTests.AppPago();
        var producto = Sembrar(app, 2);
        var movimiento = app.Leer(db => db.MovimientosInventario.Single().Id);
        app.Leer(db => { db.EventosAuditoria.Add(new EventoAuditoria { Id = Guid.NewGuid(), Entidad = nameof(MovimientoInventario),
            EntidadId = movimiento, ActorId = PerfilesFactory.AdminId, Accion = "Added", Campos = "Id", FechaUtc = DateTime.UtcNow }); return db.SaveChanges(); });
        using var admin = app.Cliente("admin");
        var ruta = $"/api/inventario/movimientos?productoId={producto}&actorId={PerfilesFactory.AdminId}&tamanoPagina=1";
        var respuesta = await admin.GetAsync(ruta);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var pagina = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, pagina.GetProperty("total").GetInt32());
        var fila = pagina.GetProperty("elementos")[0];
        Assert.Equal("Administrador", fila.GetProperty("actorNombre").GetString());
        Assert.Equal("F08", fila.GetProperty("sku").GetString());
        Assert.Empty((await admin.GetFromJsonAsync<JsonElement>(ruta + "&pagina=2")).GetProperty("elementos").EnumerateArray());
        using var recepcion = app.Cliente("recepcion");
        Assert.Equal(HttpStatusCode.Forbidden, (await recepcion.GetAsync(ruta)).StatusCode);
        using var cliente = app.Cliente("cliente");
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync(ruta)).StatusCode);
    }

    [Fact]
    public async Task F15CsvPedidosIncluyeClienteFolioYProductos()
    {
        using var app = new MercadoPagoApiTests.AppPago();
        var producto = Sembrar(app, 2);
        using var cliente = app.Cliente("cliente");
        var id = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsJsonAsync("/api/pedidos", new { idOperacion = id,
            partidas = new[] { new { productoId = producto, cantidad = 1 } } })).StatusCode);
        using var admin = app.Cliente("admin");
        var csv = await admin.GetStringAsync("/api/reportes/pedidos?formato=csv");
        Assert.Contains("NombreCliente", csv); Assert.Contains("Folio", csv); Assert.Contains("Productos", csv);
        Assert.Contains("Cliente de prueba", csv); Assert.Contains("Playera F08", csv);
        Assert.Contains(id.ToString("N")[..8].ToUpperInvariant(), csv);
    }

    private static Guid Sembrar(MercadoPagoApiTests.AppPago app, int stock) => app.Leer(db =>
    {
        var producto = new Producto { Id = Guid.NewGuid(), Sku = "F08", Nombre = "Playera F08", EsVendible = true, Precio = 100, Categoria = CategoriaProducto.Playera };
        db.Productos.Add(producto);
        db.MovimientosInventario.Add(new MovimientoInventario { Id = Guid.NewGuid(), Producto = producto,
            Tipo = TipoMovimientoInventario.Entrada, VariacionCantidad = stock, RegistradoEnUtc = DateTime.UtcNow, Motivo = "Existencias iniciales" });
        db.SaveChanges(); return producto.Id;
    });
}
