using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Domain.Interfaces;

public interface IImagenesService
{
    Task<ImagenProductoResultado> SubirAsync(Guid actorId, Guid productoId, Guid idOperacion,
        Stream archivo, string nombre, string tipoContenido, string token, CancellationToken ct, bool agregar = false);
    Task<IReadOnlyList<FotoProductoResultado>> OrdenarAsync(Guid actorId, Guid productoId, OrdenarImagenesProducto datos, CancellationToken ct);
    Task QuitarAsync(Guid actorId, Guid productoId, Guid imagenId, CancellationToken ct);
}
