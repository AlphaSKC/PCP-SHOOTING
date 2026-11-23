using PCP_Shooting.Persistence.Models;
using PCP_Shooting.Domain.Models;
using System.Text.Json;

namespace PCP_Shooting.Domain.Interfaces;

public sealed record FichaProducto(Guid Id, string Sku, string Nombre, string? Descripcion, JsonElement? EspecificacionesTecnicas,
    CategoriaProducto Categoria = CategoriaProducto.SinClasificar,
    string? ImagenUrl = null, string? NotasMarkdown = null, bool UsaTallas = false, IReadOnlyList<ExistenciaTalla>? Tallas = null);

public interface IProductosService
{
    Task<FichaProducto?> ConsultarFichaAsync(string sku, CancellationToken cancellationToken = default);
    Task<PaginaConsulta<ProductoCatalogo>> ListarCatalogoAsync(FiltroProductos filtro, CancellationToken ct);
    Task<PaginaConsulta<ProductoGestion>> ListarGestionAsync(FiltroProductosAdministrativo filtro, CancellationToken ct);
}
