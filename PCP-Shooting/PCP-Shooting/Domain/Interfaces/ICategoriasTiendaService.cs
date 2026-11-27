using PCP_Shooting.Domain.Models;
namespace PCP_Shooting.Domain.Interfaces;

public interface ICategoriasTiendaService
{
    Task<IReadOnlyList<CategoriaTiendaPublica>> ListarPublicasAsync(CancellationToken ct);
    Task<IReadOnlyList<CategoriaTiendaGestion>> ListarGestionAsync(CancellationToken ct);
    Task<CategoriaTiendaGestion> GuardarAsync(Guid? id, GuardarCategoriaTienda datos, CancellationToken ct);
    Task EliminarAsync(Guid id, CancellationToken ct);
}
