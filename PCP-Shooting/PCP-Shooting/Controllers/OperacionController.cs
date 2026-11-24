using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Controllers;

[ApiController]
[Route("api/administracion/operacion")]
[Authorize(Policy = "OperacionReservas")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class OperacionController(IOperacionService operacion, IUsuariosService usuarios) : ControllerBase
{
    [HttpGet("configuracion")]
    public async Task<ActionResult<ConfiguracionOperativaConsulta>> Configuracion(CancellationToken ct) =>
        Ok(await operacion.ConsultarConfiguracionAsync(ct));

    [HttpPut("configuracion"), Authorize(Policy = "Administrador")]
    public async Task<ActionResult<ConfiguracionOperativaConsulta>> GuardarConfiguracion(GuardarConfiguracionOperativa datos, CancellationToken ct) =>
        Ok(await operacion.GuardarConfiguracionAsync(datos, ct));

    [HttpGet("lineas")]
    public async Task<ActionResult<IReadOnlyList<LineaTiroConsulta>>> Lineas(CancellationToken ct) =>
        Ok(await operacion.ListarLineasAsync(ct));

    [HttpPut("lineas/{id:guid}"), Authorize(Policy = "Administrador")]
    public async Task<ActionResult<LineaTiroConsulta>> GuardarLinea(Guid id, GuardarLineaTiro datos, CancellationToken ct) =>
        Ok(await operacion.GuardarLineaAsync(id, datos, ct));

    [HttpGet("bloqueos")]
    public async Task<ActionResult<PaginaConsulta<BloqueoOperacionConsulta>>> Bloqueos([FromQuery] FiltroBloqueosOperacion filtro, CancellationToken ct) =>
        Ok(await operacion.ListarBloqueosAsync(filtro, ct));

    [HttpGet("bloqueos/{id:guid}")]
    public async Task<ActionResult<BloqueoOperacionConsulta>> Bloqueo(Guid id, CancellationToken ct)
    {
        var bloqueo = await operacion.ConsultarBloqueoAsync(id, ct);
        return bloqueo is null ? NotFound() : Ok(bloqueo);
    }

    [HttpPost("bloqueos")]
    public async Task<ActionResult<BloqueoOperacionConsulta>> CrearBloqueo(GuardarBloqueoOperacion datos, CancellationToken ct)
    {
        var usuario = usuarios.ObtenerActual(User);
        if (usuario is null) return Unauthorized();
        var bloqueo = await operacion.GuardarBloqueoAsync(null, usuario.Id, datos, ct);
        return CreatedAtAction(nameof(Bloqueo), new { id = bloqueo.Id }, bloqueo);
    }

    [HttpPut("bloqueos/{id:guid}")]
    public async Task<ActionResult<BloqueoOperacionConsulta>> EditarBloqueo(Guid id, GuardarBloqueoOperacion datos, CancellationToken ct)
    {
        var usuario = usuarios.ObtenerActual(User);
        if (usuario is null) return Unauthorized();
        return Ok(await operacion.GuardarBloqueoAsync(id, usuario.Id, datos, ct));
    }
}
