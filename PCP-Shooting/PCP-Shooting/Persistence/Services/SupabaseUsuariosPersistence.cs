using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Persistence.Interfaces;

namespace PCP_Shooting.Persistence.Services;

public sealed class SupabaseUsuariosPersistence(PcpShootingDbContext db) : ISupabaseUsuariosPersistence
{
    public Task<string?> ConsultarCorreoAsync(Guid id, CancellationToken ct) => db.Database.SqlQuery<string>($"""
        SELECT email AS "Value" FROM auth.users
        WHERE id = {id} AND deleted_at IS NULL AND email IS NOT NULL
        """).SingleOrDefaultAsync(ct);

    public async Task<Guid?> BuscarCuentaConfirmadaAsync(string correo, CancellationToken ct)
    {
        var id = await db.Database.SqlQuery<Guid>($"""
            SELECT id AS "Value" FROM auth.users
            WHERE lower(email) = lower({correo})
                AND email_confirmed_at IS NOT NULL
                AND deleted_at IS NULL
                AND (banned_until IS NULL OR banned_until <= now())
            """).SingleOrDefaultAsync(ct);
        return id == Guid.Empty ? null : id;
    }
}
