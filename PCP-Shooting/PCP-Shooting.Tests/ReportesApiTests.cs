using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class ReportesApiTests : IClassFixture<PerfilesFactory>
{
    private readonly PerfilesFactory factory;
    public ReportesApiTests(PerfilesFactory factory)
    {
        this.factory = factory;
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>();
        if (db.Pedidos.Any()) return;
        var producto = new Producto { Id = Guid.NewGuid(), Sku = "=FORMULA", Nombre = "Playera, \"edición\"\nAzul", Categoria = CategoriaProducto.Playera, Precio = 999, StockMinimo = 2 };
        db.Productos.Add(producto);
        db.MovimientosInventario.Add(new MovimientoInventario { Id = Guid.NewGuid(), ProductoId = producto.Id, VariacionCantidad = 1 });
        for (var i = 0; i < 3; i++)
        {
            var pedido = new Pedido { Id = Guid.NewGuid(), UsuarioId = i == 2 ? PerfilesFactory.InactivoId : PerfilesFactory.ClienteId,
                CreadoEnUtc = new DateTime(2026, 1, i + 1, 12, 0, 0, DateTimeKind.Utc), Estado = i == 2 ? EstadoPedido.PendientePago : EstadoPedido.Pagado };
            pedido.Detalles.Add(new DetallePedido { Id = Guid.NewGuid(), ProductoId = producto.Id, Cantidad = 2, PrecioUnitario = 10.25m + i });
            var pago = new Pago { Id = Guid.NewGuid(), Importe = (10.25m + i) * 2, Estado = i == 2 ? EstadoPago.Pendiente : EstadoPago.Confirmado,
                ReferenciaExterna = "REF-" + i, ConfirmadoEnUtc = i == 2 ? null : pedido.CreadoEnUtc.AddDays(10) };
            pedido.Pagos.Add(pago);
            db.Pedidos.Add(pedido);
            if (i < 2) db.Membresias.Add(new MembresiaCartaq { Id = Guid.NewGuid(), UsuarioId = pedido.UsuarioId, Pago = pago,
                InicioVigencia = new DateOnly(2026 + i, 1, 1), FinVigenciaExclusivo = new DateOnly(2027 + i, 1, 1) });
        }
        db.SaveChanges();
    }

    [Theory]
    [InlineData("pedidos", 3)]
    [InlineData("pagos", 3)]
    [InlineData("membresias", 2)]
    [InlineData("inventario", 1)]
    public async Task JsonPaginadoYCsvExportaTodasLasFilas(string tipo, int total)
    {
        using var admin = factory.CrearCliente("admin");
        var response = await admin.GetAsync($"/api/reportes/{tipo}?tamanoPagina=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(total, json.RootElement.GetProperty("datos").GetProperty("total").GetInt32());
        Assert.Single(json.RootElement.GetProperty("datos").GetProperty("elementos").EnumerateArray());
        var csv = await admin.GetAsync($"/api/reportes/{tipo}?formato=csv&tamanoPagina=1&pagina=999");
        Assert.Equal(HttpStatusCode.OK, csv.StatusCode);
        Assert.Equal("text/csv", csv.Content.Headers.ContentType!.MediaType);
        Assert.Contains("attachment", csv.Content.Headers.ContentDisposition!.ToString());
        Assert.Equal(new byte[] { 239, 187, 191 }, (await csv.Content.ReadAsByteArrayAsync()).Take(3).ToArray());
        var texto = await csv.Content.ReadAsStringAsync();
        if (tipo == "inventario")
        {
            Assert.Contains("\"'=FORMULA\"", texto);
            Assert.Contains("Playera, \"\"edición\"\"\nAzul", texto);
        }
        else Assert.Equal(total + 1, texto.TrimEnd().Split("\r\n").Length);
    }

    [Fact]
    public async Task PagosSumaSoloConfirmadosYFiltraPorFechaDeConfirmacion()
    {
        using var admin = factory.CrearCliente("admin");
        using var json = JsonDocument.Parse(await admin.GetStringAsync("/api/reportes/pagos"));
        Assert.Equal(43m, json.RootElement.GetProperty("importeConfirmado").GetDecimal());
        using var filtrado = JsonDocument.Parse(await admin.GetStringAsync("/api/reportes/pagos?desdeUtc=2026-01-11T12:00:00Z&hastaUtc=2026-01-12T12:00:00Z"));
        Assert.Equal(1, filtrado.RootElement.GetProperty("datos").GetProperty("total").GetInt32());
        Assert.Equal(20.5m, filtrado.RootElement.GetProperty("importeConfirmado").GetDecimal());
        using var pendientes = JsonDocument.Parse(await admin.GetStringAsync("/api/reportes/pagos?estado=0"));
        Assert.Equal(0m, pendientes.RootElement.GetProperty("importeConfirmado").GetDecimal());
    }

    [Fact]
    public async Task PedidosUsaPrecioHistoricoYFiltraUsuarioYFechas()
    {
        using var admin = factory.CrearCliente("admin");
        using var json = JsonDocument.Parse(await admin.GetStringAsync($"/api/reportes/pedidos?usuarioId={PerfilesFactory.ClienteId}&desdeUtc=2026-01-01T12:00:00Z&hastaUtc=2026-01-02T12:00:00Z"));
        Assert.Equal(1, json.RootElement.GetProperty("datos").GetProperty("total").GetInt32());
        Assert.Equal(20.5m, json.RootElement.GetProperty("datos").GetProperty("elementos")[0].GetProperty("total").GetDecimal());
    }

    [Theory]
    [InlineData("pedidos?pagina=0")]
    [InlineData("pedidos?estado=99")]
    [InlineData("pagos?desdeUtc=2026-02-01&hastaUtc=2026-01-01")]
    [InlineData("membresias?desde=2027-01-01&hasta=2026-01-01")]
    [InlineData("inventario?categoria=99")]
    [InlineData("inventario?formato=pdf")]
    public async Task FiltrosInvalidosDevuelven400(string ruta)
    {
        using var admin = factory.CrearCliente("admin");
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/api/reportes/" + ruta)).StatusCode);
    }

    [Theory]
    [InlineData("pedidos")]
    [InlineData("pagos")]
    [InlineData("membresias")]
    [InlineData("inventario")]
    public async Task ReportesRechazanClientesYAnonimos(string tipo)
    {
        using var cliente = factory.CrearCliente("cliente");
        using var anonimo = factory.CrearCliente(null);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync($"/api/reportes/{tipo}?formato=csv")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync($"/api/reportes/{tipo}")).StatusCode);
        using var jwt = new JwtBackendFactory();
        using var recepcion = jwt.Cliente(jwt.Token(JwtBackendFactory.RecepcionId));
        Assert.Equal(HttpStatusCode.Forbidden, (await recepcion.GetAsync($"/api/reportes/{tipo}")).StatusCode);
    }
}
