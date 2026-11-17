using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Controllers;

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
}
