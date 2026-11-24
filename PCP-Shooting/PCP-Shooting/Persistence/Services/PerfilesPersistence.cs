using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Persistence.Services;

public sealed class PerfilesPersistence(PcpShootingDbContext db) : IPerfilesPersistence
{
    public Task<PerfilUsuario?> ConsultarAsync(Guid id, CancellationToken cancellationToken) =>
        db.PerfilesUsuario.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<PaginaPerfiles> ListarAsync(FiltroPerfiles filtro, CancellationToken cancellationToken)
    {
        var consulta = db.PerfilesUsuario.AsNoTracking();
        var busqueda = filtro.Busqueda?.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(busqueda))
            consulta = consulta.Where(p => p.Nombre.ToUpper().Contains(busqueda) ||
                (p.Telefono != null && p.Telefono.Contains(busqueda)));
        if (filtro.Rol.HasValue)
            consulta = consulta.Where(p => p.Rol == filtro.Rol);
        if (filtro.Activo.HasValue)
            consulta = consulta.Where(p => p.Activo == filtro.Activo);

        var total = await consulta.CountAsync(cancellationToken);
        var desplazamiento = ((long)filtro.Pagina - 1) * filtro.TamanoPagina;
        if (desplazamiento >= total)
            return new PaginaPerfiles(Array.Empty<PerfilConsulta>(), total, filtro.Pagina, filtro.TamanoPagina);

        var elementos = await consulta.OrderBy(p => p.Nombre).ThenBy(p => p.Id)
            .Skip((int)desplazamiento).Take(filtro.TamanoPagina)
            .Select(p => new PerfilConsulta(p.Id, p.Nombre, p.Telefono, p.Rol, p.Activo))
            .ToListAsync(cancellationToken);
        return new PaginaPerfiles(elementos, total, filtro.Pagina, filtro.TamanoPagina);
    }

    public Task GuardarPropioAsync(Guid id, string nombre, string? telefono, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlInterpolatedAsync(
            $"""
                INSERT INTO "PerfilesUsuario" ("Id", "Nombre", "Telefono", "Rol", "Activo", "CreadoEnUtc")
                VALUES ({id}, {nombre}, {telefono}, {0}, {true}, {DateTime.UtcNow})
                ON CONFLICT ("Id") DO UPDATE SET "Nombre" = EXCLUDED."Nombre", "Telefono" = EXCLUDED."Telefono"
                WHERE "PerfilesUsuario"."Activo" = true
                """, cancellationToken);

    public async Task<PerfilUsuario?> CambiarAccesoAsync(Guid id, CambiarAcceso datos, CancellationToken cancellationToken)
    {
        var perfil = await db.PerfilesUsuario.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (perfil is null) return null;
        perfil.Rol = datos.Rol;
        perfil.Activo = datos.Activo;
        await db.SaveChangesAsync(cancellationToken);
        return perfil;
    }
}