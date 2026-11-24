using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Domain.Interfaces;

public interface IAlimentosService
{
    Task<PaginaConsulta<AlimentoPublico>> ListarAsync(FiltroAlimentos filtro, CancellationToken ct);
    Task<AlimentoPublico?> ConsultarAsync(string sku, CancellationToken ct);
}
