using System.ComponentModel.DataAnnotations;

namespace PCP_Shooting.Domain.Models;

public sealed class FiltroAlimentos : FiltroPagina, IValidatableObject
{
    [StringLength(150)] public string? Busqueda { get; init; }
    [Range(typeof(decimal), "0", "9999999999.99")] public decimal? PrecioMinimo { get; init; }
    [Range(typeof(decimal), "0", "9999999999.99")] public decimal? PrecioMaximo { get; init; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PrecioMinimo.HasValue && PrecioMaximo.HasValue && PrecioMinimo > PrecioMaximo)
            yield return new ValidationResult("El precio máximo debe ser mayor o igual al mínimo.", [nameof(PrecioMaximo)]);
    }
}

public sealed record AlimentoPublico(Guid Id, string Sku, string Nombre, string? Descripcion, decimal Precio,
    string? ImagenUrl, string? NotasMarkdown, long Existencia, bool Agotado);
