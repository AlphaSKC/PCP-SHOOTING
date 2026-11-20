using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Services;

public sealed class ApartadosService(PcpShootingDbContext db, IDisponibilidadService disponibilidad,
    IOperacionService operacion, ITransaccionesPersistence transacciones, TimeProvider reloj) : IApartadosService
{
    public async Task<ApartadoConsulta> CrearAsync(Guid usuarioId, CrearApartado datos, CancellationToken ct)
    {
        Validator.ValidateObject(datos, new ValidationContext(datos), true);
        if (usuarioId == Guid.Empty || datos.IdOperacion == Guid.Empty || datos.PaqueteId == Guid.Empty ||
            datos.Lineas.Any(id => id == Guid.Empty) || datos.Lineas.Distinct().Count() != datos.Lineas.Count)
            throw new ValidationException("Indica IDs válidos y una selección de líneas sin duplicados.");
        try
        {
            return db.Database.IsRelational()
                ? await transacciones.EjecutarAsync(() => CrearInternoAsync(usuarioId, datos, ct), ct)
                : await CrearInternoAsync(usuarioId, datos, ct);
        }
        catch (OperacionException ex) when (ex.Status == 409)
        {
            // Una inserción simultánea del mismo ID puede haber confirmado la misma operación.
            var existente = await BuscarAsync(datos.IdOperacion, ct);
            if (existente is not null && Coincide(existente, usuarioId, datos)) return Consulta(existente);
            throw;
        }
    }

    public async Task<ApartadoConsulta?> ConsultarAsync(Guid usuarioId, Guid id, CancellationToken ct)
    {
        var apartado = await db.Apartados.AsNoTracking().AsSingleQuery().Include(a => a.Lineas)
            .SingleOrDefaultAsync(a => a.Id == id && a.UsuarioId == usuarioId, ct);
        return apartado is null ? null : Consulta(apartado);
    }

    private async Task<ApartadoConsulta> CrearInternoAsync(Guid usuarioId, CrearApartado datos, CancellationToken ct)
    {
        if (!await db.PerfilesUsuario.AnyAsync(p => p.Id == usuarioId && p.Activo, ct))
            throw new OperacionException(403, "El apartado requiere un perfil activo.");
        var existente = await BuscarAsync(datos.IdOperacion, ct);
        if (existente is not null)
        {
            if (!Coincide(existente, usuarioId, datos)) throw new OperacionException(409, "El ID de operación ya fue utilizado para otra selección.");
            return Consulta(existente);
        }
        var estado = await disponibilidad.ConsultarAsync(new ConsultaDisponibilidad
        {
            PaqueteId = datos.PaqueteId, Fecha = datos.Fecha, Hora = datos.Hora, Personas = datos.Lineas.Count
        }, ct);
        var turno = estado.Horarios.SingleOrDefault();
        if (turno is null) throw new OperacionException(409, "No hay un turno disponible para la fecha elegida.");
        var seleccion = turno.Lineas.Where(l => datos.Lineas.Contains(l.Id)).ToArray();
        if (seleccion.Length != datos.Lineas.Count) throw new ValidationException("Alguna línea seleccionada no existe.");
        if (!turno.Disponible || seleccion.Any(l => !l.Disponible))
            throw new OperacionException(409, "Alguna línea ya no está disponible para el horario elegido.");
        var configuracion = await operacion.ConsultarConfiguracionAsync(ct);
        var paqueteVersion = await db.Paquetes.Where(p => p.Id == datos.PaqueteId).Select(p => p.Version).SingleAsync(ct);
        var ahora = reloj.GetUtcNow().UtcDateTime;
        var apartado = new Apartado
        {
            Id = datos.IdOperacion, UsuarioId = usuarioId, PaqueteId = datos.PaqueteId, PaqueteVersion = paqueteVersion,
            ConfiguracionVersion = configuracion.Version, Fecha = datos.Fecha!.Value, Hora = datos.Hora!.Value,
            ZonaHoraria = estado.ZonaHoraria, InicioUtc = turno.InicioUtc, FinActividadUtc = turno.FinActividadUtc,
            FinOcupacionUtc = turno.FinOcupacionUtc, CreadoEnUtc = ahora, VenceEnUtc = ahora.AddMinutes(configuracion.ApartadoPagoMinutos)
        };
        foreach (var linea in datos.Lineas)
            apartado.Lineas.Add(new LineaApartado { Id = Guid.NewGuid(), LineaId = linea });
        db.Apartados.Add(apartado);
        await db.SaveChangesAsync(ct);
        return Consulta(apartado);
    }

    private Task<Apartado?> BuscarAsync(Guid id, CancellationToken ct) => db.Apartados.AsNoTracking().AsSingleQuery()
        .Include(a => a.Lineas).SingleOrDefaultAsync(a => a.Id == id, ct);

    private static bool Coincide(Apartado apartado, Guid usuarioId, CrearApartado datos) =>
        apartado.UsuarioId == usuarioId && apartado.PaqueteId == datos.PaqueteId && apartado.Fecha == datos.Fecha && apartado.Hora == datos.Hora &&
        apartado.Lineas.Select(l => l.LineaId).OrderBy(id => id).SequenceEqual(datos.Lineas.OrderBy(id => id));

    private ApartadoConsulta Consulta(Apartado apartado) => new(apartado.Id, apartado.PaqueteId, apartado.Fecha, apartado.Hora,
        apartado.ZonaHoraria, apartado.InicioUtc, apartado.FinActividadUtc, apartado.FinOcupacionUtc,
        apartado.CreadoEnUtc, apartado.VenceEnUtc, apartado.VenceEnUtc > reloj.GetUtcNow().UtcDateTime ? EstadoApartado.Vigente : EstadoApartado.Vencido,
        apartado.Lineas.Select(l => l.LineaId).OrderBy(id => id).ToArray());
}
