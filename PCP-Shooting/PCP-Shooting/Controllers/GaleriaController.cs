using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Security.Claims;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Controllers;

[ApiController, AllowAnonymous, Route("api/galeria")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class GaleriaController(IGaleriaService galeria) : ControllerBase
{
    [HttpGet("albums")]
    public Task<PaginaConsulta<AlbumGaleriaPublico>> Albums([FromQuery] FiltroPagina filtro, CancellationToken ct) => galeria.AlbumsPublicosAsync(filtro, ct);
    [HttpGet("fotos")]
    public Task<PaginaConsulta<FotoGaleriaPublica>> Fotos([FromQuery] FiltroFotosGaleria filtro, CancellationToken ct) => galeria.FotosPublicasAsync(filtro, ct);
}

[ApiController, Authorize(Policy = "Administrador"), Route("api/administracion/galeria")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class GaleriaAdministracionController(IGaleriaService galeria) : ControllerBase
{
    [HttpPost("fotos/{id:guid}/imagen"), Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024), RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<ActionResult<ImagenContenidoResultado>> Imagen(Guid id, [FromForm] SubirImagenRequest datos,
        [FromServices] IGaleriaImagenesService imagenes, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue("sub"), out var actorId) ||
            !AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var header) ||
            !string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(header.Parameter)) return Unauthorized();
        await using var stream = datos.Archivo.OpenReadStream();
        return Ok(await imagenes.SubirAsync(actorId, id, datos.IdOperacion, stream, datos.Archivo.FileName, datos.Archivo.ContentType, header.Parameter, ct));
    }
    [HttpGet("albums")]
    public Task<PaginaConsulta<AlbumGaleriaGestion>> Albums([FromQuery] FiltroPagina filtro, CancellationToken ct) => galeria.AlbumsGestionAsync(filtro, ct);
    [HttpPost("albums")]
    public async Task<ActionResult<AlbumGaleriaGestion>> CrearAlbum(GuardarAlbumGaleria datos, CancellationToken ct)
        => Created("/api/administracion/galeria/albums", await galeria.GuardarAlbumAsync(null, datos, ct));
    [HttpPut("albums/{id:guid}")]
    public Task<AlbumGaleriaGestion> EditarAlbum(Guid id, GuardarAlbumGaleria datos, CancellationToken ct) => galeria.GuardarAlbumAsync(id, datos, ct);
    [HttpGet("albums/{id:guid}/fotos")]
    public Task<PaginaConsulta<FotoGaleriaGestion>> Fotos(Guid id, [FromQuery] FiltroPagina filtro, CancellationToken ct) => galeria.FotosGestionAsync(id, filtro, ct);
    [HttpPost("albums/{id:guid}/fotos")]
    public async Task<ActionResult<FotoGaleriaGestion>> CrearFoto(Guid id, GuardarFotoGaleria datos, CancellationToken ct)
        => Created($"/api/administracion/galeria/albums/{id}/fotos", await galeria.GuardarFotoAsync(null, id, datos, ct));
    [HttpPut("fotos/{id:guid}")]
    public Task<FotoGaleriaGestion> EditarFoto(Guid id, GuardarFotoGaleria datos, CancellationToken ct) => galeria.GuardarFotoAsync(id, null, datos, ct);
}
