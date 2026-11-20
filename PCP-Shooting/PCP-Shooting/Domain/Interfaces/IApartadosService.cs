using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Domain.Interfaces;

public interface IApartadosService
{
    Task<ApartadoConsulta> CrearAsync(Guid usuarioId, CrearApartado datos, CancellationToken ct);
    Task<ApartadoConsulta?> ConsultarAsync(Guid usuarioId, Guid id, CancellationToken ct);
}
