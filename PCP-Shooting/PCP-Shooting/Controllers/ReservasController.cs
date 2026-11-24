using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using System.Security.Claims;
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

    [HttpPost("{id:guid}/checkout")]
    public async Task<ActionResult<CheckoutMercadoPago>> CrearCheckout(Guid id, [FromServices] IMercadoPagoService mercadoPago, CancellationToken ct)
    {
        var usuario = usuarios.ObtenerActual(User);
        if (usuario is null) return Unauthorized();
        return Ok(await mercadoPago.CrearCheckoutReservaAsync(usuario.Id, id, ct));
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

    [HttpPost("mostrador"), Authorize(Policy = "OperacionReservas")]
    public async Task<ActionResult<ReservaWebConsulta>> CrearMostrador(CrearReservaMostrador datos, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue("sub"), out var actorId)) return Unauthorized();
        var reserva = await reservas.CrearMostradorAsync(actorId, datos, ct);
        return CreatedAtAction(nameof(ConsultarAdministrativa), new { id = reserva.Id }, reserva);
    }

    [HttpGet("administracion"), Authorize(Policy = "OperacionReservas")]
    public async Task<ActionResult<PaginaConsulta<ReservaAdministrativaResumen>>> ListarAdministrativas(
        [FromQuery] FiltroReservasAdministrativas filtro, CancellationToken ct) =>
        Ok(await reservas.ListarAdministrativasAsync(filtro, ct));

    [HttpGet("administracion/{id:guid}"), Authorize(Policy = "OperacionReservas")]
    public async Task<ActionResult<ReservaAdministrativaConsulta>> ConsultarAdministrativa(Guid id, CancellationToken ct)
    {
        var reserva = await reservas.ConsultarAdministrativaAsync(id, ct);
        return reserva is null ? NotFound() : Ok(reserva);
    }

    [HttpPost("administracion/{id:guid}/checkout"), Authorize(Policy = "OperacionReservas")]
    public async Task<ActionResult<CheckoutMercadoPago>> CrearCheckoutMostrador(Guid id, [FromServices] IMercadoPagoService mercadoPago, CancellationToken ct) =>
        Ok(await mercadoPago.CrearCheckoutReservaMostradorAsync(id, ct));
}
