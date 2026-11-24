using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/disponibilidad")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class DisponibilidadController(IDisponibilidadService disponibilidad) : ControllerBase
{
    [HttpGet("ventana")]
    public async Task<ActionResult<VentanaDisponibilidadConsulta>> Ventana(CancellationToken ct) =>
        Ok(await disponibilidad.ConsultarVentanaAsync(ct));

    [HttpGet]
    public async Task<ActionResult<DisponibilidadConsulta>> Consultar([FromQuery] ConsultaDisponibilidad filtro, CancellationToken ct) =>
        Ok(await disponibilidad.ConsultarAsync(filtro, ct));
}
