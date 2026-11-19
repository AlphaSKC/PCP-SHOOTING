using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Security;

public sealed class AccesoAlimentosFilter(PcpShootingDbContext db) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var ct = context.HttpContext.RequestAborted;
        if (!Guid.TryParse(context.HttpContext.User.FindFirstValue("sub"), out var actorId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }
        var perfil = await db.PerfilesUsuario.AsNoTracking().SingleOrDefaultAsync(p => p.Id == actorId && p.Activo, ct);
        if (perfil?.Rol == RolUsuario.Administrador)
        {
            await next();
            return;
        }
        if (perfil?.Rol != RolUsuario.Recepcion)
        {
            Denegar(context);
            return;
        }
        var datos = context.ActionArguments.Values.OfType<GuardarProducto>().FirstOrDefault();
        if (datos is not null && datos.Categoria != CategoriaProducto.Alimento)
        {
            Denegar(context);
            return;
        }
        var filtro = context.ActionArguments.Values.OfType<FiltroProductosAdministrativo>().FirstOrDefault();
        if (filtro is not null)
        {
            if (filtro.Categoria.HasValue && filtro.Categoria != CategoriaProducto.Alimento)
            {
                Denegar(context);
                return;
            }
            filtro.Categoria = CategoriaProducto.Alimento;
        }
        var alertas = context.ActionArguments.Values.OfType<FiltroAlertasInventario>().FirstOrDefault();
        if (alertas is not null)
        {
            if (alertas.Categoria.HasValue && alertas.Categoria != CategoriaProducto.Alimento)
            {
                Denegar(context);
                return;
            }
            alertas.Categoria = CategoriaProducto.Alimento;
        }
        context.ActionArguments.TryGetValue("productoId", out var id);
        if (id is null) context.ActionArguments.TryGetValue("id", out id);
        if (id is Guid identificador)
        {
            var producto = await db.Productos.AsNoTracking().SingleOrDefaultAsync(p => p.Id == identificador, ct);
            if (producto is null)
            {
                context.Result = new NotFoundResult();
                return;
            }
            if (producto.Categoria != CategoriaProducto.Alimento)
            {
                Denegar(context);
                return;
            }
        }
        else if (datos is null && filtro is null && alertas is null)
        {
            Denegar(context);
            return;
        }
        await next();
    }

    private static void Denegar(ActionExecutingContext context) =>
        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Recepción solo puede gestionar alimentos."
        }) { StatusCode = StatusCodes.Status403Forbidden };
}