using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Controllers;

[ApiController, Route("api/marcas"), AllowAnonymous]
public sealed class MarcasController(IMarcasService marcas) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<MarcaPublica>> Listar(CancellationToken ct) => marcas.ListarPublicasAsync(ct);
}

[ApiController, Route("api/administracion/marcas"), Authorize(Policy = "Administrador")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class MarcasAdministracionController(IMarcasService marcas) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<MarcaGestion>> Listar(CancellationToken ct) => marcas.ListarGestionAsync(ct);
    [HttpPost]
    public async Task<ActionResult<MarcaGestion>> Crear(GuardarMarca datos, CancellationToken ct)
    {
        var creada = await marcas.GuardarAsync(null, datos, ct);
        return Created("/api/administracion/marcas", creada);
    }
    [HttpPut("{id:guid}")]
    public Task<MarcaGestion> Editar(Guid id, GuardarMarca datos, CancellationToken ct) => marcas.GuardarAsync(id, datos, ct);
    [HttpDelete("{id:guid}/logo")]
    public async Task<IActionResult> QuitarLogo(Guid id, CancellationToken ct) { await marcas.QuitarLogoAsync(id, ct); return NoContent(); }
}
