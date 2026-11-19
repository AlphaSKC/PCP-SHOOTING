using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Controllers;

[Authorize(Policy = "PerfilActivo")]
[ApiController]
[Route("api/pedidos")]
public sealed class PedidosController(IPedidosService pedidos, IUsuariosService usuarios) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PedidoResumen>>> Listar(CancellationToken cancellationToken)
    {
        var usuario = usuarios.ObtenerActual(User);
        if (usuario is null) return Unauthorized();

        return Ok(await pedidos.ListarPorUsuarioAsync(usuario.Id, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PedidoConsulta>> Consultar(Guid id, CancellationToken cancellationToken)
    {
        var usuario = usuarios.ObtenerActual(User);
        if (usuario is null) return Unauthorized();

        var pedido = await pedidos.ConsultarAsync(usuario.Id, id, cancellationToken);
        return pedido is null ? NotFound() : Ok(pedido);
    }
    [HttpPost]
    public async Task<ActionResult<EscrituraResultado>> Crear(
        CrearPedido datos, [FromServices] IEscriturasService escrituras,
        [FromServices] IAuthorizationService autorizacion, CancellationToken cancellationToken)
    {
        var usuario = usuarios.ObtenerActual(User);
        if (usuario is null) return Unauthorized();
        var destino = datos.UsuarioId ?? usuario.Id;
        if (destino == Guid.Empty) return Problem(statusCode: 400, title: "El usuario requiere un identificador válido.");
        if (destino != usuario.Id && !(await autorizacion.AuthorizeAsync(User, "OperacionPedidos")).Succeeded)
            return Forbid();
        return Ok(await escrituras.CrearPedidoAsync(destino, datos, cancellationToken));
    }

    [HttpPost("{id:guid}/pagos")]
    [Authorize(Policy = "OperacionPedidos")]
    public async Task<ActionResult<EscrituraResultado>> RegistrarPago(
        Guid id, RegistrarPago datos, [FromServices] IEscriturasService escrituras, CancellationToken cancellationToken) =>
        Ok(await escrituras.RegistrarPagoAsync(id, datos, cancellationToken));

    [HttpPost("{id:guid}/recogida")]
    [Authorize(Policy = "OperacionPedidos")]
    public async Task<ActionResult<EscrituraResultado>> Recoger(
        Guid id, [FromServices] IEscriturasService escrituras, CancellationToken cancellationToken) =>
        Ok(await escrituras.RecogerPedidoAsync(id, cancellationToken));

    [HttpPost("{id:guid}/cancelacion")]
    public async Task<ActionResult<EscrituraResultado>> Cancelar(
        Guid id, [FromServices] IEscriturasService escrituras, CancellationToken ct)
    {
        var usuario = usuarios.ObtenerActual(User);
        if (usuario is null) return Unauthorized();
        return Ok(await escrituras.CancelarPedidoAsync(usuario.Id, id, ct));
    }
}
