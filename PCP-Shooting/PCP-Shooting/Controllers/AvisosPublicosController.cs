using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Controllers;

[ApiController, Route("api/avisos-publicos"), AllowAnonymous]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AvisosPublicosController(IAvisosPublicosService avisos) : ControllerBase
{
    [HttpGet]
    public Task<PaginaConsulta<AvisoPublicoDto>> Listar([FromQuery] FiltroPagina filtro, CancellationToken ct) => avisos.ListarPublicosAsync(filtro, ct);
}

[ApiController, Route("api/administracion/avisos-publicos"), Authorize(Policy = "Administrador")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AvisosPublicosAdministracionController(IAvisosPublicosService avisos) : ControllerBase
{
    [HttpGet]
    public Task<PaginaConsulta<AvisoPublicoGestion>> Listar([FromQuery] FiltroAvisosPublicos filtro, CancellationToken ct) => avisos.ListarGestionAsync(filtro, ct);
    [HttpGet("{id:guid}")]
    public Task<AvisoPublicoGestion> Obtener(Guid id, CancellationToken ct) => avisos.ObtenerAsync(id, ct);
    [HttpPost]
    public async Task<ActionResult<AvisoPublicoGestion>> Crear(GuardarAvisoPublico datos, CancellationToken ct)
    {
        var creado = await avisos.GuardarAsync(null, datos, ct);
        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }
    [HttpPut("{id:guid}")]
    public Task<AvisoPublicoGestion> Editar(Guid id, GuardarAvisoPublico datos, CancellationToken ct) => avisos.GuardarAsync(id, datos, ct);
}
