using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Domain.Interfaces;

public interface IConciliacionProgramadaService
{
    Task<CicloConciliacionResultado> EjecutarCicloAsync(CancellationToken ct);
}
