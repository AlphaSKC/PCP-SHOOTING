using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Services;

public sealed class ContactoService(PcpShootingDbContext db, TimeProvider reloj) : IContactoService
{
    public async Task<ContactoRecibido> RecibirAsync(CrearSolicitudContacto datos, CancellationToken ct)
    {
        Validar(datos);
        var nombre = datos.Nombre.Trim(); var email = datos.Email.Trim(); var telefono = string.IsNullOrWhiteSpace(datos.Telefono) ? null : datos.Telefono.Trim();
        var mensaje = datos.Mensaje.Trim();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { nombre, email, telefono, datos.Motivo, mensaje })))).ToLowerInvariant();
        var existente = await db.Set<SolicitudContacto>().AsNoTracking().SingleOrDefaultAsync(s => s.Id == datos.IdOperacion, ct);
        if (existente is not null)
        {
            if (existente.SolicitudHash != hash) throw new OperacionException(409, "El identificador corresponde a otra solicitud. Usa una operación nueva para otro mensaje.");
            return new(existente.Id);
        }
        var ahora = reloj.GetUtcNow().UtcDateTime;
        db.Add(new SolicitudContacto { Id = datos.IdOperacion, Nombre = nombre, Email = email, Telefono = telefono, Motivo = datos.Motivo,
            Mensaje = mensaje, SolicitudHash = hash, CreadoEnUtc = ahora, PrivacidadAceptadaEnUtc = ahora,
            Estado = EstadoSolicitudContacto.Pendiente, Version = Guid.NewGuid(), ActualizadoEnUtc = ahora });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        { db.ChangeTracker.Clear(); throw new OperacionException(409, "Reintenta el envío con el mismo IdOperacion."); }
        return new(datos.IdOperacion);
    }
    public Task<PaginaConsulta<ContactoResumen>> ListarAsync(FiltroSolicitudesContacto filtro, CancellationToken ct)
    {
        Validar(filtro);
        var consulta = db.Set<SolicitudContacto>().AsNoTracking();
        if (filtro.Estado.HasValue) consulta = consulta.Where(s => s.Estado == filtro.Estado);
        if (!string.IsNullOrEmpty(filtro.Motivo)) consulta = consulta.Where(s => s.Motivo == filtro.Motivo);
        if (filtro.DesdeUtc.HasValue) consulta = consulta.Where(s => s.CreadoEnUtc >= filtro.DesdeUtc.Value.UtcDateTime);
        if (filtro.HastaUtc.HasValue) consulta = consulta.Where(s => s.CreadoEnUtc < filtro.HastaUtc.Value.UtcDateTime);
        var buscar = filtro.Busqueda?.Trim().ToUpperInvariant();
        if (!string.IsNullOrEmpty(buscar)) consulta = consulta.Where(s => s.Nombre.ToUpper().Contains(buscar) || s.Email.ToUpper().Contains(buscar));
        return PaginacionConsulta.CrearAsync(consulta.OrderByDescending(s => s.CreadoEnUtc).ThenBy(s => s.Id)
            .Select(s => new ContactoResumen(s.Id, s.Nombre, s.Email, s.Motivo, s.Estado, s.CreadoEnUtc)), filtro, ct);
    }
    public async Task<ContactoDetalle?> ConsultarAsync(Guid id, CancellationToken ct)
    {
        var solicitud = await db.Set<SolicitudContacto>().AsNoTracking().SingleOrDefaultAsync(s => s.Id == id, ct);
        return solicitud is null ? null : Detalle(solicitud);
    }
    public async Task<ContactoDetalle> ActualizarAsync(Guid id, ActualizarSolicitudContacto datos, CancellationToken ct)
    {
        Validar(datos);
        if (datos.Version == Guid.Empty) throw new ValidationException("La versión actual es obligatoria.");
        var s = await db.Set<SolicitudContacto>().SingleOrDefaultAsync(s => s.Id == id, ct) ?? throw new OperacionException(404, "Solicitud inexistente.");
        if (s.Version != datos.Version) throw new OperacionException(409, "La solicitud cambió. Recarga y revisa antes de guardar.");
        s.Estado = datos.Estado; s.NotasInternas = string.IsNullOrWhiteSpace(datos.NotasInternas) ? null : datos.NotasInternas.Trim();
        s.Version = Guid.NewGuid(); s.ActualizadoEnUtc = reloj.GetUtcNow().UtcDateTime;
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); throw new OperacionException(409, "La solicitud cambió. Recarga y revisa antes de guardar."); }
        return Detalle(s);
    }
    private static void Validar(object datos) => Validator.ValidateObject(datos, new ValidationContext(datos), true);
    private static ContactoDetalle Detalle(SolicitudContacto s) => new(s.Id, s.Nombre, s.Email, s.Telefono, s.Motivo, s.Mensaje, s.CreadoEnUtc,
        s.PrivacidadAceptadaEnUtc, s.Estado, s.NotasInternas, s.Version, s.ActualizadoEnUtc);
}
