using PCP_Shooting.Domain.Models;
namespace PCP_Shooting.Domain.Interfaces;

public interface IGaleriaService
{
    Task<PaginaConsulta<AlbumGaleriaPublico>> AlbumsPublicosAsync(FiltroPagina filtro, CancellationToken ct);
    Task<PaginaConsulta<FotoGaleriaPublica>> FotosPublicasAsync(FiltroFotosGaleria filtro, CancellationToken ct);
    Task<PaginaConsulta<AlbumGaleriaGestion>> AlbumsGestionAsync(FiltroPagina filtro, CancellationToken ct);
    Task<PaginaConsulta<FotoGaleriaGestion>> FotosGestionAsync(Guid albumId, FiltroPagina filtro, CancellationToken ct);
    Task<AlbumGaleriaGestion> GuardarAlbumAsync(Guid? id, GuardarAlbumGaleria datos, CancellationToken ct);
    Task<FotoGaleriaGestion> GuardarFotoAsync(Guid? id, Guid? albumId, GuardarFotoGaleria datos, CancellationToken ct);
}
