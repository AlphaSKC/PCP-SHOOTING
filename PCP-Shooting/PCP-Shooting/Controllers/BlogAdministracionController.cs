using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Controllers;

[ApiController]
[Authorize(Policy = "Administrador")]
[Route("api/administracion/blog")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class BlogAdministracionController(IBlogService blog, IUsuariosService usuarios) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginaConsulta<ArticuloBlogGestion>>> Listar([FromQuery] FiltroBlogGestion filtro, CancellationToken ct) =>
        Ok(await blog.ListarGestionAsync(filtro, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ArticuloBlogGestion>> Consultar(Guid id, CancellationToken ct)
    {
        var articulo = await blog.ConsultarGestionAsync(id, ct);
        return articulo is null ? NotFound() : Ok(articulo);
    }

    [HttpPost]
    public async Task<ActionResult<ArticuloBlogGestion>> Crear(GuardarArticuloBlog datos, CancellationToken ct)
    {
        var usuario = usuarios.ObtenerActual(User);
        if (usuario is null) return Unauthorized();
        var articulo = await blog.GuardarAsync(null, usuario.Id, datos, ct);
        return CreatedAtAction(nameof(Consultar), new { id = articulo.Id }, articulo);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ArticuloBlogGestion>> Editar(Guid id, GuardarArticuloBlog datos, CancellationToken ct)
    {
        var usuario = usuarios.ObtenerActual(User);
        if (usuario is null) return Unauthorized();
        return Ok(await blog.GuardarAsync(id, usuario.Id, datos, ct));
    }
}
