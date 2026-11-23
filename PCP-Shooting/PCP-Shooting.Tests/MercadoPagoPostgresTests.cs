using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PCP_Shooting.Domain.Services;
using PCP_Shooting.Persistence.Models;
using PCP_Shooting.Persistence.Services;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class MercadoPagoPostgresTests(PostgresEscriturasFixture fixture) : IClassFixture<PostgresEscriturasFixture>
{
    [PostgresFact]
    public async Task ConciliacionPersisteAprobacionTardiaSinDuplicarlaConTransaccionesReales()
    {
        await using var db = fixture.CrearContext();
        var ahora = DateTime.UtcNow;
        var usuario = new PerfilUsuario { Id = Guid.NewGuid(), Nombre = "Cliente conciliación" };
        var reserva = new ReservaWeb { Id = Guid.NewGuid(), Usuario = usuario, CreadoPor = usuario, EsMostrador = true,
            CodigoPaquete = "CONCILIACION", NombrePaquete = "Actividad", IncluyeMarkdown = "Actividad", SolicitudHash = new string('A',64),
            ZonaHoraria = "America/Cancun", PrecioPorPersona = 100.25m, Total = 100.25m,
            CreadoEnUtc = ahora.AddMinutes(-20), VencePagoEnUtc = ahora.AddMinutes(-10),
            Fecha = DateOnly.FromDateTime(ahora), Hora = new(13,0), InicioUtc = ahora.AddHours(2),
            FinActividadUtc = ahora.AddHours(2).AddMinutes(10), FinOcupacionUtc = ahora.AddHours(2).AddMinutes(15) };
        reserva.Pagos.Add(new PagoReserva { Id = Guid.NewGuid(), Estado = EstadoPago.Pendiente, Importe = 100.25m,
            CreadoEnUtc = ahora.AddMinutes(-15), PreferenciaMercadoPagoId = "preferencia", CheckoutUrl = "https://ejemplo.test/pago" });
        db.ReservasWeb.Add(reserva); await db.SaveChangesAsync();
        using var handler = new MercadoPagoApiTests.PagoHandler { Responder = r => MercadoPagoApiTests.AppPago.Json(
            r.RequestUri!.AbsolutePath.EndsWith("search")
                ? new { results = new[] { new { id = 123 } }, paging = new { total = 1 } }
                : (object)new { id = 123, external_reference = "reserva:" + reserva.Id, status = "approved", transaction_amount = 100.25m, currency_id = "MXN" }) };
        var tx = new TransaccionesPersistence(db);
        var servicio = new MercadoPagoService(db, new MercadoPagoApiTests.ClientesHttp(handler),
            Options.Create(new MercadoPagoOptions { AccessToken = "simulado" }), tx, new EscriturasService(db, tx), TimeProvider.System);
        Assert.Equal(1, (await servicio.ConciliarAsync("reservas", reserva.Id, 1, default)).Consultados);
        db.ChangeTracker.Clear();
        var fecha = (await db.ReservasWeb.SingleAsync(r => r.Id == reserva.Id)).PagoConfirmadoEnUtc;
        Assert.NotNull(fecha);
        await servicio.ConciliarAsync("reservas", reserva.Id, 1, default);
        db.ChangeTracker.Clear();
        Assert.Equal(fecha, (await db.ReservasWeb.SingleAsync(r => r.Id == reserva.Id)).PagoConfirmadoEnUtc);
        Assert.Equal(EstadoPago.Confirmado, (await db.PagosReserva.SingleAsync(p => p.ReservaId == reserva.Id)).Estado);
        Assert.Empty(await db.MovimientosInventario.ToListAsync());
    }
}
