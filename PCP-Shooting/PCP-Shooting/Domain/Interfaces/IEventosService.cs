using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Domain.Interfaces;

public interface IEventosService
{
    Task<PaginaConsulta<EventoResumen>> ListarPublicosAsync(FiltroEventos filtro, CancellationToken ct);
    Task<EventoDetalle?> ConsultarPublicoAsync(string slug, CancellationToken ct);
    Task<PaginaConsulta<EventoGestion>> ListarGestionAsync(FiltroEventosGestion filtro, CancellationToken ct);
    Task<EventoGestion?> ConsultarGestionAsync(Guid id, CancellationToken ct);
    Task<EventoGestion> GuardarAsync(Guid? id, GuardarEvento datos, CancellationToken ct);
}
