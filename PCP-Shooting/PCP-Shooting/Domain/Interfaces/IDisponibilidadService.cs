using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Domain.Interfaces;

public interface IDisponibilidadService
{
    Task<VentanaDisponibilidadConsulta> ConsultarVentanaAsync(CancellationToken ct);
    Task<DisponibilidadConsulta> ConsultarAsync(ConsultaDisponibilidad filtro, CancellationToken ct);
}
