using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/blog")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class BlogController(IBlogService blog) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginaConsulta<ArticuloBlogResumen>>> Listar([FromQuery] FiltroBlog filtro, CancellationToken ct) =>
        Ok(await blog.ListarPublicosAsync(filtro, ct));

    [HttpGet("{slug}")]
    public async Task<ActionResult<ArticuloBlogDetalle>> Consultar(string slug, CancellationToken ct)
    {
        var articulo = await blog.ConsultarPublicoAsync(slug, ct);
        return articulo is null ? NotFound() : Ok(articulo);
    }
}
