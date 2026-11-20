using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Services;

public sealed class OperacionService(PcpShootingDbContext db, TimeProvider reloj) : IOperacionService
{
    // Una sola sentencia conserva la misma instantánea para parámetros, versión y horarios.
    public Task<ConfiguracionOperativaConsulta> ConsultarConfiguracionAsync(CancellationToken ct) =>
        db.ConfiguracionOperativa.AsNoTracking().AsSingleQuery().Select(configuracion =>
            new ConfiguracionOperativaConsulta("America/Cancun", configuracion.DuracionActividadMinutos, configuracion.MargenMinutos,
                configuracion.DuracionActividadMinutos + configuracion.MargenMinutos, configuracion.ApartadoPagoMinutos,
                configuracion.AnticipacionMinimaMinutos, configuracion.AnticipacionMaximaDias,
                configuracion.LlegadaAnticipadaMinutos, configuracion.ToleranciaLlegadaMinutos, configuracion.Version,
                db.HorariosOperacion.OrderBy(h => h.DiaSemana).Select(h => new HorarioOperacionDatos
                {
                    DiaSemana = h.DiaSemana, Cerrado = h.Cerrado, Apertura = h.Apertura, Cierre = h.Cierre,
                    PrimerTurno = h.PrimerTurno, UltimoTurno = h.UltimoTurno
                }).ToList())).SingleAsync(ct);

    public async Task<ConfiguracionOperativaConsulta> GuardarConfiguracionAsync(GuardarConfiguracionOperativa datos, CancellationToken ct)
    {
        Validar(datos);
        if (datos.Horarios.Any(h => h is null) || datos.Horarios.Select(h => h.DiaSemana).Distinct().Count() != 7)
            throw new ValidationException("Envía exactamente una configuración por cada día de la semana.");
        if (datos.AnticipacionMinimaMinutos >= datos.AnticipacionMaximaDias * 1440)
            throw new ValidationException("La anticipación mínima debe ser menor que la máxima.");
        var intervalo = datos.DuracionActividadMinutos + datos.MargenMinutos;
        foreach (var horario in datos.Horarios)
        {
            Validar(horario);
            if (horario.Cerrado) continue;
            var horas = new[] { horario.Apertura, horario.Cierre, horario.PrimerTurno, horario.UltimoTurno };
            if (horas.Any(h => h.Ticks % TimeSpan.TicksPerMinute != 0) ||
                horario.Apertura >= horario.Cierre || horario.PrimerTurno < horario.Apertura ||
                horario.UltimoTurno < horario.PrimerTurno ||
                horario.UltimoTurno.ToTimeSpan().TotalMinutes + intervalo > horario.Cierre.ToTimeSpan().TotalMinutes ||
                (horario.UltimoTurno - horario.PrimerTurno).TotalMinutes % intervalo != 0)
                throw new ValidationException("Los turnos deben caber en el horario de atención, incluir el margen y mantener un intervalo uniforme.");
        }
        var actual = await db.ConfiguracionOperativa.SingleAsync(ct);
        ComprobarVersion(actual.Version, datos.Version);
        actual.DuracionActividadMinutos = datos.DuracionActividadMinutos;
        actual.MargenMinutos = datos.MargenMinutos;
        actual.ApartadoPagoMinutos = datos.ApartadoPagoMinutos;
        actual.AnticipacionMinimaMinutos = datos.AnticipacionMinimaMinutos;
        actual.AnticipacionMaximaDias = datos.AnticipacionMaximaDias;
        actual.LlegadaAnticipadaMinutos = datos.LlegadaAnticipadaMinutos;
        actual.ToleranciaLlegadaMinutos = datos.ToleranciaLlegadaMinutos;
        actual.Version = Guid.NewGuid();
        var horarios = await db.HorariosOperacion.ToListAsync(ct);
        foreach (var horario in horarios)
        {
            var cambio = datos.Horarios.Single(h => h.DiaSemana == horario.DiaSemana);
            horario.Cerrado = cambio.Cerrado;
            horario.Apertura = cambio.Apertura;
            horario.Cierre = cambio.Cierre;
            horario.PrimerTurno = cambio.PrimerTurno;
            horario.UltimoTurno = cambio.UltimoTurno;
        }
        await GuardarAsync(ct);
        return await ConsultarConfiguracionAsync(ct);
    }

    public async Task<IReadOnlyList<LineaTiroConsulta>> ListarLineasAsync(CancellationToken ct) =>
        await db.LineasTiro.AsNoTracking().OrderBy(l => l.Numero)
            .Select(l => new LineaTiroConsulta(l.Id, l.Numero, l.Nombre, l.Activa, l.Capacidad, l.Version)).ToListAsync(ct);

    public async Task<LineaTiroConsulta> GuardarLineaAsync(Guid id, GuardarLineaTiro datos, CancellationToken ct)
    {
        Validar(datos);
        if (string.IsNullOrWhiteSpace(datos.Nombre)) throw new ValidationException("El nombre de la línea es obligatorio.");
        var linea = await db.LineasTiro.SingleOrDefaultAsync(l => l.Id == id, ct) ?? throw new OperacionException(404, "Línea inexistente.");
        ComprobarVersion(linea.Version, datos.Version);
        linea.Nombre = datos.Nombre.Trim();
        linea.Activa = datos.Activa;
        linea.Version = Guid.NewGuid();
        await GuardarAsync(ct);
        return new(linea.Id, linea.Numero, linea.Nombre, linea.Activa, linea.Capacidad, linea.Version);
    }

    public Task<PaginaConsulta<BloqueoOperacionConsulta>> ListarBloqueosAsync(FiltroBloqueosOperacion filtro, CancellationToken ct)
    {
        Validar(filtro);
        var consulta = db.BloqueosOperacion.AsNoTracking();
        if (filtro.LineaId.HasValue) consulta = consulta.Where(b => b.LineaId == filtro.LineaId || b.LineaId == null);
        if (filtro.Activo.HasValue) consulta = consulta.Where(b => b.Activo == filtro.Activo);
        // Intervalos semiabiertos: un bloqueo que termina al iniciar otro horario no lo ocupa.
        if (filtro.DesdeUtc.HasValue) consulta = consulta.Where(b => b.FinUtc > filtro.DesdeUtc.Value.UtcDateTime);
        if (filtro.HastaUtc.HasValue) consulta = consulta.Where(b => b.InicioUtc < filtro.HastaUtc.Value.UtcDateTime);
        return PaginacionConsulta.CrearAsync(consulta.OrderBy(b => b.InicioUtc).ThenBy(b => b.Id).Select(Proyectar), filtro, ct);
    }

    public Task<BloqueoOperacionConsulta?> ConsultarBloqueoAsync(Guid id, CancellationToken ct) =>
        db.BloqueosOperacion.AsNoTracking().Where(b => b.Id == id).Select(Proyectar).SingleOrDefaultAsync(ct);

    public async Task<BloqueoOperacionConsulta> GuardarBloqueoAsync(Guid? id, Guid actorId, GuardarBloqueoOperacion datos, CancellationToken ct)
    {
        Validar(datos);
        if (datos.InicioUtc == default || datos.FinUtc == default || datos.FinUtc <= datos.InicioUtc || string.IsNullOrWhiteSpace(datos.Motivo))
            throw new ValidationException("Indica un motivo y un intervalo de fechas válido.");
        if (datos.LineaId.HasValue && !await db.LineasTiro.AnyAsync(l => l.Id == datos.LineaId, ct))
            throw new OperacionException(404, "Línea inexistente.");
        var ahora = reloj.GetUtcNow().UtcDateTime;
        var bloqueo = id.HasValue ? await db.BloqueosOperacion.SingleOrDefaultAsync(b => b.Id == id, ct)
            : new BloqueoOperacion { Id = Guid.NewGuid(), Motivo = "", CreadoPorId = actorId, CreadoEnUtc = ahora };
        if (bloqueo is null) throw new OperacionException(404, "Bloqueo inexistente.");
        if (id.HasValue) ComprobarVersion(bloqueo.Version, datos.Version);
        bloqueo.LineaId = datos.LineaId;
        bloqueo.InicioUtc = datos.InicioUtc.UtcDateTime;
        bloqueo.FinUtc = datos.FinUtc.UtcDateTime;
        bloqueo.Motivo = datos.Motivo.Trim();
        bloqueo.Activo = datos.Activo;
        bloqueo.ActualizadoEnUtc = ahora;
        bloqueo.Version = Guid.NewGuid();
        if (!id.HasValue) db.BloqueosOperacion.Add(bloqueo);
        await GuardarAsync(ct);
        return (await ConsultarBloqueoAsync(bloqueo.Id, ct))!;
    }

    private static readonly Expression<Func<BloqueoOperacion, BloqueoOperacionConsulta>> Proyectar = b => new(
        b.Id, b.LineaId, b.InicioUtc, b.FinUtc, b.Motivo, b.Activo, b.CreadoPorId, b.CreadoEnUtc, b.ActualizadoEnUtc, b.Version);

    private async Task GuardarAsync(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new OperacionException(409, "El registro cambió. Consulta su versión actual antes de editarlo."); }
    }

    private static void ComprobarVersion(Guid actual, Guid? enviada)
    {
        if (enviada is null || enviada == Guid.Empty) throw new ValidationException("La versión actual es obligatoria.");
        if (actual != enviada) throw new OperacionException(409, "El registro cambió. Consulta su versión actual antes de editarlo.");
    }
    private static void Validar(object datos) => Validator.ValidateObject(datos, new ValidationContext(datos), true);
}
