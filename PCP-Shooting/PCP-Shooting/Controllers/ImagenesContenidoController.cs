using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Controllers;

[ApiController, Authorize(Policy = "Administrador"), Route("api/administracion")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ImagenesContenidoController(IImagenesContenidoService imagenes, IUsuariosService usuarios) : ControllerBase
{
    [HttpPost("tienda/categorias/{id:guid}/imagen"), Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024), RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<ActionResult<ImagenCategoriaTiendaResultado>> Categoria(Guid id, [FromForm] SubirImagenRequest datos, CancellationToken ct)
    {
        var usuario = usuarios.ObtenerActual(User);
        if (usuario is null || !AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var header) ||
            !string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(header.Parameter))
            throw new OperacionException(401, "Se requiere la sesión Bearer para cargar la imagen.");
        await using var archivo = datos.Archivo.OpenReadStream();
        var resultado = await imagenes.SubirCategoriaAsync(usuario.Id, id, datos.IdOperacion, archivo, datos.Archivo.FileName, datos.Archivo.ContentType, header.Parameter, ct);
        return Ok(new ImagenCategoriaTiendaResultado(resultado.Url, resultado.Version));
    }
    [HttpPost("marcas/{id:guid}/logo"), Consumes("multipart/form-data")]
    [RequestSizeLimit(3 * 1024 * 1024), RequestFormLimits(MultipartBodyLengthLimit = 3 * 1024 * 1024)]
    public async Task<ActionResult<LogoMarcaResultado>> Logo(Guid id, [FromForm] SubirImagenRequest datos, CancellationToken ct)
    {
        var resultado = await Subir(id, true, datos, ct);
        return Ok(new LogoMarcaResultado(resultado.Url, resultado.Version));
    }
    [HttpPost("blog/{id:guid}/portada"), Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024), RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<ActionResult<PortadaBlogResultado>> Portada(Guid id, [FromForm] SubirImagenRequest datos, CancellationToken ct)
    {
        var resultado = await Subir(id, false, datos, ct);
        return Ok(new PortadaBlogResultado(resultado.Url, resultado.Version));
    }
    private async Task<ImagenContenidoResultado> Subir(Guid id, bool marca, SubirImagenRequest datos, CancellationToken ct)
    {
        var usuario = usuarios.ObtenerActual(User);
        if (usuario is null || !AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var header) ||
            !string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(header.Parameter))
            throw new OperacionException(401, "Se requiere la sesión Bearer para cargar la imagen.");
        await using var archivo = datos.Archivo.OpenReadStream();
        return await imagenes.SubirAsync(usuario.Id, id, marca, datos.IdOperacion, archivo, datos.Archivo.FileName, datos.Archivo.ContentType, header.Parameter, ct);
    }
}
