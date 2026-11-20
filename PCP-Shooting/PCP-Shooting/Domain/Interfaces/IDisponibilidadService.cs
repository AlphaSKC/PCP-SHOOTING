using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Domain.Interfaces;

public interface IDisponibilidadService
{
    Task<DisponibilidadConsulta> ConsultarAsync(ConsultaDisponibilidad filtro, CancellationToken ct);
}
