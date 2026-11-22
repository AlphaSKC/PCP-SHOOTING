using PCP_Shooting.Domain.Interfaces;

namespace PCP_Shooting.Persistence.Models;

public sealed class PerfilUsuario
{
    public Guid Id { get; set; }
    public required string Nombre { get; set; }
    public string? Telefono { get; set; }
    public RolUsuario Rol { get; set; } = RolUsuario.Cliente;
    public bool Activo { get; set; } = true;
    public DateTime CreadoEnUtc { get; set; } = DateTime.UtcNow;
}
