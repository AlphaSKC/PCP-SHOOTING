using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class AlimentosReportePostgresTests(PostgresEscriturasFixture fixture) : IClassFixture<PostgresEscriturasFixture>
{
    [PostgresFact]
    public async Task ReporteAlimentosSumaPartidasHistoricasYExportaEnPostgreSql()
    {
        using var factory = new OperacionesFactory(fixture);
        using var admin = factory.Cliente(OperacionesFactory.AdminId);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/usuarios/yo")).StatusCode);
        var alimentoId = Guid.NewGuid();
        await using (var db = fixture.CrearContext())
        {
            db.Productos.Add(new Producto { Id = alimentoId, Sku = "FOOD", Nombre = "Bebida", Categoria = CategoriaProducto.Alimento, Precio = 999m, Activo = false });
            var ropa = new Producto { Id = Guid.NewGuid(), Sku = "MERCH", Nombre = "Playera", Categoria = CategoriaProducto.Playera };
            db.Productos.Add(ropa);
            foreach (var estado in new[] { EstadoPedido.Pagado, EstadoPedido.Recogido, EstadoPedido.PendientePago, EstadoPedido.Cancelado })
            {
                var pedido = new Pedido { Id = Guid.NewGuid(), UsuarioId = OperacionesFactory.ClienteId,
                    Estado = estado, CreadoEnUtc = new DateTime(2026, 11, 24, 12, 0, 0, DateTimeKind.Utc) };
                pedido.Detalles.Add(new DetallePedido { Id = Guid.NewGuid(), ProductoId = alimentoId, Cantidad = 2, PrecioUnitario = 10.25m });
                pedido.Detalles.Add(new DetallePedido { Id = Guid.NewGuid(), ProductoId = ropa.Id, Cantidad = 1, PrecioUnitario = 100m });
                db.Pedidos.Add(pedido);
            }
            await db.SaveChangesAsync();
        }
        var ruta = $"/api/reportes/alimentos?productoId={alimentoId}&usuarioId={OperacionesFactory.ClienteId}&desdeUtc=2026-11-24T12:00:00Z&hastaUtc=2026-11-24T13:00:00Z";
        var reporte = await admin.GetFromJsonAsync<JsonElement>(ruta + "&tamanoPagina=1");
        Assert.Equal(2, reporte.GetProperty("datos").GetProperty("total").GetInt32());
        Assert.Equal(41m, reporte.GetProperty("importeConfirmado").GetDecimal());
        Assert.Equal(20.5m, Assert.Single(reporte.GetProperty("datos").GetProperty("elementos").EnumerateArray()).GetProperty("total").GetDecimal());
        var csv = await admin.GetStringAsync(ruta + "&formato=csv&pagina=999&tamanoPagina=1");
        Assert.Equal(3, csv.TrimEnd().Split("\r\n").Length);
        var sinCliente = await admin.GetFromJsonAsync<JsonElement>("/api/reportes/alimentos?usuarioId=" + Guid.NewGuid());
        Assert.Equal(0m, sinCliente.GetProperty("importeConfirmado").GetDecimal());
    }
}
