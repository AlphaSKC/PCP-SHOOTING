using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Domain.Interfaces;

public interface IBlogService
{
    Task<PaginaConsulta<ArticuloBlogResumen>> ListarPublicosAsync(FiltroBlog filtro, CancellationToken ct);
    Task<ArticuloBlogDetalle?> ConsultarPublicoAsync(string slug, CancellationToken ct);
    Task<PaginaConsulta<ArticuloBlogGestion>> ListarGestionAsync(FiltroBlogGestion filtro, CancellationToken ct);
    Task<ArticuloBlogGestion?> ConsultarGestionAsync(Guid id, CancellationToken ct);
    Task<ArticuloBlogGestion> GuardarAsync(Guid? id, Guid actorId, GuardarArticuloBlog datos, CancellationToken ct);
}
