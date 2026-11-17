using System.Text.Json;

namespace PCP_Shooting.Domain.Interfaces;

public sealed record FichaProducto(Guid Id, string Sku, string Nombre, string? Descripcion, JsonElement? EspecificacionesTecnicas);

public interface IProductosService
{
    Task<FichaProducto?> ConsultarFichaAsync(string sku, CancellationToken cancellationToken = default);
}
