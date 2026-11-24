using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PCP_Shooting.Persistence.Migrations;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class EventosPostgresTests(PostgresEscriturasFixture fixture) : IClassFixture<PostgresEscriturasFixture>
{
    [PostgresFact]
    public async Task MigracionPublicacionFiltrosYAuditoriaEnPostgres()
    {
        await using (var db = fixture.CrearContext())
        {
            Assert.StartsWith("pcp_test_", await db.Database.SqlQueryRaw<string>("SELECT current_schema() AS \"Value\"").SingleAsync());
            await db.Database.ExecuteSqlRawAsync("DROP TABLE \"Eventos\"");
            foreach (var cmd in db.GetService<IMigrationsSqlGenerator>().Generate(new EventosInformativos().UpOperations, db.Model))
                await db.Database.ExecuteSqlRawAsync(cmd.CommandText);
            Assert.True(await db.Database.SqlQueryRaw<bool>("""
                SELECT c.relrowsecurity AND NOT has_table_privilege('anon',c.oid,'SELECT')
                    AND NOT has_table_privilege('authenticated',c.oid,'SELECT') AS "Value"
                FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
                WHERE n.nspname=current_schema() AND c.relname='Eventos'
                """).SingleAsync());
        }
        using var origen = new OperacionesFactory(fixture);
        using var factory = origen.WithWebHostBuilder(b => b.ConfigureServices(s => { s.RemoveAll<TimeProvider>(); s.AddSingleton<TimeProvider>(new RelojDisponibilidad(DateTimeOffset.Parse("2026-11-24T15:00:00Z"))); }));
        using var admin = factory.CreateClient(); admin.DefaultRequestHeaders.Add("X-Usuario-Prueba", OperacionesFactory.AdminId.ToString());
        using var publico = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/usuarios/yo")).StatusCode);
        var creado = await admin.PostAsJsonAsync("/api/administracion/eventos", EventosApiTests.Datos("encuentro-pg", "Publicado"));
        Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
        var evento = await creado.Content.ReadFromJsonAsync<JsonElement>(); var id = evento.GetProperty("id").GetGuid();
        Assert.Equal(1, (await publico.GetFromJsonAsync<JsonElement>("/api/eventos?desdeUtc=2027-01-10T15:00:00Z&hastaUtc=2027-01-11T00:00:00Z&busqueda=ENCUENTRO&proximos=true")).GetProperty("total").GetInt32());
        Assert.Equal(0, (await publico.GetFromJsonAsync<JsonElement>("/api/eventos?hastaUtc=2027-01-10T15:00:00Z")).GetProperty("total").GetInt32());
        Assert.Equal(1, (await admin.GetFromJsonAsync<JsonElement>("/api/administracion/eventos?estado=Publicado")).GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.OK, (await publico.GetAsync("/api/eventos/encuentro-pg")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/api/administracion/eventos", EventosApiTests.Datos("encuentro-pg"))).StatusCode);
        var cambio = EventosApiTests.Datos("encuentro-pg", "Archivado", evento.GetProperty("version").GetGuid());
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/administracion/eventos/{id}", cambio)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync($"/api/administracion/eventos/{id}", cambio)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await publico.GetAsync("/api/eventos/encuentro-pg")).StatusCode);
        await using var verificar = fixture.CrearContext();
        Assert.Equal(EstadoEvento.Archivado, (await verificar.Set<Evento>().SingleAsync(e => e.Id == id)).Estado);
        Assert.Contains(await verificar.EventosAuditoria.AsNoTracking().ToListAsync(), e => e.EntidadId == id && e.Accion == "Modified" && e.ActorId == OperacionesFactory.AdminId);
    }
}
