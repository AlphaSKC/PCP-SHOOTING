using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
namespace PCP_Shooting.Controllers;

[ApiController, Authorize(Policy = "PerfilActivo"), Route("api/reservas")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ReservasController(IReservasWebService reservas, IUsuariosService usuarios) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ReservaWebConsulta>> Crear(CrearReservaWeb datos, CancellationToken ct)
    {
        var usuario = usuarios.ObtenerActual(User);
        if (usuario is null) return Unauthorized();
        var reserva = await reservas.CrearAsync(usuario.Id, datos, ct);
        return CreatedAtAction(nameof(Consultar), new { id = reserva.Id }, reserva);
    }
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReservaWebConsulta>> Consultar(Guid id, CancellationToken ct)
    {
        var usuario = usuarios.ObtenerActual(User);
        if (usuario is null) return Unauthorized();
        var reserva = await reservas.ConsultarAsync(usuario.Id, id, ct);
        return reserva is null ? NotFound() : Ok(reserva);
    }
    [HttpGet]
    public async Task<ActionResult<PaginaConsulta<ReservaWebResumen>>> Listar([FromQuery] FiltroPagina filtro, CancellationToken ct)
    {
        var usuario = usuarios.ObtenerActual(User);
        if (usuario is null) return Unauthorized();
        return Ok(await reservas.ListarAsync(usuario.Id, filtro, ct));
    }
}
