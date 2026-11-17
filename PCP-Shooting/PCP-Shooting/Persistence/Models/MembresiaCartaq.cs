namespace PCP_Shooting.Persistence.Models;

public sealed class MembresiaCartaq
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public Guid PagoId { get; set; }
    public Pago Pago { get; set; } = null!;
    public DateOnly InicioVigencia { get; set; }
    public DateOnly FinVigenciaExclusivo { get; set; }
}
