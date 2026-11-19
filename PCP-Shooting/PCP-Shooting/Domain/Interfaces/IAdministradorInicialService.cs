namespace PCP_Shooting.Domain.Interfaces;

public interface IAdministradorInicialService
{
    Task<Guid> InicializarAsync(string correo, CancellationToken ct);
}