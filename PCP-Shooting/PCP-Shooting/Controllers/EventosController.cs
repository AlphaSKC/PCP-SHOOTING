using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Controllers;

[ApiController, Route("api/eventos"), AllowAnonymous]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class EventosController(IEventosService eventos) : ControllerBase
{
    [HttpGet]
    public Task<PaginaConsulta<EventoResumen>> Listar([FromQuery] FiltroEventos filtro, CancellationToken ct) => eventos.ListarPublicosAsync(filtro, ct);
    [HttpGet("{slug}")]
    public async Task<ActionResult<EventoDetalle>> Consultar(string slug, CancellationToken ct)
    {
        var evento = await eventos.ConsultarPublicoAsync(slug, ct);
        return evento is null ? NotFound() : Ok(evento);
    }
}

[ApiController, Route("api/administracion/eventos"), Authorize(Policy = "Administrador")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class EventosAdministracionController(IEventosService eventos) : ControllerBase
{
    [HttpGet]
    public Task<PaginaConsulta<EventoGestion>> Listar([FromQuery] FiltroEventosGestion filtro, CancellationToken ct) => eventos.ListarGestionAsync(filtro, ct);
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EventoGestion>> Consultar(Guid id, CancellationToken ct)
    {
        var evento = await eventos.ConsultarGestionAsync(id, ct);
        return evento is null ? NotFound() : Ok(evento);
    }
    [HttpPost]
    public async Task<ActionResult<EventoGestion>> Crear(GuardarEvento datos, CancellationToken ct)
    {
        var evento = await eventos.GuardarAsync(null, datos, ct);
        return CreatedAtAction(nameof(Consultar), new { id = evento.Id }, evento);
    }
    [HttpPut("{id:guid}")]
    public Task<EventoGestion> Editar(Guid id, GuardarEvento datos, CancellationToken ct) => eventos.GuardarAsync(id, datos, ct);
}
