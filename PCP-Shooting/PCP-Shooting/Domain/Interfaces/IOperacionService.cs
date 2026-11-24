using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Domain.Interfaces;

public interface IOperacionService
{
    Task<ConfiguracionOperativaConsulta> ConsultarConfiguracionAsync(CancellationToken ct);
    Task<ConfiguracionOperativaConsulta> GuardarConfiguracionAsync(GuardarConfiguracionOperativa datos, CancellationToken ct);
    Task<IReadOnlyList<LineaTiroConsulta>> ListarLineasAsync(CancellationToken ct);
    Task<LineaTiroConsulta> GuardarLineaAsync(Guid id, GuardarLineaTiro datos, CancellationToken ct);
    Task<PaginaConsulta<BloqueoOperacionConsulta>> ListarBloqueosAsync(FiltroBloqueosOperacion filtro, CancellationToken ct);
    Task<BloqueoOperacionConsulta?> ConsultarBloqueoAsync(Guid id, CancellationToken ct);
    Task<BloqueoOperacionConsulta> GuardarBloqueoAsync(Guid? id, Guid actorId, GuardarBloqueoOperacion datos, CancellationToken ct);
}
