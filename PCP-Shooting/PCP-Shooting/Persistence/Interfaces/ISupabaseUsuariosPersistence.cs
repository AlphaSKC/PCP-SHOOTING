namespace PCP_Shooting.Persistence.Interfaces;

public interface ISupabaseUsuariosPersistence
{
    Task<Guid?> BuscarCuentaConfirmadaAsync(string correo, CancellationToken ct);
    Task<string?> ConsultarCorreoAsync(Guid id, CancellationToken ct);
}
