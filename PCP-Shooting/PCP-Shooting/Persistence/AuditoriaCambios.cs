using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Persistence;

internal static class AuditoriaCambios
{
    public static List<EventoAuditoria> Preparar(PcpShootingDbContext db, HttpContext? solicitud)
    {
        db.ChangeTracker.DetectChanges();
        Guid? actorId = Guid.TryParse(solicitud?.User.FindFirstValue("sub"), out var id) ? id : null;
        var eventos = db.ChangeTracker.Entries()
            .Where(entry => entry.Entity is not EventoAuditoria &&
                entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(entry => Crear(entry, actorId, solicitud?.TraceIdentifier))
            .ToList();
        db.AddRange(eventos);
        return eventos;
    }

    private static EventoAuditoria Crear(EntityEntry entry, Guid? actorId, string? solicitudId) => new()
    {
        Id = Guid.NewGuid(),
        ActorId = actorId,
        Entidad = entry.Metadata.ClrType.Name,
        EntidadId = (Guid)entry.Property("Id").CurrentValue!,
        Accion = entry.State.ToString(),
        Campos = string.Join(",", entry.Properties
            .Where(property => entry.State != EntityState.Modified || property.IsModified)
            .Select(property => property.Metadata.Name).OrderBy(nombre => nombre)),
        FechaUtc = DateTime.UtcNow,
        SolicitudId = solicitudId
    };
}