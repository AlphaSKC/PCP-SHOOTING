using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Domain.Services;
using PCP_Shooting.Persistence.Migrations;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class ContenidoSitioPostgresTests(PostgresEscriturasFixture fixture) : IClassFixture<PostgresEscriturasFixture>
{
    [PostgresFact]
    public async Task MigracionRlsVersionYPersistenciaNoCambianConfiguracionOperativa()
    {
        await using var db = fixture.CrearContext();
        Assert.StartsWith("pcp_test_", await db.Database.SqlQueryRaw<string>("SELECT current_schema() AS \"Value\"").SingleAsync());
        var versionOperativa = await db.ConfiguracionOperativa.AsNoTracking().Select(c => c.Version).SingleAsync();
        await db.Database.ExecuteSqlRawAsync("DROP TABLE \"ConfiguracionContenidoSitio\"");
        await db.Database.OpenConnectionAsync();
        foreach (var cmd in db.GetService<IMigrationsSqlGenerator>().Generate(new ContenidoInstitucional().UpOperations, db.Model))
        {
            await using var comando = db.Database.GetDbConnection().CreateCommand();
            comando.CommandText = cmd.CommandText;
            await comando.ExecuteNonQueryAsync();
        }
        Assert.True(await db.Database.SqlQueryRaw<bool>("""
            SELECT c.relrowsecurity AND NOT has_table_privilege('anon', c.oid, 'SELECT')
                AND NOT has_table_privilege('authenticated', c.oid, 'SELECT') AS "Value"
            FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname=current_schema() AND c.relname='ConfiguracionContenidoSitio'
            """).SingleAsync());
        var servicio = new ContenidoSitioService(db, TimeProvider.System);
        var inicial = await servicio.ConsultarAsync(default);
        Assert.Null(inicial.Contenido.Testimonio);
        inicial.Contenido.Direccion = "Dirección persistida de prueba";
        inicial.Contenido.HorarioPublico = "Solo texto informativo";
        inicial.Contenido.InstagramUrl = null;
        var guardado = await servicio.GuardarAsync(new() { Contenido = inicial.Contenido, Version = inicial.Version }, default);
        Assert.NotEqual(inicial.Version, guardado.Version);
        db.ChangeTracker.Clear();
        var leido = await servicio.ConsultarAsync(default);
        Assert.Equal("Dirección persistida de prueba", leido.Contenido.Direccion);
        Assert.Null(leido.Contenido.InstagramUrl);
        Assert.Equal(versionOperativa, await db.ConfiguracionOperativa.AsNoTracking().Select(c => c.Version).SingleAsync());
        Assert.Equal(409, (await Assert.ThrowsAsync<OperacionException>(() => servicio.GuardarAsync(new() { Contenido = inicial.Contenido, Version = inicial.Version }, default))).Status);
        Assert.Contains(await db.EventosAuditoria.AsNoTracking().ToListAsync(), a => a.Entidad == nameof(ConfiguracionContenidoSitio) && a.Accion == "Modified");
    }
}
