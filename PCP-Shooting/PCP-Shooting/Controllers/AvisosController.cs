using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Controllers;

[ApiController, Authorize(Policy = "Administrador"), Route("api/administracion/avisos")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AvisosController(IAvisosService avisos) : ControllerBase
{
    [HttpGet]
    public Task<AvisosConsulta> Consultar(CancellationToken ct) => avisos.ConsultarAsync(ct);
    [HttpPut]
    public Task<AvisosConsulta> Guardar(GuardarAvisos datos, CancellationToken ct) => avisos.GuardarAsync(datos, ct);
}
