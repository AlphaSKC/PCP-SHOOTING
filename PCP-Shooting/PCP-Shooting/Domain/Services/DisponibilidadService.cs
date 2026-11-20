using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;

namespace PCP_Shooting.Domain.Services;

public sealed class DisponibilidadService(PcpShootingDbContext db, IOperacionService operacion,
    IPaquetesService paquetes, TimeProvider reloj) : IDisponibilidadService
{
    public async Task<DisponibilidadConsulta> ConsultarAsync(ConsultaDisponibilidad filtro, CancellationToken ct)
    {
        Validator.ValidateObject(filtro, new ValidationContext(filtro), true);
        if (filtro.PaqueteId is null || filtro.PaqueteId == Guid.Empty || filtro.Fecha is null)
            throw new ValidationException("Indica un paquete y una fecha válidos.");
        var ahora = reloj.GetUtcNow();
        // Misma instantánea para paquete, horarios, líneas y bloqueos durante esta consulta.
        await using var lectura = db.Database.IsRelational() && db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct) : null;
        if (await paquetes.ConsultarPublicoAsync(filtro.PaqueteId.Value, ct) is null)
            throw new OperacionException(404, "Paquete inexistente o no disponible en el catálogo.");
        var configuracion = await operacion.ConsultarConfiguracionAsync(ct);
        var fecha = filtro.Fecha.Value;
        var horario = configuracion.Horarios.Single(h => h.DiaSemana == (int)fecha.DayOfWeek);
        var resultado = new List<TurnoDisponibilidad>();
        var respuesta = new DisponibilidadConsulta(filtro.PaqueteId.Value, fecha, configuracion.ZonaHoraria,
            ahora.UtcDateTime, !horario.Cerrado, filtro.Personas, resultado);
        if (horario.Cerrado) return respuesta;
        var primero = (int)horario.PrimerTurno.ToTimeSpan().TotalMinutes;
        var ultimo = (int)horario.UltimoTurno.ToTimeSpan().TotalMinutes;
        var intervalo = configuracion.IntervaloTurnosMinutos;
        if (filtro.Hora is TimeOnly solicitada && (solicitada.Ticks % TimeSpan.TicksPerMinute != 0 ||
            solicitada < horario.PrimerTurno || solicitada > horario.UltimoTurno ||
            ((int)solicitada.ToTimeSpan().TotalMinutes - primero) % intervalo != 0))
            throw new ValidationException("La hora debe coincidir con un turno del horario configurado.");
        var zona = TimeZoneInfo.FindSystemTimeZoneById(configuracion.ZonaHoraria);
        var minimo = ahora.AddMinutes(configuracion.AnticipacionMinimaMinutos).UtcDateTime;
        // Interpretación provisional de dos días: ventana móvil exacta de 48 h, no fechas calendario.
        var maximo = ahora.AddDays(configuracion.AnticipacionMaximaDias).UtcDateTime;
        var hoyLocal = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(ahora, zona).DateTime);
        var ultimaFecha = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(new DateTimeOffset(maximo), zona).DateTime);
        if (fecha < hoyLocal || fecha > ultimaFecha) return respuesta;

        DateTime Utc(DateTime local) => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), zona);
        var inicioDia = Utc(fecha.ToDateTime(horario.PrimerTurno));
        var finDia = Utc(fecha.ToDateTime(horario.UltimoTurno).AddMinutes(intervalo));
        var lineas = await db.LineasTiro.AsNoTracking().OrderBy(l => l.Numero)
            .Select(l => new { l.Id, l.Numero, l.Nombre, l.Activa }).ToListAsync(ct);
        var bloqueos = await db.BloqueosOperacion.AsNoTracking()
            .Where(b => b.Activo && b.InicioUtc < finDia && b.FinUtc > inicioDia)
            .Select(b => new { b.LineaId, b.InicioUtc, b.FinUtc }).ToListAsync(ct);
        var apartados = await db.LineasApartado.AsNoTracking()
            .Where(l => l.Apartado.VenceEnUtc > ahora.UtcDateTime && l.Apartado.InicioUtc < finDia && l.Apartado.FinOcupacionUtc > inicioDia)
            .Select(l => new { l.LineaId, l.Apartado.InicioUtc, l.Apartado.FinOcupacionUtc }).ToListAsync(ct);
        for (var minuto = primero; minuto <= ultimo; minuto += intervalo)
        {
            ct.ThrowIfCancellationRequested();
            var hora = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(minuto));
            if (filtro.Hora.HasValue && filtro.Hora != hora) continue;
            var local = fecha.ToDateTime(hora);
            var inicio = Utc(local);
            var finActividad = Utc(local.AddMinutes(configuracion.DuracionActividadMinutos));
            var finOcupacion = Utc(local.AddMinutes(intervalo));
            var anticipacionValida = inicio >= minimo && inicio <= maximo;
            var mapa = lineas.Select(linea =>
            {
                var estado = !linea.Activa ? EstadoLineaDisponibilidad.Inactiva
                    : !anticipacionValida ? EstadoLineaDisponibilidad.FueraDeAnticipacion
                    : bloqueos.Any(b => (b.LineaId == null || b.LineaId == linea.Id) && b.InicioUtc < finOcupacion && b.FinUtc > inicio)
                        ? EstadoLineaDisponibilidad.Bloqueada
                    : apartados.Any(a => a.LineaId == linea.Id && a.InicioUtc < finOcupacion && a.FinOcupacionUtc > inicio)
                        ? EstadoLineaDisponibilidad.Apartada : EstadoLineaDisponibilidad.Libre;
                return new LineaDisponibilidad(linea.Id, linea.Numero, linea.Nombre, estado == EstadoLineaDisponibilidad.Libre, estado);
            }).ToArray();
            var libres = mapa.Count(l => l.Disponible);
            resultado.Add(new TurnoDisponibilidad(hora, inicio, finActividad, finOcupacion,
                libres >= filtro.Personas, libres, mapa));
        }
        return respuesta;
    }
}
