using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Controllers;

[Authorize(Policy = "PerfilActivo")]
[ApiController]
[Route("api/membresias")]
public sealed class MembresiasController(IMembresiasService membresias, IUsuariosService usuarios) : ControllerBase
{
    [HttpGet("vigente")]
    public async Task<ActionResult<MembresiaVigente>> ConsultarVigente(
        [FromQuery] DateOnly? fechaLocal, CancellationToken cancellationToken)
    {
        var usuario = usuarios.ObtenerActual(User);
        if (usuario is null) return Unauthorized();
        if (fechaLocal is null) return BadRequest("Indica fechaLocal en formato AAAA-MM-DD.");

        var membresia = await membresias.ConsultarVigenteAsync(usuario.Id, fechaLocal.Value, cancellationToken);
        return membresia is null ? NotFound() : Ok(membresia);
    }
    [HttpPost("renovaciones")]
    [Authorize(Policy = "Administrador")]
    public async Task<ActionResult<EscrituraResultado>> Renovar(
        RenovarMembresia datos, [FromServices] IEscriturasService escrituras, CancellationToken cancellationToken) =>
        Ok(await escrituras.RenovarMembresiaAsync(datos, cancellationToken));

    [HttpGet("historial")]
    public async Task<ActionResult<PaginaConsulta<MembresiaHistorial>>> Historial(
        [FromQuery] FiltroPagina filtro, [FromServices] IAdministracionService administracion, CancellationToken ct)
    {
        var usuario = usuarios.ObtenerActual(User);
        if (usuario is null) return Unauthorized();
        return Ok(await administracion.HistorialMembresiasAsync(usuario.Id, filtro, ct));
    }
}
