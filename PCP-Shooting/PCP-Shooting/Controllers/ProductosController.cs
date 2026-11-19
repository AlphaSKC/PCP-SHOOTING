using PCP_Shooting.Security;
using Microsoft.AspNetCore.Authorization;
using PCP_Shooting.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Interfaces;

namespace PCP_Shooting.Controllers;

[ApiController]
[Route("api/productos")]
public sealed class ProductosController(IProductosService productos) : ControllerBase
{
    [HttpGet("{sku}")]
    public async Task<ActionResult<FichaProducto>> Consultar(string sku, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sku)) return BadRequest();

        var ficha = await productos.ConsultarFichaAsync(sku, cancellationToken);
        return ficha is null ? NotFound() : Ok(ficha);
    }
    [HttpPost]
    [Authorize(Policy = "OperacionPedidos")]
    [ServiceFilter(typeof(AccesoAlimentosFilter))]
    public async Task<ActionResult<EscrituraResultado>> Crear(
        GuardarProducto datos,
        [FromServices] IEscriturasService escrituras, CancellationToken cancellationToken) =>
        Ok(await escrituras.GuardarProductoAsync(null, datos, cancellationToken));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "OperacionPedidos")]
    [ServiceFilter(typeof(AccesoAlimentosFilter))]
    public async Task<ActionResult<EscrituraResultado>> Actualizar(
        Guid id, GuardarProducto datos,
        [FromServices] IEscriturasService escrituras, CancellationToken cancellationToken) =>
        Ok(await escrituras.GuardarProductoAsync(id, datos, cancellationToken));

    [HttpGet]
    public async Task<ActionResult<PaginaConsulta<ProductoCatalogo>>> Listar(
        [FromQuery] FiltroProductos filtro, CancellationToken ct) =>
        Ok(await productos.ListarCatalogoAsync(filtro, ct));

    [HttpPatch("{id:guid}/estado")]
    [Authorize(Policy = "OperacionPedidos")]
    [ServiceFilter(typeof(AccesoAlimentosFilter))]
    public async Task<ActionResult<EscrituraResultado>> CambiarEstado(
        Guid id, CambiarEstadoProducto datos, [FromServices] IEscriturasService escrituras, CancellationToken ct) =>
        Ok(await escrituras.CambiarEstadoProductoAsync(id, datos, ct));

}
