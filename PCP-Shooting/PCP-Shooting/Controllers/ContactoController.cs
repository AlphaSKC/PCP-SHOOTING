using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
namespace PCP_Shooting.Controllers;

[ApiController, AllowAnonymous, Route("api/contacto")]
public sealed class ContactoController(IContactoService contacto) : ControllerBase
{
    [HttpPost, EnableRateLimiting("Contacto"), RequestSizeLimit(16 * 1024)]
    public async Task<ActionResult<ContactoRecibido>> Recibir(CrearSolicitudContacto datos, CancellationToken ct)
        => StatusCode(StatusCodes.Status201Created, await contacto.RecibirAsync(datos, ct));
}
[ApiController, Authorize(Policy = "Administrador"), Route("api/administracion/contacto")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ContactoAdministracionController(IContactoService contacto) : ControllerBase
{
    [HttpGet] public Task<PaginaConsulta<ContactoResumen>> Listar([FromQuery] FiltroSolicitudesContacto filtro, CancellationToken ct) => contacto.ListarAsync(filtro, ct);
    [HttpGet("{id:guid}")] public async Task<ActionResult<ContactoDetalle>> Consultar(Guid id, CancellationToken ct)
    { var s = await contacto.ConsultarAsync(id, ct); return s is null ? NotFound() : Ok(s); }
    [HttpPut("{id:guid}")] public Task<ContactoDetalle> Actualizar(Guid id, ActualizarSolicitudContacto datos, CancellationToken ct) => contacto.ActualizarAsync(id, datos, ct);
}
