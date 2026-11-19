using PCP_Shooting.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Controllers;

[ApiController]
[Route("api/administracion")]
[Authorize(Policy = "OperacionPedidos")]
public sealed class AdministracionController(IAdministracionService administracion) : ControllerBase
{
    [HttpGet("pedidos")]
    public async Task<ActionResult<PaginaConsulta<PedidoAdministrativo>>> Pedidos(
        [FromQuery] FiltroPedidos filtro, CancellationToken ct) =>
        Ok(await administracion.ListarPedidosAsync(filtro, ct));

    [HttpGet("pedidos/{id:guid}")]
    public async Task<ActionResult<PedidoDetalleAdministrativo>> Pedido(Guid id, CancellationToken ct)
    {
        var pedido = await administracion.ConsultarPedidoAsync(id, ct);
        return pedido is null ? NotFound() : Ok(pedido);
    }

    [Authorize(Policy = "Administrador")]
    [HttpGet("pagos")]
    public async Task<ActionResult<PaginaConsulta<PagoAdministrativo>>> Pagos(
        [FromQuery] FiltroPagos filtro, CancellationToken ct) =>
        Ok(await administracion.ListarPagosAsync(filtro, ct));

    [Authorize(Policy = "Administrador")]
    [HttpGet("usuarios/{id:guid}/membresias")]
    public async Task<ActionResult<PaginaConsulta<MembresiaHistorial>>> Membresias(
        Guid id, [FromQuery] FiltroPagina filtro, CancellationToken ct) =>
        Ok(await administracion.HistorialMembresiasAsync(id, filtro, ct));

    [Authorize(Policy = "Administrador")]
    [HttpGet("auditoria")]
    public async Task<ActionResult<PaginaConsulta<AuditoriaConsulta>>> Auditoria(
        [FromQuery] FiltroAuditoria filtro, CancellationToken ct) =>
        Ok(await administracion.ConsultarAuditoriaAsync(filtro, ct));
    [ServiceFilter(typeof(AccesoAlimentosFilter))]
    [HttpGet("productos")]
    public async Task<ActionResult<PaginaConsulta<ProductoGestion>>> Productos(
        [FromQuery] FiltroProductosAdministrativo filtro, [FromServices] IProductosService productos, CancellationToken ct) =>
        Ok(await productos.ListarGestionAsync(filtro, ct));

    [Authorize(Policy = "Administrador")]
    [HttpGet("usuarios/{id:guid}/historial")]
    public async Task<ActionResult<HistorialCliente>> HistorialCliente(
        Guid id, [FromQuery] FiltroHistorialCliente filtro, CancellationToken ct)
    {
        var historial = await administracion.ConsultarHistorialClienteAsync(id, filtro, ct);
        return historial is null ? NotFound() : Ok(historial);
    }

}