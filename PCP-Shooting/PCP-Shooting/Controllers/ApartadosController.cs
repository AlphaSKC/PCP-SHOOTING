using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Controllers;

[ApiController]
[Authorize(Policy = "PerfilActivo")]
[Route("api/apartados")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ApartadosController(IApartadosService apartados, IUsuariosService usuarios) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<HistorialApartadosConsulta>> Listar([FromQuery] FiltroApartados filtro, CancellationToken ct)
    {
        var usuario = usuarios.ObtenerActual(User);
        if (usuario is null) return Unauthorized();
        return Ok(await apartados.ListarAsync(usuario.Id, filtro, ct));
    }

    [HttpPost]
    public async Task<ActionResult<ApartadoConsulta>> Crear(CrearApartado datos, CancellationToken ct)
    {
        var usuario = usuarios.ObtenerActual(User);
        if (usuario is null) return Unauthorized();
        var apartado = await apartados.CrearAsync(usuario.Id, datos, ct);
        return CreatedAtAction(nameof(Consultar), new { id = apartado.Id }, apartado);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApartadoConsulta>> Consultar(Guid id, CancellationToken ct)
    {
        var usuario = usuarios.ObtenerActual(User);
        if (usuario is null) return Unauthorized();
        var apartado = await apartados.ConsultarAsync(usuario.Id, id, ct);
        return apartado is null ? NotFound() : Ok(apartado);
    }
}
