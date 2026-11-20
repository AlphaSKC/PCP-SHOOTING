using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Domain.Interfaces;

public interface IApartadosService
{
    Task<HistorialApartadosConsulta> ListarAsync(Guid usuarioId, FiltroApartados filtro, CancellationToken ct);
    Task<ApartadoConsulta> CrearAsync(Guid usuarioId, CrearApartado datos, CancellationToken ct);
    Task<ApartadoConsulta?> ConsultarAsync(Guid usuarioId, Guid id, CancellationToken ct);
}
