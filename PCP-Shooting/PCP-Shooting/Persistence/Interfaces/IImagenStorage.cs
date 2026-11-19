using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Persistence.Interfaces;

public interface IImagenStorage
{
    string ObtenerUrlPublica(string ruta);
    Task SubirAsync(string ruta, ImagenValidada imagen, string token, CancellationToken ct);
}