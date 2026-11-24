using System.ComponentModel.DataAnnotations;

namespace PCP_Shooting.Domain.Services;

public sealed class ConciliacionProgramadaOptions
{
    public const string Section = "MercadoPago:ConciliacionProgramada";
    public bool Habilitada { get; set; }
    [Range(30, 3600)] public int IntervaloSegundos { get; set; } = 120;
    [Range(1, 100)] public int Lote { get; set; } = 10;
    [Range(1, 10)] public int PaginasPorOperacion { get; set; } = 2;
    [Range(5, 120)] public int TimeoutOperacionSegundos { get; set; } = 30;
    [Range(0, 60)] public int RetrasoMinutos { get; set; } = 2;
}
