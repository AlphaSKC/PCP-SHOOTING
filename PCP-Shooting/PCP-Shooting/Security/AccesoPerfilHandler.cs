using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Persistence;

namespace PCP_Shooting.Security;

public sealed record AccesoPerfilRequirement(bool Administrador) : IAuthorizationRequirement;

public sealed class AccesoPerfilHandler(PcpShootingDbContext db)
    : AuthorizationHandler<AccesoPerfilRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, AccesoPerfilRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true ||
            !Guid.TryParse(context.User.FindFirstValue("sub"), out var id)) return;

        var perfil = await db.PerfilesUsuario.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id);
        if (perfil is null || !perfil.Activo ||
            (requirement.Administrador && perfil.Rol != RolUsuario.Administrador)) return;

        context.Succeed(requirement);
    }
}