namespace PCP_Shooting.Persistence.Models;

public sealed class AvisoPublico
{
    public Guid Id { get; set; }
    public string? Titulo { get; set; }
    public string Texto { get; set; } = "";
    public bool Activo { get; set; }
    public int Orden { get; set; }
    public DateTime? InicioUtc { get; set; }
    public DateTime? FinUtc { get; set; }
    public Guid Version { get; set; }
    public DateTime CreadoEnUtc { get; set; }
    public DateTime ActualizadoEnUtc { get; set; }
}
