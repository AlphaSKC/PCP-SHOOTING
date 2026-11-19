using System.ComponentModel.DataAnnotations;

namespace PCP_Shooting.Domain.Models;

public sealed class SubirImagenRequest
{
    public Guid IdOperacion { get; init; }
    [Required] public IFormFile Archivo { get; init; } = null!;
}

public sealed record ImagenValidada(byte[] Contenido, string Extension, string TipoContenido, string Sha256);
public sealed record ImagenProductoResultado(Guid Id, Guid ProductoId, string Url, string TipoContenido,
    long Tamano, bool EsImagenActual);
