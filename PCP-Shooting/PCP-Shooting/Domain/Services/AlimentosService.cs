using System.ComponentModel.DataAnnotations;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Services;

public sealed class AlimentosService(IProductosPersistence productos) : IAlimentosService
{
    public async Task<PaginaConsulta<AlimentoPublico>> ListarAsync(FiltroAlimentos filtro, CancellationToken ct)
    {
        Validator.ValidateObject(filtro, new ValidationContext(filtro), true);
        var pagina = await productos.ListarCatalogoAsync(new FiltroProductos {
            Pagina = filtro.Pagina, TamanoPagina = filtro.TamanoPagina, Busqueda = filtro.Busqueda,
            PrecioMinimo = filtro.PrecioMinimo, PrecioMaximo = filtro.PrecioMaximo,
            Categoria = CategoriaProducto.Alimento, EsVendible = true
        }, ct);
        return new(pagina.Elementos.Select(Proyectar).ToArray(), pagina.Total, pagina.Pagina, pagina.TamanoPagina);
    }

    public async Task<AlimentoPublico?> ConsultarAsync(string sku, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        var producto = await productos.ConsultarAlimentoAsync(sku.Trim(), ct);
        return producto is null ? null : Proyectar(producto);
    }

    private static AlimentoPublico Proyectar(ProductoCatalogo p) => new(p.Id, p.Sku, p.Nombre, p.Descripcion,
        p.Precio ?? throw new InvalidOperationException("Un alimento vendible debe tener precio."),
        p.ImagenUrl, p.NotasMarkdown, p.Existencia ?? 0, p.Agotado ?? true);
}
