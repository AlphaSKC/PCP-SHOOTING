using System.ComponentModel.DataAnnotations;

namespace PCP_Shooting.Domain.Models;

public sealed class SubirImagenRequest
{
    public Guid IdOperacion { get; init; }
    public bool Agregar { get; init; }
    [Required] public IFormFile Archivo { get; init; } = null!;
}

public sealed record ImagenValidada(byte[] Contenido, string Extension, string TipoContenido, string Sha256);
public sealed record FotoProductoResultado(Guid Id, string Url, int Orden);
public sealed record OrdenarImagenesProducto([Required] IReadOnlyList<Guid> Ids);
public sealed record ImagenProductoResultado(Guid Id, Guid ProductoId, string Url, string TipoContenido,
    long Tamano, bool EsImagenActual);
