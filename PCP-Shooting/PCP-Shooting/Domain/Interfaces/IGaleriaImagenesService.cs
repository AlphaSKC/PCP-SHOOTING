using PCP_Shooting.Domain.Models;
namespace PCP_Shooting.Domain.Interfaces;
public interface IGaleriaImagenesService
{
    Task<ImagenContenidoResultado> SubirAsync(Guid actorId, Guid fotoId, Guid idOperacion, Stream archivo, string nombre, string tipoContenido, string token, CancellationToken ct);
}
