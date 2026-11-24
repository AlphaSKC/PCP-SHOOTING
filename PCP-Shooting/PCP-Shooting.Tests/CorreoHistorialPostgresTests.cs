using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Persistence.Services;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class CorreoHistorialPostgresTests(PostgresEscriturasFixture fixture) : IClassFixture<PostgresEscriturasFixture>
{
    [PostgresFact]
    public async Task ConsultaCorreoActualPorIdSinModificarAuth()
    {
        await using var db = fixture.CrearContext();
        var servicio = new SupabaseUsuariosPersistence(db);
        Assert.Null(await servicio.ConsultarCorreoAsync(Guid.NewGuid(), default));
        var cuentas = await db.Database.SqlQuery<CuentaAuth>($"""
            SELECT id AS "Id", email AS "Correo" FROM auth.users
            WHERE deleted_at IS NULL AND email IS NOT NULL LIMIT 1
            """).ToListAsync();
        Assert.NotEmpty(cuentas);
        var cuenta = cuentas[0];
        var correo = await servicio.ConsultarCorreoAsync(cuenta.Id, default);
        Assert.True(string.Equals(cuenta.Correo, correo, StringComparison.Ordinal), "La consulta debe devolver el correo actual del ID solicitado.");
    }

    private sealed class CuentaAuth
    {
        public Guid Id { get; set; }
        public string Correo { get; set; } = "";
    }
}
