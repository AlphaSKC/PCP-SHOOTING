using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Domain.Interfaces;

public interface IAvisosService
{
    Task<AvisosConsulta> ConsultarAsync(CancellationToken ct);
    Task<PlantillasWhatsAppConsulta> ConsultarWhatsAppAsync(CancellationToken ct);
    Task<AvisosConsulta> GuardarAsync(GuardarAvisos datos, CancellationToken ct);
}
