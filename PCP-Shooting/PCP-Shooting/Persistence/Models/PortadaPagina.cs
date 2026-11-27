namespace PCP_Shooting.Persistence.Models;

public sealed class PortadaPagina
{
    public Guid Id { get; set; }
    public string Pagina { get; set; } = "";
    public bool Configurada { get; set; }
    public bool Activa { get; set; }
    public string? ImagenUrl { get; set; }
    public string Posicion { get; set; } = "50% 50%";
    public string Velo { get; set; } = "normal";
    public Guid Version { get; set; }
    public DateTime ActualizadoEnUtc { get; set; }
}
public sealed class CargaPortadaPagina
{
    public Guid Id { get; set; }
    public Guid PortadaId { get; set; }
    public Guid CreadorId { get; set; }
    public string Ruta { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public string TipoContenido { get; set; } = "";
    public long Tamano { get; set; }
    public Guid VersionDestino { get; set; }
    public bool Disponible { get; set; }
    public DateTime CreadaEnUtc { get; set; }
    public DateTime? SubidaEnUtc { get; set; }
}
