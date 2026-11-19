using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Security;

namespace PCP_Shooting.Controllers;

[ApiController]
[Route("api/productos/{id:guid}/imagen")]
[Authorize(Policy = "OperacionPedidos")]
[ServiceFilter(typeof(AccesoAlimentosFilter))]
public sealed class ImagenesController(IImagenesService imagenes, IUsuariosService usuarios) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<ActionResult<ImagenProductoResultado>> Subir(
        Guid id, [FromForm] SubirImagenRequest datos, CancellationToken ct)
    {
        var usuario = usuarios.ObtenerActual(User);
        if (usuario is null || !AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var header) ||
            !string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(header.Parameter))
            return Unauthorized();
        await using var archivo = datos.Archivo.OpenReadStream();
        return Ok(await imagenes.SubirAsync(usuario.Id, id, datos.IdOperacion, archivo,
            datos.Archivo.FileName, datos.Archivo.ContentType, header.Parameter, ct));
    }
}