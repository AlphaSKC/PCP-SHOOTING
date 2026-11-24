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
            var paquete = new Paquete { Id = Guid.NewGuid(), Codigo = "HIST-RESERVAS", Nombre = "Paquete histórico",
                IncluyeMarkdown = "Actividad", Version = Guid.NewGuid(), PrecioPorPersona = 100m };
            db.Paquetes.Add(paquete);
            var producto = new Producto { Id = Guid.NewGuid(), Sku = Guid.NewGuid().ToString(), Nombre = "Anterior", Precio = 999, Activo = false };
            db.Productos.Add(producto);
            foreach (var indice in new[] { 0, 1, 9 })
            {
                var usuarioId = indice == 9 ? OperacionesFactory.OtroId : OperacionesFactory.ClienteId;
                var precio = 17.25m + indice;
                var inicio = new DateTime(2026, 1, 1, 16, 0, 0, DateTimeKind.Utc).AddDays(indice);
                var reserva = new ReservaWeb { Id = Guid.NewGuid(), UsuarioId = usuarioId, CreadoPorId = OperacionesFactory.AdminId,
                    PaqueteId = paquete.Id, PaqueteVersion = paquete.Version, CodigoPaquete = paquete.Codigo, NombrePaquete = paquete.Nombre,
                    IncluyeMarkdown = "Actividad", SolicitudHash = new string('a', 64), PrecioPorPersona = 100m, Total = 100m,
                    Fecha = DateOnly.FromDateTime(inicio), Hora = new TimeOnly(11, 0), ZonaHoraria = "America/Cancun",
                    InicioUtc = inicio, FinActividadUtc = inicio.AddMinutes(10), FinOcupacionUtc = inicio.AddMinutes(15),
                    CreadoEnUtc = inicio.AddHours(-1), VencePagoEnUtc = inicio.AddMinutes(-50),
                    PagoConfirmadoEnUtc = indice == 0 ? inicio.AddMinutes(-55) : null };
                reserva.Participantes.Add(new ParticipanteReserva { Id = Guid.NewGuid(), LineaId = await db.LineasTiro.Select(l => l.Id).FirstAsync(),
                    Nombre = "Visitante", Nacimiento = new DateOnly(2000, 1, 1) });
                if (indice == 1) reserva.Pagos.Add(new PagoReserva { Id = Guid.NewGuid(), Importe = 100m,
                    CreadoEnUtc = inicio.AddMinutes(-55), Estado = EstadoPago.Rechazado, ConciliacionEnCurso = true });
                db.ReservasWeb.Add(reserva);
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
        Assert.Equal(2, historial.Reservas.Total);
        Assert.Equal("PendientePago", Assert.Single(historial.Reservas.Elementos).Estado.ToString());
        Assert.Equal(34.50m, Assert.Single(historial.Pedidos.Elementos).Total);
        Assert.Equal(36.50m, Assert.Single(historial.Pagos.Elementos).Importe);
        Assert.Equal(new DateOnly(2024, 1, 1), Assert.Single(historial.Membresias.Elementos).InicioVigencia);
        var inactivo = await admin.GetFromJsonAsync<HistorialCliente>(
            $"/api/administracion/usuarios/{OperacionesFactory.OtroId}/historial");
        Assert.False(inactivo!.Perfil.Activo);
        Assert.Equal(1, inactivo.Pedidos.Total);
        Assert.Equal(1, inactivo.Reservas.Total);
        var reporte = await admin.GetFromJsonAsync<System.Text.Json.JsonElement>(
            $"/api/reportes/reservas?usuarioId={OperacionesFactory.ClienteId}&estado=Pagada&desdeUtc=2026-01-01T16:00:00Z&hastaUtc=2026-01-02T16:00:00Z");
        Assert.Equal(1, reporte.GetProperty("datos").GetProperty("total").GetInt32());
        Assert.Equal(100m, reporte.GetProperty("importeConfirmado").GetDecimal());
        var pendiente = await admin.GetFromJsonAsync<System.Text.Json.JsonElement>(
            $"/api/reportes/reservas?usuarioId={OperacionesFactory.ClienteId}&estado=PendientePago");
        Assert.Equal(1, pendiente.GetProperty("datos").GetProperty("total").GetInt32());
        Assert.Equal(0m, pendiente.GetProperty("importeConfirmado").GetDecimal());
        var vencida = await admin.GetFromJsonAsync<System.Text.Json.JsonElement>(
            $"/api/reportes/reservas?usuarioId={OperacionesFactory.OtroId}&estado=Vencida");
        Assert.Equal(1, vencida.GetProperty("datos").GetProperty("total").GetInt32());
        var csv = await admin.GetStringAsync($"/api/reportes/reservas?usuarioId={OperacionesFactory.ClienteId}&formato=csv");
        Assert.Equal(3, csv.TrimEnd().Split("\r\n").Length);
    }
}
