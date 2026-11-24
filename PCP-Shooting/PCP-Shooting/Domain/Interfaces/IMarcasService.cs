using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Domain.Interfaces;

public interface IMarcasService
{
    Task<IReadOnlyList<MarcaPublica>> ListarPublicasAsync(CancellationToken ct);
    Task<IReadOnlyList<MarcaGestion>> ListarGestionAsync(CancellationToken ct);
    Task<MarcaGestion> GuardarAsync(Guid? id, GuardarMarca datos, CancellationToken ct);
    Task QuitarLogoAsync(Guid id, CancellationToken ct);
}
public interface IImagenesContenidoService
{
    Task<ImagenContenidoResultado> SubirAsync(Guid actorId, Guid destinoId, bool marca, Guid idOperacion,
        Stream archivo, string nombre, string tipoContenido, string token, CancellationToken ct);
}
