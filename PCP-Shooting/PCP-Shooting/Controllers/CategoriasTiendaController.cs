using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
namespace PCP_Shooting.Controllers;

[ApiController, AllowAnonymous, Route("api/tienda/categorias")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CategoriasTiendaController(ICategoriasTiendaService categorias) : ControllerBase
{
    [HttpGet] public Task<IReadOnlyList<CategoriaTiendaPublica>> Listar(CancellationToken ct) => categorias.ListarPublicasAsync(ct);
}

[ApiController, Authorize(Policy = "Administrador"), Route("api/administracion/tienda/categorias")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CategoriasTiendaAdministracionController(ICategoriasTiendaService categorias) : ControllerBase
{
    [HttpGet] public Task<IReadOnlyList<CategoriaTiendaGestion>> Listar(CancellationToken ct) => categorias.ListarGestionAsync(ct);
    [HttpPost] public async Task<ActionResult<CategoriaTiendaGestion>> Crear(GuardarCategoriaTienda datos, CancellationToken ct)
    { var categoria = await categorias.GuardarAsync(null, datos, ct); return Created("/api/administracion/tienda/categorias", categoria); }
    [HttpPut("{id:guid}")] public Task<CategoriaTiendaGestion> Editar(Guid id, GuardarCategoriaTienda datos, CancellationToken ct) => categorias.GuardarAsync(id, datos, ct);
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    { await categorias.EliminarAsync(id, ct); return NoContent(); }
}
