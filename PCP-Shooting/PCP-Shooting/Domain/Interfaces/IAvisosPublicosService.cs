using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Domain.Interfaces;

public interface IAvisosPublicosService
{
    Task<PaginaConsulta<AvisoPublicoDto>> ListarPublicosAsync(FiltroPagina filtro, CancellationToken ct);
    Task<PaginaConsulta<AvisoPublicoGestion>> ListarGestionAsync(FiltroAvisosPublicos filtro, CancellationToken ct);
    Task<AvisoPublicoGestion> ObtenerAsync(Guid id, CancellationToken ct);
    Task<AvisoPublicoGestion> GuardarAsync(Guid? id, GuardarAvisoPublico datos, CancellationToken ct);
}
