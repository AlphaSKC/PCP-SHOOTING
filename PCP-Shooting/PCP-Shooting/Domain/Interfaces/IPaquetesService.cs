using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Domain.Interfaces;

public interface IPaquetesService
{
    Task<PaginaConsulta<PaquetePublico>> ListarPublicosAsync(FiltroPaquetes filtro, CancellationToken ct);
    Task<PaquetePublico?> ConsultarPublicoAsync(Guid id, CancellationToken ct);
    Task<PaginaConsulta<PaqueteGestion>> ListarGestionAsync(FiltroPaquetesGestion filtro, CancellationToken ct);
    Task<PaqueteGestion?> ConsultarGestionAsync(Guid id, CancellationToken ct);
    Task<PaqueteGestion> GuardarAsync(Guid? id, GuardarPaquete datos, CancellationToken ct);
}
