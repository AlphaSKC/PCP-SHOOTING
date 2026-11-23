using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Domain.Services;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class AvisosPostgresTests(PostgresEscriturasFixture fixture) : IClassFixture<PostgresEscriturasFixture>
{
    [PostgresFact]
    public async Task MigracionProtegeConfiguracionYGuardadoAuditaActorSinPerderPlantillas()
    {
        var actor = Guid.NewGuid();
        var acceso = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(
            new ClaimsIdentity(new[] { new Claim("sub", actor.ToString()) }, "Prueba")) } };
        await using var db = fixture.CrearContext(acceso);
        var esquema = await db.Database.SqlQueryRaw<string>("SELECT current_schema() AS \"Value\"").SingleAsync();
        Assert.StartsWith("pcp_test_", esquema);
        await db.Database.ExecuteSqlRawAsync("DROP TABLE \"ConfiguracionAvisos\"");
        var migracion = new PCP_Shooting.Persistence.Migrations.ConfiguracionAvisos();
        foreach (var comando in db.GetService<IMigrationsSqlGenerator>().Generate(migracion.UpOperations, db.Model))
            await db.Database.ExecuteSqlRawAsync(comando.CommandText);
        Assert.Equal(1, await db.Database.SqlQueryRaw<int>("""
            SELECT count(*)::int AS "Value" FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname=current_schema() AND c.relname='ConfiguracionAvisos' AND c.relrowsecurity
            AND NOT has_table_privilege('anon',c.oid,'SELECT') AND NOT has_table_privilege('authenticated',c.oid,'SELECT')
            """).SingleAsync());
        var servicio = new AvisosService(db, TimeProvider.System);
        var inicial = await servicio.ConsultarAsync(default);
        Assert.Null(inicial.NotifyEmail);
        var datos = new GuardarAvisos { Version = inicial.Version, NotifyEmail = "club@ejemplo.test",
            WaOrderReady = "Hola [nombre], pedido [número] listo.\nGracias.", WaReminder = "[hora] — [línea]" };
        var guardado = await servicio.GuardarAsync(datos, default);
        await using var otraSesion = fixture.CrearContext();
        Assert.Equal(guardado, await new AvisosService(otraSesion, TimeProvider.System).ConsultarAsync(default));
        Assert.Equal(409, (await Assert.ThrowsAsync<OperacionException>(() => servicio.GuardarAsync(datos, default))).Status);
        var evento = Assert.Single(await db.EventosAuditoria.Where(e => e.EntidadId == ConfiguracionAvisos.IdUnico).ToListAsync());
        Assert.Equal(actor, evento.ActorId);
        Assert.Equal("Modified", evento.Accion);
        var config = await db.ConfiguracionAvisos.SingleAsync();
        config.WaReminder = new string('X',4001);
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
