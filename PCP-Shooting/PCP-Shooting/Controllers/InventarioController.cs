using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Interfaces;

namespace PCP_Shooting.Controllers;

[ApiController]
[Route("api/inventario")]
public sealed class InventarioController(IInventarioService inventario, IUsuariosService usuarios) : ControllerBase
{
    [HttpGet("{productoId:guid}/existencia")]
    public async Task<ActionResult<long>> ConsultarExistencia(Guid productoId, CancellationToken cancellationToken)
    {
        var usuario = usuarios.ObtenerActual(User);
        if (usuario is null) return Unauthorized();
        if (usuario.Rol != RolUsuario.Administrador) return StatusCode(StatusCodes.Status403Forbidden);

        return Ok(await inventario.ConsultarExistenciaAsync(productoId, cancellationToken));
    }

    [HttpGet("{productoId:guid}/movimientos")]
    public async Task<ActionResult<IReadOnlyList<MovimientoConsulta>>> ConsultarHistorial(
        Guid productoId, CancellationToken cancellationToken)
    {
        var usuario = usuarios.ObtenerActual(User);
        if (usuario is null) return Unauthorized();
        if (usuario.Rol != RolUsuario.Administrador) return StatusCode(StatusCodes.Status403Forbidden);

        return Ok(await inventario.ConsultarHistorialAsync(productoId, cancellationToken));
    }
}
