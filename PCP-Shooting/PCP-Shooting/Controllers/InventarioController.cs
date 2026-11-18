using PCP_Shooting.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Interfaces;

namespace PCP_Shooting.Controllers;

[Authorize(Policy = "Administrador")]
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
    [HttpPost("{productoId:guid}/movimientos")]
    public async Task<ActionResult<EscrituraResultado>> Registrar(
        Guid productoId, RegistrarMovimiento datos,
        [FromServices] IEscriturasService escrituras, CancellationToken cancellationToken) =>
        Ok(await escrituras.RegistrarMovimientoAsync(productoId, datos, cancellationToken));

    [HttpGet("alertas")]
    public async Task<ActionResult<PaginaConsulta<AlertaInventario>>> Alertas(
        [FromQuery] FiltroPagina filtro, CancellationToken ct) =>
        Ok(await inventario.ConsultarAlertasAsync(filtro, ct));

    [HttpPut("{productoId:guid}/minimo")]
    public async Task<ActionResult<EscrituraResultado>> ConfigurarMinimo(
        Guid productoId, ConfigurarStockMinimo datos, [FromServices] IEscriturasService escrituras, CancellationToken ct) =>
        Ok(await escrituras.ConfigurarStockMinimoAsync(productoId, datos, ct));

}
