using PCP_Shooting.Domain.Models;
namespace PCP_Shooting.Domain.Interfaces;
public interface IReservasWebService
{
    Task<ReservaWebConsulta> CrearAsync(Guid usuarioId, CrearReservaWeb datos, CancellationToken ct);
    Task<ReservaWebConsulta?> ConsultarAsync(Guid usuarioId, Guid id, CancellationToken ct);
    Task<PaginaConsulta<ReservaWebResumen>> ListarAsync(Guid usuarioId, FiltroPagina filtro, CancellationToken ct);
}
