using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence.Interfaces;
namespace PCP_Shooting.Domain.Interfaces;

public interface IPortadasService
{
    Task<IReadOnlyList<PortadaPublica>> ListarPublicasAsync(CancellationToken ct);
    Task<IReadOnlyList<PortadaGestion>> ListarGestionAsync(CancellationToken ct);
    Task<PortadaGestion> GuardarAsync(string pagina, GuardarPortada datos, CancellationToken ct);
    Task<ImagenPortadaResultado> SubirAsync(string pagina, Guid actorId, Guid idOperacion, Stream archivo,
        string nombre, string tipoContenido, string token, IImagenStorage storage, CancellationToken ct);
}
