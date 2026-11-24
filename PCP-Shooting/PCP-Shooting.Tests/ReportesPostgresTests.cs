using System.Net;
using System.Text.Json;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class ReportesPostgresTests(PostgresEscriturasFixture fixture) : IClassFixture<PostgresEscriturasFixture>
{
    [PostgresFact]
    public async Task ReportesConsultanPostgreSqlConFiltrosImportesYExportacion()
    {
        using var factory = new OperacionesFactory(fixture);
        using var admin = factory.Cliente(OperacionesFactory.AdminId);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/usuarios/yo")).StatusCode);
        await using (var db = fixture.CrearContext())
        {
            var producto = new Producto { Id = Guid.NewGuid(), Sku = "REPORTE", Nombre = "Playera edición", Categoria = CategoriaProducto.Playera, StockMinimo = 2, Precio = 999 };
            db.Productos.Add(producto);
            db.MovimientosInventario.Add(new MovimientoInventario { Id = Guid.NewGuid(), ProductoId = producto.Id, VariacionCantidad = 1,
                RegistradoEnUtc = DateTime.UtcNow, Tipo = TipoMovimientoInventario.Entrada });
            var pedido = new Pedido { Id = Guid.NewGuid(), UsuarioId = OperacionesFactory.ClienteId, Estado = EstadoPedido.Pagado,
                CreadoEnUtc = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc) };
            pedido.Detalles.Add(new DetallePedido { Id = Guid.NewGuid(), ProductoId = producto.Id, Cantidad = 2, PrecioUnitario = 10.25m });
            var pago = new Pago { Id = Guid.NewGuid(), Importe = 20.50m, Estado = EstadoPago.Confirmado, ReferenciaExterna = "REPORTE-PG",
                ConfirmadoEnUtc = pedido.CreadoEnUtc.AddDays(10) };
            pedido.Pagos.Add(pago);
            db.Pedidos.Add(pedido);
            db.Membresias.Add(new MembresiaCartaq { Id = Guid.NewGuid(), UsuarioId = pedido.UsuarioId, Pago = pago,
                InicioVigencia = new DateOnly(2026, 1, 1), FinVigenciaExclusivo = new DateOnly(2027, 1, 1) });
            await db.SaveChangesAsync();
        }
        var consultas = new[]
        {
            $"pedidos?usuarioId={OperacionesFactory.ClienteId}&busqueda=reporte&estado=1&desdeUtc=2026-01-01T12:00:00Z&hastaUtc=2026-01-02T12:00:00Z",
            "pagos?estado=1&referencia=REPORTE&desdeUtc=2026-01-11T12:00:00Z&hastaUtc=2026-01-12T12:00:00Z",
            "membresias?desde=2026-01-01&hasta=2027-01-01&vigenteEn=2026-12-31",
            "inventario?categoria=2&busqueda=edición&conExistencias=true&bajoMinimo=true&activo=true"
        };
        foreach (var consulta in consultas)
        {
            using var json = JsonDocument.Parse(await admin.GetStringAsync("/api/reportes/" + consulta));
            Assert.Equal(1, json.RootElement.GetProperty("datos").GetProperty("total").GetInt32());
            var fila = Assert.Single(json.RootElement.GetProperty("datos").GetProperty("elementos").EnumerateArray());
            if (consulta.StartsWith("pedidos")) Assert.Equal(20.50m, fila.GetProperty("total").GetDecimal());
            if (consulta.StartsWith("pagos") || consulta.StartsWith("membresias"))
                Assert.Equal(20.50m, json.RootElement.GetProperty("importeConfirmado").GetDecimal());
            if (consulta.StartsWith("inventario")) Assert.Equal(1, fila.GetProperty("existencia").GetInt64());
            var csv = await admin.GetAsync("/api/reportes/" + consulta + "&formato=csv");
            Assert.Equal(HttpStatusCode.OK, csv.StatusCode);
            Assert.Equal(2, (await csv.Content.ReadAsStringAsync()).TrimEnd().Split("\r\n").Length);
        }
        using var vencidas = JsonDocument.Parse(await admin.GetStringAsync("/api/reportes/membresias?vigenteEn=2027-01-01"));
        Assert.Equal(0, vencidas.RootElement.GetProperty("datos").GetProperty("total").GetInt32());
    }
}
