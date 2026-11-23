using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class TallasApiTests
{
    private static object Producto(bool? usaTallas = true, int categoria = 2) => new { sku = "PLAYERA-TALLAS", nombre = "Playera negra",
        categoria, precio = 125m, esVendible = true, usaTallas };
    private static async Task<Guid> Crear(HttpClient admin, bool? usaTallas = true, int categoria = 2)
    {
        var respuesta = await admin.PostAsJsonAsync("/api/productos", Producto(usaTallas, categoria));
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }
    private static Task<HttpResponseMessage> Mover(HttpClient admin, Guid producto, string? talla, int cantidad, Guid? id = null) =>
        admin.PostAsJsonAsync($"/api/inventario/{producto}/movimientos", new { idOperacion = id ?? Guid.NewGuid(), talla,
            tipo = cantidad > 0 ? 0 : 1, variacionCantidad = cantidad, motivo = "Movimiento por talla" });
    private static object Pedido(Guid id, Guid producto, params (string? Talla, int Cantidad)[] partidas) => new { idOperacion = id,
        partidas = partidas.Select(p => new { productoId = producto, talla = p.Talla, cantidad = p.Cantidad }).ToArray() };

    [Fact]
    public async Task DosTallasDelMismoProductoSeCompranYDescuentanUnaSolaVezConPrecioComun()
    {
        using var app = new MercadoPagoApiTests.AppPago(); using var admin = app.Cliente("admin"); using var cliente = app.Cliente("cliente");
        var producto = await Crear(admin);
        foreach (var (talla, cantidad) in new[] { ("S",2), ("M",1), ("L",3) }) Assert.Equal(HttpStatusCode.OK, (await Mover(admin, producto, talla, cantidad)).StatusCode);
        var catalogo = (await cliente.GetFromJsonAsync<JsonElement>("/api/productos")).GetProperty("elementos")[0];
        Assert.True(catalogo.GetProperty("usaTallas").GetBoolean());
        Assert.Equal(4, catalogo.GetProperty("tallas").GetArrayLength());
        Assert.Equal(0, catalogo.GetProperty("tallas").EnumerateArray().Single(t => t.GetProperty("talla").GetString() == "XL").GetProperty("existencia").GetInt64());
        var id = Guid.NewGuid(); var datos = Pedido(id, producto, ("S",2), ("M",1));
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsJsonAsync("/api/pedidos", datos)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsJsonAsync("/api/pedidos", datos)).StatusCode);
        Assert.Equal(6, app.Leer(db => db.MovimientosInventario.Sum(m => m.VariacionCantidad)));
        var detalle = await cliente.GetFromJsonAsync<JsonElement>("/api/pedidos/" + id);
        Assert.Equal(375m, detalle.GetProperty("total").GetDecimal());
        Assert.Equal(new[] { "M", "S" }, detalle.GetProperty("detalles").EnumerateArray().Select(d => d.GetProperty("talla").GetString()).OrderBy(t => t));
        var pago = await admin.PostAsJsonAsync($"/api/pedidos/{id}/pagos", new { idOperacion = Guid.NewGuid(), referenciaExterna = "TALLAS-PAGO" });
        Assert.Equal(HttpStatusCode.OK, pago.StatusCode);
        var pagoId = (await pago.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/pagos/{pagoId}/confirmacion", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/pagos/{pagoId}/confirmacion", null)).StatusCode);
        var stock = await admin.GetFromJsonAsync<JsonElement>($"/api/inventario/{producto}/tallas");
        Assert.Equal(3, stock.EnumerateArray().Single(t => t.GetProperty("talla").GetString() == "L").GetProperty("existencia").GetInt64());
        Assert.Equal(0, stock.EnumerateArray().Single(t => t.GetProperty("talla").GetString() == "S").GetProperty("existencia").GetInt64());
        Assert.Equal(2, app.Leer(db => db.MovimientosInventario.Count(m => m.DetallePedidoId != null)));
    }

    [Fact]
    public async Task ExigeTallaValidaStockPropioYNoPermiteCambiarlaEnReintentos()
    {
        using var app = new MercadoPagoApiTests.AppPago(); using var admin = app.Cliente("admin"); using var cliente = app.Cliente("cliente");
        var producto = await Crear(admin);
        Assert.Equal(HttpStatusCode.BadRequest, (await Mover(admin, producto, null, 2)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Mover(admin, producto, "XXL", 2)).StatusCode);
        var movimiento = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.OK, (await Mover(admin, producto, "S", 2, movimiento)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Mover(admin, producto, "M", 2, movimiento)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync("/api/pedidos", Pedido(Guid.NewGuid(), producto, (null,1)))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await cliente.PostAsJsonAsync("/api/pedidos", Pedido(Guid.NewGuid(), producto, ("M",1)))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync("/api/pedidos", Pedido(Guid.NewGuid(), producto, ("S",1),("S",1)))).StatusCode);
        var id = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsJsonAsync("/api/pedidos", Pedido(id, producto, ("S",1)))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await cliente.PostAsJsonAsync("/api/pedidos", Pedido(id, producto, ("M",1)))).StatusCode);
        using var recepcion = app.Cliente("recepcion");
        Assert.Equal(HttpStatusCode.Forbidden, (await recepcion.GetAsync($"/api/inventario/{producto}/tallas")).StatusCode);
    }

    [Fact]
    public async Task ActivacionNoReparteStockHistoricoNiPermiteTallasEnAlimentos()
    {
        using var app = new MercadoPagoApiTests.AppPago(); using var admin = app.Cliente("admin");
        var producto = await Crear(admin, false);
        Assert.Equal(HttpStatusCode.OK, (await Mover(admin, producto, null, 2)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync("/api/productos/" + producto, Producto(true))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Mover(admin, producto, null, -2)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync("/api/productos/" + producto, Producto(true))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync("/api/productos/" + producto, Producto(true,1))).StatusCode);
    }

    [Fact]
    public async Task PagoNoUsaOtraTallaYSinStockNoPermiteDesactivarConPedidoPendiente()
    {
        using var app = new MercadoPagoApiTests.AppPago(); using var admin = app.Cliente("admin"); using var cliente = app.Cliente("cliente");
        var producto = await Crear(admin);
        Assert.Equal(HttpStatusCode.OK, (await Mover(admin, producto, "S", 1)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Mover(admin, producto, "L", 2)).StatusCode);
        var id = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsJsonAsync("/api/pedidos", Pedido(id, producto, ("S",1)))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Mover(admin, producto, "S", -1)).StatusCode);
        var pago = await admin.PostAsJsonAsync($"/api/pedidos/{id}/pagos", new { idOperacion = Guid.NewGuid(), referenciaExterna = "SIN-STOCK-S" });
        var pagoId = (await pago.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsync($"/api/pagos/{pagoId}/confirmacion", null)).StatusCode);
        Assert.Equal(0, app.Leer(db => db.MovimientosInventario.Count(m => m.DetallePedidoId != null)));
        Assert.Equal(HttpStatusCode.OK, (await Mover(admin, producto, "L", -2)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync("/api/productos/" + producto, Producto(false))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync("/api/productos/" + producto, Producto(null))).StatusCode);
        var ficha = await cliente.GetFromJsonAsync<JsonElement>("/api/productos/PLAYERA-TALLAS");
        Assert.True(ficha.GetProperty("usaTallas").GetBoolean());
        Assert.Equal(4, ficha.GetProperty("tallas").GetArrayLength());
    }
}
