using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PCP_Shooting.Persistence.Migrations;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class AvisosPublicosPostgresTests(PostgresEscriturasFixture fixture) : IClassFixture<PostgresEscriturasFixture>
{
    [PostgresFact]
    public async Task MigracionYFiltrosPersistenConAuditoriaYAccesoDirectoBloqueado()
    {
        await using (var db = fixture.CrearContext())
        {
            Assert.StartsWith("pcp_test_", await db.Database.SqlQueryRaw<string>("SELECT current_schema() AS \"Value\"").SingleAsync());
            await db.Database.ExecuteSqlRawAsync("DROP TABLE \"AvisosPublicos\"");
            foreach (var cmd in db.GetService<IMigrationsSqlGenerator>().Generate(new AvisosPublicos().UpOperations, db.Model))
                await db.Database.ExecuteSqlRawAsync(cmd.CommandText);
            Assert.True(await db.Database.SqlQueryRaw<bool>("""
                SELECT c.relrowsecurity AND NOT has_table_privilege('anon',c.oid,'SELECT')
                    AND NOT has_table_privilege('authenticated',c.oid,'SELECT') AS "Value"
                FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
                WHERE n.nspname=current_schema() AND c.relname='AvisosPublicos'
                """).SingleAsync());
        }
        using var factory = new OperacionesFactory(fixture); using var admin = factory.Cliente(OperacionesFactory.AdminId); using var publico = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/usuarios/yo")).StatusCode);
        var datos = new { titulo = "Aviso PG", texto = "Horario informativo", activo = true, orden = 2, inicioUtc = (DateTimeOffset?)null, finUtc = (DateTimeOffset?)null };
        var creado = await admin.PostAsJsonAsync("/api/administracion/avisos-publicos", datos);
        Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync(creado.Headers.Location)).StatusCode);
        var aviso = await creado.Content.ReadFromJsonAsync<JsonElement>(); var id = aviso.GetProperty("id").GetGuid();
        Assert.Equal(1, (await publico.GetFromJsonAsync<JsonElement>("/api/avisos-publicos")).GetProperty("total").GetInt32());
        Assert.Equal(1, (await admin.GetFromJsonAsync<JsonElement>("/api/administracion/avisos-publicos?estado=Vigente&busqueda=informativo")).GetProperty("total").GetInt32());
        Assert.Equal(0, (await admin.GetFromJsonAsync<JsonElement>("/api/administracion/avisos-publicos?estado=Programado")).GetProperty("total").GetInt32());
        var cambio = new { datos.titulo, datos.texto, activo = false, datos.orden, datos.inicioUtc, datos.finUtc, version = aviso.GetProperty("version").GetGuid() };
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/administracion/avisos-publicos/{id}", cambio)).StatusCode);
        Assert.Equal(0, (await publico.GetFromJsonAsync<JsonElement>("/api/avisos-publicos")).GetProperty("total").GetInt32());
        await using var verificar = fixture.CrearContext();
        Assert.False((await verificar.Set<AvisoPublico>().SingleAsync(a => a.Id == id)).Activo);
        Assert.Contains(await verificar.EventosAuditoria.AsNoTracking().ToListAsync(), e => e.EntidadId == id && e.Accion == "Modified" && e.ActorId == OperacionesFactory.AdminId);
    }
}
