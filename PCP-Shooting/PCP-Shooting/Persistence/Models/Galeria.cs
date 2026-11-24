namespace PCP_Shooting.Persistence.Models;

public sealed class AlbumGaleria
{
    public Guid Id { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }
    public int Orden { get; set; }
    public bool Publicado { get; set; }
    public Guid Version { get; set; }
    public DateTime CreadoEnUtc { get; set; }
    public DateTime ActualizadoEnUtc { get; set; }
    public ICollection<FotoGaleria> Fotos { get; } = new List<FotoGaleria>();
}

public sealed class FotoGaleria
{
    public Guid Id { get; set; }
    public Guid AlbumId { get; set; }
    public AlbumGaleria Album { get; set; } = null!;
    public required string Titulo { get; set; }
    public required string TextoAlternativo { get; set; }
    public string? Descripcion { get; set; }
    public int Orden { get; set; }
    public bool Publicada { get; set; }
    public string? ImagenUrl { get; set; }
    public Guid Version { get; set; }
    public DateTime CreadoEnUtc { get; set; }
    public DateTime ActualizadoEnUtc { get; set; }
}

public sealed class CargaFotoGaleria
{
    public Guid Id { get; set; }
    public Guid FotoId { get; set; }
    public FotoGaleria Foto { get; set; } = null!;
    public Guid ActorId { get; set; }
    public required string Ruta { get; set; }
    public required string Sha256 { get; set; }
    public required string TipoContenido { get; set; }
    public long Tamano { get; set; }
    public Guid VersionDestino { get; set; }
    public bool Disponible { get; set; }
    public DateTime CreadaEnUtc { get; set; }
    public DateTime? SubidaEnUtc { get; set; }
}
