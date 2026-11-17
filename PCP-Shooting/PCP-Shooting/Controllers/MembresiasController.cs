using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Controllers;

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
}
