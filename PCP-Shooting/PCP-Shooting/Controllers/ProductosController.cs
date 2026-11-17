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
}
