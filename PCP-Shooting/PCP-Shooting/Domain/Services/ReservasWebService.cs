using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Services;

public sealed class ReservasWebService(PcpShootingDbContext db, ITransaccionesPersistence transacciones, TimeProvider reloj,
    IDisponibilidadService disponibilidad, IOperacionService operacion) : IReservasWebService
{
    public async Task<ReservaWebConsulta> CrearAsync(Guid usuarioId, CrearReservaWeb datos, CancellationToken ct)
    {
        Validator.ValidateObject(datos, new ValidationContext(datos), true);
        if (usuarioId == Guid.Empty || datos.IdOperacion == Guid.Empty || datos.ApartadoId == Guid.Empty || datos.Participantes.Any(p => p is null))
            throw new ValidationException("Indica la operación, el apartado y los participantes válidos.");
        foreach (var p in datos.Participantes)
        {
            Validator.ValidateObject(p, new ValidationContext(p), true);
            if (p.LineaId == Guid.Empty || string.IsNullOrWhiteSpace(p.Nombre)) throw new ValidationException("Cada participante requiere nombre y línea.");
        }
        if (datos.Participantes.Select(p => p.LineaId).Distinct().Count() != datos.Participantes.Count)
            throw new ValidationException("Cada participante debe ocupar una línea distinta.");
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            datos.ApartadoId,
            Participantes = datos.Participantes.OrderBy(p => p.LineaId).Select(p => new { p.LineaId, Nombre = p.Nombre.Trim(), p.Nacimiento,
                TutorNombre = p.TutorNombre?.Trim(), p.TutorNacimiento })
        }))));
        try
        {
            return db.Database.IsRelational() ? await transacciones.EjecutarAsync(() => CrearInterno(usuarioId, datos, hash, ct), ct)
                : await CrearInterno(usuarioId, datos, hash, ct);
        }
        catch (OperacionException ex) when (ex.Status == 409)
        {
            var anterior = await Buscar(datos.IdOperacion, ct);
            if (anterior is not null && anterior.UsuarioId == usuarioId && anterior.SolicitudHash == hash) return Consulta(anterior);
            throw;
        }
    }

    public async Task<ReservaWebConsulta> CrearMostradorAsync(Guid actorId, CrearReservaMostrador datos, CancellationToken ct)
    {
        Validator.ValidateObject(datos, new ValidationContext(datos), true);
        if (actorId == Guid.Empty || datos.IdOperacion == Guid.Empty || datos.ClienteId == Guid.Empty || datos.PaqueteId == Guid.Empty ||
            datos.Participantes.Any(p => p is null) || datos.Participantes.Select(p => p.LineaId).Distinct().Count() != datos.Participantes.Count)
            throw new ValidationException("Indica el cliente, paquete y participantes válidos, con una línea distinta por persona.");
        foreach (var participante in datos.Participantes)
        {
            Validator.ValidateObject(participante, new ValidationContext(participante), true);
            if (participante.LineaId == Guid.Empty || string.IsNullOrWhiteSpace(participante.Nombre))
                throw new ValidationException("Cada participante requiere nombre y línea.");
        }
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            datos.ClienteId, datos.PaqueteId, datos.Fecha, datos.Hora,
            Participantes = datos.Participantes.OrderBy(p => p.LineaId).Select(p => new { p.LineaId, Nombre = p.Nombre.Trim(), p.Nacimiento,
                TutorNombre = p.TutorNombre?.Trim(), p.TutorNacimiento })
        }))));
        try
        {
            return db.Database.IsRelational()
                ? await transacciones.EjecutarAsync(() => CrearMostradorInternoAsync(actorId, datos, hash, ct), ct)
                : await CrearMostradorInternoAsync(actorId, datos, hash, ct);
        }
        catch (OperacionException ex) when (ex.Status == 409)
        {
            var anterior = await Buscar(datos.IdOperacion, ct);
            if (anterior is not null && anterior.CreadoPorId == actorId && anterior.UsuarioId == datos.ClienteId && anterior.SolicitudHash == hash)
                return Consulta(anterior);
            throw;
        }
    }

    private async Task<ReservaWebConsulta> CrearMostradorInternoAsync(Guid actorId, CrearReservaMostrador datos, string hash, CancellationToken ct)
    {
        if (!await db.PerfilesUsuario.AnyAsync(p => p.Id == actorId && p.Activo && (p.Rol == RolUsuario.Administrador || p.Rol == RolUsuario.Recepcion), ct))
            throw new OperacionException(403, "Solo Recepción o Administración puede registrar reservas de mostrador.");
        var existente = await Buscar(datos.IdOperacion, ct);
        if (existente is not null)
        {
            if (existente.CreadoPorId != actorId || existente.UsuarioId != datos.ClienteId || existente.SolicitudHash != hash)
                throw new OperacionException(409, "El ID de operación ya fue usado para otros datos.");
            return Consulta(existente);
        }
        if (!await db.PerfilesUsuario.AnyAsync(p => p.Id == datos.ClienteId && p.Activo && p.Rol == RolUsuario.Cliente, ct))
            throw new OperacionException(404, "El perfil del cliente no existe, está inactivo o no corresponde a un cliente registrado.");
        var ahora = reloj.GetUtcNow().UtcDateTime;
        var config = await operacion.ConsultarConfiguracionAsync(ct);
        var zonaClub = TimeZoneInfo.FindSystemTimeZoneById(config.ZonaHoraria);
        if (TimeZoneInfo.ConvertTime(reloj.GetUtcNow(), zonaClub).DateTime >= DateTime.SpecifyKind(datos.Fecha!.Value.ToDateTime(datos.Hora!.Value), DateTimeKind.Unspecified))
            throw new OperacionException(409, "No se puede registrar una reserva en un horario que ya comenzó.");
        var horario = config.Horarios.Single(h => h.DiaSemana == (int)datos.Fecha.Value.DayOfWeek);
        if (horario.Cerrado || datos.Hora.Value < horario.PrimerTurno || datos.Hora.Value > horario.UltimoTurno ||
            (datos.Hora.Value.ToTimeSpan() - horario.PrimerTurno.ToTimeSpan()).TotalMinutes % config.IntervaloTurnosMinutos != 0)
            throw new OperacionException(409, "El club está cerrado o el horario no pertenece a un turno disponible.");
        var disponibilidadTurno = await disponibilidad.ConsultarAsync(new ConsultaDisponibilidad
        {
            PaqueteId = datos.PaqueteId, Fecha = datos.Fecha, Hora = datos.Hora, Personas = datos.Participantes.Count
        }, ct);
        var turno = disponibilidadTurno.Horarios.SingleOrDefault() ?? throw new OperacionException(409, "No existe un turno disponible para ese horario.");
        var seleccion = turno.Lineas.Where(l => datos.Participantes.Any(p => p.LineaId == l.Id)).ToArray();
        if (seleccion.Length != datos.Participantes.Count) throw new ValidationException("Alguna línea seleccionada no existe.");
        if (seleccion.Any(l => !l.Disponible)) throw new OperacionException(409, "Alguna línea ya no está disponible para el horario elegido.");
        foreach (var p in datos.Participantes)
        {
            if (Edad(p.Nacimiento!.Value, datos.Fecha.Value) < 9) throw new ValidationException("La edad mínima es de nueve años en la fecha de la actividad.");
            var tieneTutor = !string.IsNullOrWhiteSpace(p.TutorNombre) && p.TutorNacimiento.HasValue;
            if (Edad(p.Nacimiento.Value, datos.Fecha.Value) < 18 && (!tieneTutor || Edad(p.TutorNacimiento!.Value, datos.Fecha.Value) < 18))
                throw new ValidationException("Un menor requiere nombre y nacimiento de su tutor adulto.");
            if ((p.TutorNombre is not null || p.TutorNacimiento.HasValue) && !tieneTutor) throw new ValidationException("Los datos del tutor deben estar completos.");
            if (tieneTutor && Edad(p.TutorNacimiento!.Value, datos.Fecha.Value) < 18) throw new ValidationException("El tutor debe ser mayor de edad.");
        }
        var lineas = datos.Participantes.Select(p => p.LineaId).ToArray();
        if (await db.BloqueosOperacion.AnyAsync(b => b.Activo && (b.LineaId == null || lineas.Contains(b.LineaId.Value)) &&
                b.InicioUtc < turno.FinOcupacionUtc && b.FinUtc > turno.InicioUtc, ct) ||
            await db.LineasApartado.AnyAsync(l => l.Apartado.VenceEnUtc > ahora && lineas.Contains(l.LineaId) &&
                l.Apartado.InicioUtc < turno.FinOcupacionUtc && l.Apartado.FinOcupacionUtc > turno.InicioUtc, ct) ||
            await db.ParticipantesReserva.AnyAsync(p => lineas.Contains(p.LineaId) && p.Reserva.InicioUtc < turno.FinOcupacionUtc &&
                p.Reserva.FinOcupacionUtc > turno.InicioUtc && (p.Reserva.PagoConfirmadoEnUtc != null || p.Reserva.VencePagoEnUtc > ahora ||
                    p.Reserva.Pagos.Any(pago => pago.Estado == EstadoPago.Pendiente)), ct))
            throw new OperacionException(409, "La selección ya no está disponible.");
        var paquete = await db.Paquetes.AsNoTracking().Include(p => p.Consumibles).ThenInclude(c => c.Producto).AsSingleQuery()
            .SingleOrDefaultAsync(p => p.Id == datos.PaqueteId, ct) ?? throw new OperacionException(404, "Paquete inexistente.");
        if (!paquete.Activo || !(paquete.PrecioPorPersona > 0) || paquete.Consumibles.Any(c => !c.Producto.Activo ||
                c.Producto.Categoria != CategoriaProducto.Consumible || c.Producto.EsVendible))
            throw new OperacionException(409, "El paquete no está disponible para reservar.");
        var total = paquete.PrecioPorPersona!.Value * datos.Participantes.Count;
        if (total > 9999999999.99m) throw new ValidationException("El importe total excede el límite permitido.");
        var reserva = new ReservaWeb
        {
            Id = datos.IdOperacion, ApartadoId = null, EsMostrador = true, UsuarioId = datos.ClienteId, CreadoPorId = actorId,
            PaqueteId = paquete.Id, PaqueteVersion = paquete.Version, CodigoPaquete = paquete.Codigo, NombrePaquete = paquete.Nombre,
            DescripcionPaquete = paquete.Descripcion, IncluyeMarkdown = paquete.IncluyeMarkdown, RequisitosMarkdown = paquete.RequisitosMarkdown,
            SolicitudHash = hash, PrecioPorPersona = paquete.PrecioPorPersona.Value, PrecioSocioInformativo = paquete.PrecioSocioCartaq,
            Total = total, Fecha = datos.Fecha.Value, Hora = datos.Hora.Value, ZonaHoraria = disponibilidadTurno.ZonaHoraria,
            InicioUtc = turno.InicioUtc, FinActividadUtc = turno.FinActividadUtc, FinOcupacionUtc = turno.FinOcupacionUtc,
            VencePagoEnUtc = ahora.AddMinutes(config.ApartadoPagoMinutos), CreadoEnUtc = ahora
        };
        foreach (var p in datos.Participantes)
            reserva.Participantes.Add(new ParticipanteReserva { Id = Guid.NewGuid(), LineaId = p.LineaId, Nombre = p.Nombre.Trim(),
                Nacimiento = p.Nacimiento!.Value, TutorNombre = p.TutorNombre?.Trim(), TutorNacimiento = p.TutorNacimiento });
        foreach (var c in paquete.Consumibles)
            reserva.Consumibles.Add(new ConsumoReserva { Id = Guid.NewGuid(), ProductoId = c.ProductoId, Sku = c.Producto.Sku,
                Nombre = c.Producto.Nombre, CantidadPorPersona = c.CantidadPorPersona,
                CantidadTotal = checked(c.CantidadPorPersona * datos.Participantes.Count) });
        if (reserva.VencePagoEnUtc <= reloj.GetUtcNow().UtcDateTime) throw new OperacionException(409, "El plazo de pago no es válido.");
        db.ReservasWeb.Add(reserva);
        await db.SaveChangesAsync(ct);
        return Consulta((await Buscar(reserva.Id, ct))!);
    }

    private async Task<ReservaWebConsulta> CrearInterno(Guid usuarioId, CrearReservaWeb datos, string hash, CancellationToken ct)
    {
        if (!await db.PerfilesUsuario.AnyAsync(p => p.Id == usuarioId && p.Activo, ct)) throw new OperacionException(403, "Se requiere un perfil activo.");
        var existente = await Buscar(datos.IdOperacion, ct);
        if (existente is not null)
        {
            if (existente.UsuarioId != usuarioId || existente.SolicitudHash != hash) throw new OperacionException(409, "El ID de operación ya fue usado para otros datos.");
            return Consulta(existente);
        }
        var apartado = await db.Apartados.AsNoTracking().Include(a => a.Lineas).AsSingleQuery()
            .SingleOrDefaultAsync(a => a.Id == datos.ApartadoId && a.UsuarioId == usuarioId, ct)
            ?? throw new OperacionException(404, "Apartado inexistente.");
        if (await db.ReservasWeb.AnyAsync(r => r.ApartadoId == apartado.Id, ct)) throw new OperacionException(409, "El apartado ya tiene una reserva.");
        var ahora = reloj.GetUtcNow().UtcDateTime;
        if (apartado.VenceEnUtc <= ahora || apartado.InicioUtc <= ahora) throw new OperacionException(409, "El apartado venció o la actividad ya comenzó.");
        var lineas = apartado.Lineas.Select(l => l.LineaId).OrderBy(id => id).ToArray();
        if (!lineas.SequenceEqual(datos.Participantes.Select(p => p.LineaId).OrderBy(id => id)))
            throw new ValidationException("Los participantes deben corresponder exactamente a las líneas apartadas.");
        foreach (var p in datos.Participantes)
        {
            if (Edad(p.Nacimiento!.Value, apartado.Fecha) < 9) throw new ValidationException("La edad mínima es de nueve años en la fecha de la actividad.");
            var tieneTutor = !string.IsNullOrWhiteSpace(p.TutorNombre) && p.TutorNacimiento.HasValue;
            if (Edad(p.Nacimiento.Value, apartado.Fecha) < 18 && (!tieneTutor || Edad(p.TutorNacimiento!.Value, apartado.Fecha) < 18))
                throw new ValidationException("Un menor requiere nombre y nacimiento de su tutor adulto.");
            if ((p.TutorNombre is not null || p.TutorNacimiento.HasValue) && !tieneTutor) throw new ValidationException("Los datos del tutor deben estar completos.");
            if (tieneTutor && Edad(p.TutorNacimiento!.Value, apartado.Fecha) < 18) throw new ValidationException("El tutor debe ser mayor de edad.");
        }
        if (await db.LineasTiro.CountAsync(l => lineas.Contains(l.Id) && l.Activa, ct) != lineas.Length ||
            await db.BloqueosOperacion.AnyAsync(b => b.Activo && (b.LineaId == null || lineas.Contains(b.LineaId.Value)) &&
                b.InicioUtc < apartado.FinOcupacionUtc && b.FinUtc > apartado.InicioUtc, ct) ||
            await db.LineasApartado.AnyAsync(l => l.ApartadoId != apartado.Id && lineas.Contains(l.LineaId) && l.Apartado.VenceEnUtc > ahora &&
                l.Apartado.InicioUtc < apartado.FinOcupacionUtc && l.Apartado.FinOcupacionUtc > apartado.InicioUtc, ct))
            throw new OperacionException(409, "La selección ya no está disponible.");
        var paquete = await db.Paquetes.AsNoTracking().Include(p => p.Consumibles).ThenInclude(c => c.Producto).AsSingleQuery()
            .SingleAsync(p => p.Id == apartado.PaqueteId, ct);
        if (!paquete.Activo || paquete.Version != apartado.PaqueteVersion || !(paquete.PrecioPorPersona > 0) ||
            paquete.Consumibles.Any(c => !c.Producto.Activo || c.Producto.Categoria != CategoriaProducto.Consumible || c.Producto.EsVendible))
            throw new OperacionException(409, "El paquete cambió o no está disponible. Revisa el catálogo y crea otro apartado.");
        var total = paquete.PrecioPorPersona!.Value * datos.Participantes.Count;
        if (total > 9999999999.99m) throw new ValidationException("El importe total excede el límite permitido.");
        var reserva = new ReservaWeb
        {
            Id = datos.IdOperacion, ApartadoId = apartado.Id, UsuarioId = usuarioId, CreadoPorId = usuarioId, PaqueteId = paquete.Id,
            PaqueteVersion = paquete.Version, CodigoPaquete = paquete.Codigo, NombrePaquete = paquete.Nombre, DescripcionPaquete = paquete.Descripcion,
            IncluyeMarkdown = paquete.IncluyeMarkdown, RequisitosMarkdown = paquete.RequisitosMarkdown, SolicitudHash = hash,
            PrecioPorPersona = paquete.PrecioPorPersona.Value, PrecioSocioInformativo = paquete.PrecioSocioCartaq, Total = total,
            Fecha = apartado.Fecha, Hora = apartado.Hora, ZonaHoraria = apartado.ZonaHoraria, InicioUtc = apartado.InicioUtc,
            FinActividadUtc = apartado.FinActividadUtc, FinOcupacionUtc = apartado.FinOcupacionUtc, VencePagoEnUtc = apartado.VenceEnUtc, CreadoEnUtc = ahora
        };
        foreach (var p in datos.Participantes) reserva.Participantes.Add(new ParticipanteReserva { Id = Guid.NewGuid(), LineaId = p.LineaId,
            Nombre = p.Nombre.Trim(), Nacimiento = p.Nacimiento!.Value, TutorNombre = p.TutorNombre?.Trim(), TutorNacimiento = p.TutorNacimiento });
        foreach (var c in paquete.Consumibles) reserva.Consumibles.Add(new ConsumoReserva { Id = Guid.NewGuid(), ProductoId = c.ProductoId,
            Sku = c.Producto.Sku, Nombre = c.Producto.Nombre, CantidadPorPersona = c.CantidadPorPersona, CantidadTotal = checked(c.CantidadPorPersona * lineas.Length) });
        // Revalidar el plazo después de cargar los datos, sin ampliarlo.
        if (apartado.VenceEnUtc <= reloj.GetUtcNow().UtcDateTime) throw new OperacionException(409, "El apartado venció durante la operación.");
        db.ReservasWeb.Add(reserva);
        await db.SaveChangesAsync(ct);
        return Consulta((await Buscar(reserva.Id, ct))!);
    }

    public async Task<ReservaWebConsulta?> ConsultarAsync(Guid usuarioId, Guid id, CancellationToken ct)
    {
        var reserva = await Cargar().SingleOrDefaultAsync(r => r.Id == id && r.UsuarioId == usuarioId, ct);
        return reserva is null ? null : Consulta(reserva);
    }
    public Task<PaginaConsulta<ReservaWebResumen>> ListarAsync(Guid usuarioId, FiltroPagina filtro, CancellationToken ct)
    {
        Validator.ValidateObject(filtro, new ValidationContext(filtro), true);
        var ahora = reloj.GetUtcNow().UtcDateTime;
        return PaginacionConsulta.CrearAsync(db.ReservasWeb.AsNoTracking().Where(r => r.UsuarioId == usuarioId)
            .OrderByDescending(r => r.CreadoEnUtc).ThenBy(r => r.Id).Select(r => new ReservaWebResumen(r.Id, r.ApartadoId, r.NombrePaquete,
                r.InicioUtc, r.VencePagoEnUtc, r.Total, r.PagoConfirmadoEnUtc != null ? EstadoReservaWeb.Pagada :
                    r.VencePagoEnUtc > ahora || r.Pagos.Any(p => p.Estado == EstadoPago.Pendiente) ? EstadoReservaWeb.PendientePago : EstadoReservaWeb.Vencida,
                r.Participantes.Count)), filtro, ct);
    }
    public Task<PaginaConsulta<ReservaAdministrativaResumen>> ListarAdministrativasAsync(FiltroReservasAdministrativas filtro, CancellationToken ct)
    {
        Validator.ValidateObject(filtro, new ValidationContext(filtro), true);
        var ahora = reloj.GetUtcNow().UtcDateTime;
        var consulta = db.ReservasWeb.AsNoTracking().Where(r => filtro.Fecha == null || r.Fecha == filtro.Fecha.Value);
        if (filtro.Estado == EstadoReservaWeb.PendientePago) consulta = consulta.Where(r => r.PagoConfirmadoEnUtc == null &&
            (r.VencePagoEnUtc > ahora || r.Pagos.Any(p => p.Estado == EstadoPago.Pendiente)));
        if (filtro.Estado == EstadoReservaWeb.Vencida) consulta = consulta.Where(r => r.PagoConfirmadoEnUtc == null &&
            r.VencePagoEnUtc <= ahora && !r.Pagos.Any(p => p.Estado == EstadoPago.Pendiente));
        if (filtro.Estado == EstadoReservaWeb.Pagada) consulta = consulta.Where(r => r.PagoConfirmadoEnUtc != null);
        return PaginacionConsulta.CrearAsync(consulta.OrderBy(r => r.InicioUtc).ThenBy(r => r.Id)
            .Select(r => new ReservaAdministrativaResumen(r.Id, r.UsuarioId, r.Usuario.Nombre, r.Usuario.Telefono,
                r.NombrePaquete, r.Fecha, r.Hora, r.InicioUtc, r.Total,
                r.PagoConfirmadoEnUtc != null ? EstadoReservaWeb.Pagada :
                    r.VencePagoEnUtc > ahora || r.Pagos.Any(p => p.Estado == EstadoPago.Pendiente) ? EstadoReservaWeb.PendientePago : EstadoReservaWeb.Vencida,
                r.Participantes.Count)), filtro, ct);
    }
    public async Task<ReservaAdministrativaConsulta?> ConsultarAdministrativaAsync(Guid id, CancellationToken ct)
    {
        var ahora = reloj.GetUtcNow().UtcDateTime;
        return await db.ReservasWeb.AsNoTracking().Where(r => r.Id == id).Select(r => new ReservaAdministrativaConsulta(
            r.Id, r.UsuarioId, r.Usuario.Nombre, r.Usuario.Telefono, r.PaqueteId, r.CodigoPaquete, r.NombrePaquete, r.Fecha, r.Hora,
            r.ZonaHoraria, r.InicioUtc, r.FinActividadUtc, r.FinOcupacionUtc, r.VencePagoEnUtc, r.CreadoEnUtc,
            r.PrecioPorPersona, r.Total, r.PagoConfirmadoEnUtc != null ? EstadoReservaWeb.Pagada :
                r.VencePagoEnUtc > ahora || r.Pagos.Any(p => p.Estado == EstadoPago.Pendiente) ? EstadoReservaWeb.PendientePago : EstadoReservaWeb.Vencida,
            r.Participantes.OrderBy(p => p.LineaId).Select(p => new ParticipanteAdministrativaConsulta(p.LineaId,
                db.LineasTiro.Where(l => l.Id == p.LineaId).Select(l => l.Numero).Single(), p.Nombre, p.Nacimiento, p.TutorNombre, p.TutorNacimiento)).ToArray())).SingleOrDefaultAsync(ct);
    }
    private IQueryable<ReservaWeb> Cargar() => db.ReservasWeb.AsNoTracking().Include(r => r.Participantes).ThenInclude(p => p.Linea).Include(r => r.Consumibles)
        .Include(r => r.Pagos).AsSingleQuery();
    private Task<ReservaWeb?> Buscar(Guid id, CancellationToken ct) => Cargar().SingleOrDefaultAsync(r => r.Id == id, ct);
    private static int Edad(DateOnly nacimiento, DateOnly fecha)
    {
        if (nacimiento > fecha) return -1;
        var edad = fecha.Year - nacimiento.Year;
        return nacimiento.AddYears(edad) > fecha ? edad - 1 : edad;
    }
    private ReservaWebConsulta Consulta(ReservaWeb r) => new(r.Id, r.ApartadoId, r.PaqueteId, r.CodigoPaquete, r.NombrePaquete,
        r.DescripcionPaquete, r.IncluyeMarkdown, r.RequisitosMarkdown, r.Fecha, r.Hora, r.ZonaHoraria, r.InicioUtc, r.FinActividadUtc,
        r.FinOcupacionUtc, r.VencePagoEnUtc, r.CreadoEnUtc, r.PrecioPorPersona, r.PrecioSocioInformativo, r.Total,
        r.PagoConfirmadoEnUtc != null ? EstadoReservaWeb.Pagada :
            r.VencePagoEnUtc > reloj.GetUtcNow().UtcDateTime || r.Pagos.Any(p => p.Estado == EstadoPago.Pendiente)
                ? EstadoReservaWeb.PendientePago : EstadoReservaWeb.Vencida,
        r.Participantes.OrderBy(p => p.LineaId).Select(p => new ParticipanteReservaConsulta(p.LineaId, p.Nombre, p.Nacimiento, p.TutorNombre, p.TutorNacimiento,
            p.Linea.Numero, p.Linea.Nombre)).ToArray(),
        r.Consumibles.OrderBy(c => c.ProductoId).Select(c => new ConsumoReservaConsulta(c.ProductoId, c.Sku, c.Nombre, c.CantidadPorPersona, c.CantidadTotal)).ToArray());
}
