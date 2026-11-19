using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Services;

public sealed class AdministradorInicialService(
    PcpShootingDbContext db, ISupabaseUsuariosPersistence cuentas, ITransaccionesPersistence transacciones)
    : IAdministradorInicialService
{
    public Task<Guid> InicializarAsync(string correo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(correo) || !new EmailAddressAttribute().IsValid(correo.Trim()))
            throw new OperacionException(400, "Indica el correo de una cuenta confirmada de Supabase Auth.");

        return transacciones.EjecutarAsync(async () =>
        {
            var usuarioId = await cuentas.BuscarCuentaConfirmadaAsync(correo.Trim(), ct);
            if (!usuarioId.HasValue)
                throw new OperacionException(404, "Primero registra y confirma esa cuenta en Supabase Auth.");
            var administradores = await db.PerfilesUsuario.AsNoTracking()
                .Where(p => p.Rol == RolUsuario.Administrador).Select(p => p.Id).ToListAsync(ct);
            if (administradores.Contains(usuarioId.Value)) return usuarioId.Value;
            if (administradores.Count > 0)
                throw new OperacionException(409, "El primer administrador ya existe. La asignación de roles continúa desde una cuenta administradora.");

            var perfil = await db.PerfilesUsuario.SingleOrDefaultAsync(p => p.Id == usuarioId.Value, ct);
            if (perfil is null)
            {
                perfil = new PerfilUsuario { Id = usuarioId.Value, Nombre = "Administrador" };
                db.PerfilesUsuario.Add(perfil);
            }
            perfil.Rol = RolUsuario.Administrador;
            perfil.Activo = true;
            return perfil.Id;
        }, ct);
    }
}