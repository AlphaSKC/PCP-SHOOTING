using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class HistorialClientePostgresTests(PostgresEscriturasFixture fixture)
    : IClassFixture<PostgresEscriturasFixture>
{
    [PostgresFact]
    public async Task HistorialIntegraDatosYPaginaSinMezclarUsuariosEnPostgreSql()
    {
        using var factory = new OperacionesFactory(fixture);
        using var admin = factory.Cliente(OperacionesFactory.AdminId);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/usuarios/yo")).StatusCode);
        await using (var db = fixture.CrearContext())
        {
            var producto = new Producto { Id = Guid.NewGuid(), Sku = Guid.NewGuid().ToString(), Nombre = "Anterior", Precio = 999, Activo = false };
            db.Productos.Add(producto);
            foreach (var indice in new[] { 0, 1, 9 })
            {
                var usuarioId = indice == 9 ? OperacionesFactory.OtroId : OperacionesFactory.ClienteId;
                var precio = 17.25m + indice;
                var pedido = new Pedido
                {
                    Id = Guid.NewGuid(), UsuarioId = usuarioId, Estado = EstadoPedido.Pagado,
                    CreadoEnUtc = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc).AddDays(indice)
                };
                pedido.Detalles.Add(new DetallePedido { Id = Guid.NewGuid(), ProductoId = producto.Id, Cantidad = 2, PrecioUnitario = precio });
                var pago = new Pago
                {
                    Id = Guid.NewGuid(), Importe = precio * 2, Estado = EstadoPago.Confirmado,
                    ReferenciaExterna = Guid.NewGuid().ToString(), ConfirmadoEnUtc = pedido.CreadoEnUtc.AddHours(1)
                };
                pedido.Pagos.Add(pago);
                db.Pedidos.Add(pedido);
                db.Membresias.Add(new MembresiaCartaq
                {
                    Id = Guid.NewGuid(), UsuarioId = usuarioId, Pago = pago,
                    InicioVigencia = new DateOnly(2024 + indice, 1, 1),
                    FinVigenciaExclusivo = new DateOnly(2025 + indice, 1, 1)
                });
            }
            var otro = await db.PerfilesUsuario.SingleAsync(p => p.Id == OperacionesFactory.OtroId);
            otro.Activo = false;
            await db.SaveChangesAsync();
        }
        var historial = await admin.GetFromJsonAsync<HistorialCliente>(
            $"/api/administracion/usuarios/{OperacionesFactory.ClienteId}/historial" +
            $"?tamanoPagina=1&paginaPedidos=2&paginaPagos=1&paginaMembresias=2&usuarioId={OperacionesFactory.OtroId}");
        Assert.Equal(OperacionesFactory.ClienteId, historial!.Perfil.Id);
        Assert.Equal(2, historial.Pedidos.Total);
        Assert.Equal(2, historial.Pagos.Total);
        Assert.Equal(2, historial.Membresias.Total);
        Assert.Equal(34.50m, Assert.Single(historial.Pedidos.Elementos).Total);
        Assert.Equal(36.50m, Assert.Single(historial.Pagos.Elementos).Importe);
        Assert.Equal(new DateOnly(2024, 1, 1), Assert.Single(historial.Membresias.Elementos).InicioVigencia);
        var inactivo = await admin.GetFromJsonAsync<HistorialCliente>(
            $"/api/administracion/usuarios/{OperacionesFactory.OtroId}/historial");
        Assert.False(inactivo!.Perfil.Activo);
        Assert.Equal(1, inactivo.Pedidos.Total);
    }
}
