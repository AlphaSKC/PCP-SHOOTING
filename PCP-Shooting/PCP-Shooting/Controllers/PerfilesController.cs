using PCP_Shooting.Domain.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Interfaces;

namespace PCP_Shooting.Controllers;

[ApiController]
[Route("api/perfiles")]
[Authorize]
public sealed class PerfilesController(IPerfilesService perfiles) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Administrador")]
    public async Task<ActionResult<PaginaPerfiles>> Listar(
        [FromQuery] FiltroPerfiles filtro, CancellationToken cancellationToken) =>
        Ok(await perfiles.ListarAsync(filtro, cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Administrador")]
    public async Task<ActionResult<PerfilConsulta>> ConsultarPorId(Guid id, CancellationToken cancellationToken)
    {
        var perfil = await perfiles.ConsultarAsync(id, cancellationToken);
        return perfil is null ? NotFound() : Ok(perfil);
    }
    [HttpGet("yo")]
    public async Task<ActionResult<PerfilConsulta>> Consultar(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue("sub"), out var id)) return Unauthorized();
        var perfil = await perfiles.ConsultarAsync(id, cancellationToken);
        return perfil is null ? NotFound() : Ok(perfil);
    }

    [HttpPut("yo")]
    public async Task<ActionResult<PerfilConsulta>> Guardar(
        EditarPerfil datos, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue("sub"), out var id)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(datos.Nombre)) return Problem(statusCode: 400, title: "El nombre es obligatorio.");
        var perfil = await perfiles.GuardarPropioAsync(id, datos, cancellationToken);
        return perfil.Activo ? Ok(perfil) : StatusCode(StatusCodes.Status403Forbidden);
    }

    [HttpPatch("{id:guid}/acceso")]
    [Authorize(Policy = "Administrador")]
    public async Task<ActionResult<PerfilConsulta>> CambiarAcceso(
        Guid id, CambiarAcceso datos, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(User.FindFirstValue("sub"), out var actualId) && actualId == id)
            return Problem(statusCode: 400, title: "La modificación de tu propio acceso requiere otro administrador.");

        var perfil = await perfiles.CambiarAccesoAsync(id, datos, cancellationToken);
        return perfil is null ? NotFound() : Ok(perfil);
    }
}