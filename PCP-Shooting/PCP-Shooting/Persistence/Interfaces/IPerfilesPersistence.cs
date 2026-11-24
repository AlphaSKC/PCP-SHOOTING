using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Persistence.Interfaces;

public interface IPerfilesPersistence
{
    Task<PerfilUsuario?> ConsultarAsync(Guid id, CancellationToken cancellationToken);
    Task<PaginaPerfiles> ListarAsync(FiltroPerfiles filtro, CancellationToken cancellationToken);
    Task GuardarPropioAsync(Guid id, string nombre, string? telefono, CancellationToken cancellationToken);
    Task<PerfilUsuario?> CambiarAccesoAsync(Guid id, CambiarAcceso datos, CancellationToken cancellationToken);
}