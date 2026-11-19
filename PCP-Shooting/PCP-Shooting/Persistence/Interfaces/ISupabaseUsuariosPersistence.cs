namespace PCP_Shooting.Persistence.Interfaces;

public interface ISupabaseUsuariosPersistence
{
    Task<Guid?> BuscarCuentaConfirmadaAsync(string correo, CancellationToken ct);
}