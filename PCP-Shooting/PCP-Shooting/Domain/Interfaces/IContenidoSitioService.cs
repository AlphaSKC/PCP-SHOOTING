using PCP_Shooting.Domain.Models;
namespace PCP_Shooting.Domain.Interfaces;
public interface IContenidoSitioService
{
    Task<ContenidoSitioConsulta> ConsultarAsync(CancellationToken ct);
    Task<ContenidoSitioConsulta> GuardarAsync(GuardarContenidoSitio datos, CancellationToken ct);
}
