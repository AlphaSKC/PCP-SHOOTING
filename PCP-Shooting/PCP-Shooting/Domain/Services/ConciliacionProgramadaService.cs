using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Services;

public sealed class ConciliacionProgramadaService(PcpShootingDbContext db, IServiceScopeFactory scopes,
    IOptions<ConciliacionProgramadaOptions> options, IOptions<MercadoPagoOptions> mercado,
    TimeProvider reloj, ILogger<ConciliacionProgramadaService> log) : IConciliacionProgramadaService
{
    public async Task<CicloConciliacionResultado> EjecutarCicloAsync(CancellationToken ct)
    {
        var config = options.Value;
        if (!config.Habilitada || string.IsNullOrWhiteSpace(mercado.Value.AccessToken)) return new(0, 0, 0, 0);
        Validator.ValidateObject(config, new ValidationContext(config), true);
        var limite = reloj.GetUtcNow().UtcDateTime.AddMinutes(-config.RetrasoMinutos);
        var pedidos = db.Pagos.AsNoTracking().Where(p => p.EsMercadoPago && p.Pedido.Estado == EstadoPedido.PendientePago &&
            (p.Estado == EstadoPago.Pendiente || (p.Estado == EstadoPago.Rechazado && p.PaginaConciliacion > 0 && p.UltimaConciliacionEnUtc != null)) && p.Pedido.CreadoEnUtc <= limite)
            .Select(p => new { PagoId = p.Id, DestinoId = p.PedidoId, Tipo = "pedidos", Ultima = p.UltimaConciliacionEnUtc, Pagina = p.PaginaConciliacion, Creado = p.Pedido.CreadoEnUtc });
        var reservas = db.PagosReserva.AsNoTracking().Where(p => p.Reserva.PagoConfirmadoEnUtc == null &&
            (p.Estado == EstadoPago.Pendiente || (p.Estado == EstadoPago.Rechazado && p.ConciliacionEnCurso)) && p.CreadoEnUtc <= limite)
            .Select(p => new { PagoId = p.Id, DestinoId = p.ReservaId, Tipo = "reservas", Ultima = p.UltimaConciliacionEnUtc, Pagina = p.PaginaConciliacion, Creado = p.CreadoEnUtc });
        var trabajos = await pedidos.Concat(reservas).OrderBy(p => p.Ultima ?? p.Creado).ThenBy(p => p.Creado)
            .ThenBy(p => p.Tipo).ThenBy(p => p.PagoId).Take(config.Lote).ToArrayAsync(ct);
        var revisadas = 0; var fallidas = 0; var paginas = 0; var consultados = 0;
        foreach (var trabajo in trabajos)
        {
            ct.ThrowIfCancellationRequested();
            await using var scope = scopes.CreateAsyncScope();
            var escritura = scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>();
            try
            {
                var registro = await Cargar(escritura, trabajo.Tipo, trabajo.PagoId, ct);
                if (registro is null || !registro.PuedeRevisar) continue;
                revisadas++;
                registro.Ultima = reloj.GetUtcNow().UtcDateTime;
                registro.EnCurso = true;
                registro.Pagina = Math.Max(1, registro.Pagina);
                await escritura.SaveChangesAsync(ct);
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeout.CancelAfter(TimeSpan.FromSeconds(config.TimeoutOperacionSegundos));
                var servicio = scope.ServiceProvider.GetRequiredService<IMercadoPagoService>();
                for (var paso = 0; paso < config.PaginasPorOperacion; paso++)
                {
                    var pagina = registro.Pagina;
                    var resultado = await servicio.ConciliarAsync(trabajo.Tipo, trabajo.DestinoId, pagina, timeout.Token);
                    paginas++; consultados += resultado.Consultados;
                    escritura.ChangeTracker.Clear();
                    registro = (await Cargar(escritura, trabajo.Tipo, trabajo.PagoId, timeout.Token))!;
                    if (registro.Estado is EstadoPago.Confirmado or EstadoPago.Cancelado)
                        registro.Pagina = 0;
                    else if (resultado.HayMas) registro.Pagina = pagina < 10000 ? pagina + 1 : 1;
                    else registro.Pagina = registro.Estado == EstadoPago.Pendiente ? 1 : 0;
                    if (!resultado.HayMas || registro.Estado is EstadoPago.Confirmado or EstadoPago.Cancelado) registro.EnCurso = false;
                    await escritura.SaveChangesAsync(timeout.Token);
                    if (!resultado.HayMas || registro.Pagina == 0) break;
                    if (pagina == 10000)
                    {
                        log.LogWarning("Conciliación {Tipo}/{Id}: límite de páginas alcanzado; requiere revisión manual.", trabajo.Tipo, trabajo.DestinoId);
                        break;
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                fallidas++;
                log.LogWarning("Conciliación {Tipo}/{Id} pendiente de reintento ({TipoError}).", trabajo.Tipo, trabajo.DestinoId, ex.GetType().Name);
            }
        }
        return new(revisadas, fallidas, paginas, consultados);
    }
    private static async Task<Registro?> Cargar(PcpShootingDbContext db, string tipo, Guid id, CancellationToken ct)
    {
        if (tipo == "pedidos")
        {
            var p = await db.Pagos.Include(p => p.Pedido).SingleOrDefaultAsync(p => p.Id == id, ct);
            return p is null ? null : new(p, null);
        }
        var reserva = await db.PagosReserva.Include(p => p.Reserva).SingleOrDefaultAsync(p => p.Id == id, ct);
        return reserva is null ? null : new(null, reserva);
    }
    private sealed class Registro(Pago? pedido, PagoReserva? reserva)
    {
        public bool EnCurso { set { if (reserva is not null) reserva.ConciliacionEnCurso = value; } }
        public EstadoPago Estado => pedido?.Estado ?? reserva!.Estado;
        public bool PuedeRevisar => (pedido is not null ? pedido.EsMercadoPago && pedido.Pedido.Estado == EstadoPedido.PendientePago : reserva!.Reserva.PagoConfirmadoEnUtc == null) &&
            (Estado == EstadoPago.Pendiente || (Estado == EstadoPago.Rechazado &&
                (reserva is not null ? reserva.ConciliacionEnCurso : Pagina > 0 && Ultima != null)));
        public DateTime? Ultima
        {
            get => pedido is not null ? pedido.UltimaConciliacionEnUtc : reserva!.UltimaConciliacionEnUtc;
            set { if (pedido is not null) pedido.UltimaConciliacionEnUtc = value; else reserva!.UltimaConciliacionEnUtc = value; }
        }
        public int Pagina
        {
            get => pedido?.PaginaConciliacion ?? reserva!.PaginaConciliacion;
            set { if (pedido is not null) pedido.PaginaConciliacion = value; else reserva!.PaginaConciliacion = value; }
        }
    }
}
