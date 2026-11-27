using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence.Interfaces;
namespace PCP_Shooting.Controllers;

[ApiController,AllowAnonymous,Route("api/portadas")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PortadasController(IPortadasService portadas) : ControllerBase
{
    [HttpGet] public Task<IReadOnlyList<PortadaPublica>> Listar(CancellationToken ct) => portadas.ListarPublicasAsync(ct);
}
[ApiController,Authorize(Policy = "Administrador"),Route("api/administracion/portadas")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PortadasAdministracionController(IPortadasService portadas, IUsuariosService usuarios) : ControllerBase
{
    [HttpGet] public Task<IReadOnlyList<PortadaGestion>> Listar(CancellationToken ct) => portadas.ListarGestionAsync(ct);
    [HttpPut("{pagina}")] public Task<PortadaGestion> Guardar(string pagina, GuardarPortada datos, CancellationToken ct) => portadas.GuardarAsync(pagina,datos,ct);
    [HttpPost("{pagina}/imagen"),Consumes("multipart/form-data")]
    [RequestSizeLimit(9*1024*1024),RequestFormLimits(MultipartBodyLengthLimit = 9*1024*1024)]
    public async Task<ActionResult<ImagenPortadaResultado>> Subir(string pagina,[FromForm] SubirImagenRequest datos,CancellationToken ct)
    {
        if (!PaginasPortada.Valida(pagina)) return NotFound();
        var usuario = usuarios.ObtenerActual(User);
        if (usuario is null || !AuthenticationHeaderValue.TryParse(Request.Headers.Authorization,out var header) ||
            !string.Equals(header.Scheme,"Bearer",StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(header.Parameter)) return Unauthorized();
        var storage = HttpContext.RequestServices.GetRequiredService<IImagenStorage>();
        await using var archivo = datos.Archivo.OpenReadStream();
        return Ok(await portadas.SubirAsync(pagina,usuario.Id,datos.IdOperacion,archivo,datos.Archivo.FileName,datos.Archivo.ContentType,header.Parameter,storage,ct));
    }
}
