using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
namespace PCP_Shooting.Controllers;

[ApiController, Route("api/contenido-sitio"), AllowAnonymous]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ContenidoSitioController(IContenidoSitioService contenido) : ControllerBase
{
    [HttpGet] public async Task<ContenidoSitioDatos> Consultar(CancellationToken ct) => (await contenido.ConsultarAsync(ct)).Contenido;
}
[ApiController, Route("api/administracion/contenido-sitio"), Authorize(Policy = "Administrador")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ContenidoSitioAdministracionController(IContenidoSitioService contenido) : ControllerBase
{
    [HttpGet] public Task<ContenidoSitioConsulta> Consultar(CancellationToken ct) => contenido.ConsultarAsync(ct);
    [HttpPut] public Task<ContenidoSitioConsulta> Guardar(GuardarContenidoSitio datos, CancellationToken ct) => contenido.GuardarAsync(datos, ct);
}
