namespace PCP_Shooting.Persistence.Models;

public sealed class MarcaAliada
{
    public Guid Id { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }
    public string? Url { get; set; }
    public string? LogoUrl { get; set; }
    public bool Activo { get; set; }
    public int Orden { get; set; }
    public Guid Version { get; set; }
    public DateTime CreadoEnUtc { get; set; }
    public DateTime ActualizadoEnUtc { get; set; }
}

public sealed class CargaImagenContenido
{
    public Guid Id { get; set; }
    public Guid? MarcaId { get; set; }
    public Guid? ArticuloBlogId { get; set; }
    public Guid CreadorId { get; set; }
    public required string Ruta { get; set; }
    public required string Sha256 { get; set; }
    public required string TipoContenido { get; set; }
    public long Tamano { get; set; }
    public Guid VersionDestino { get; set; }
    public bool Disponible { get; set; }
    public DateTime CreadaEnUtc { get; set; }
    public DateTime? SubidaEnUtc { get; set; }
}
