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

public sealed class ContactoPostgresTests(PostgresEscriturasFixture fixture) : IClassFixture<PostgresEscriturasFixture>
{
    [PostgresFact]
    public async Task MigracionProtegeDatosYFlujoHttpPersisteConAuditoriaSinDuplicar()
    {
        await using (var db = fixture.CrearContext())
        {
            Assert.StartsWith("pcp_test_", await db.Database.SqlQueryRaw<string>("SELECT current_schema() AS \"Value\"").SingleAsync());
            await db.Database.ExecuteSqlRawAsync("DROP TABLE \"SolicitudesContacto\"");
            foreach (var cmd in db.GetService<IMigrationsSqlGenerator>().Generate(new SolicitudesContacto().UpOperations, db.Model))
                await db.Database.ExecuteSqlRawAsync(cmd.CommandText);
            Assert.True(await db.Database.SqlQueryRaw<bool>("""
                SELECT c.relrowsecurity AND NOT has_table_privilege('anon',c.oid,'SELECT')
                    AND NOT has_table_privilege('authenticated',c.oid,'SELECT') AS "Value"
                FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
                WHERE n.nspname=current_schema() AND c.relname='SolicitudesContacto'
                """).SingleAsync());
        }
        using var factory = new OperacionesFactory(fixture); using var admin = factory.Cliente(OperacionesFactory.AdminId); using var publico = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/usuarios/yo")).StatusCode);
        var id = Guid.NewGuid();
        var datos = new { idOperacion = id, nombre = "Visitante PG", email = "visitante@example.invalid", motivo = "grupos", mensaje = "Solicito información de grupos.", privacidad = true };
        Assert.Equal(HttpStatusCode.Created, (await publico.PostAsJsonAsync("/api/contacto", datos)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await publico.PostAsJsonAsync("/api/contacto", datos)).StatusCode);
        var detalle = await admin.GetFromJsonAsync<JsonElement>($"/api/administracion/contacto/{id}");
        var creado = detalle.GetProperty("creadoEnUtc").GetDateTime();
        var query = "/api/administracion/contacto?estado=Pendiente&motivo=grupos&busqueda=visitante&desdeUtc=" + Uri.EscapeDataString(creado.ToString("O"));
        Assert.Equal(1, (await admin.GetFromJsonAsync<JsonElement>(query)).GetProperty("total").GetInt32());
        Assert.Equal(0, (await admin.GetFromJsonAsync<JsonElement>("/api/administracion/contacto?hastaUtc=" + Uri.EscapeDataString(creado.ToString("O")))).GetProperty("total").GetInt32());
        var cambio = new { estado = "Archivada", notasInternas = "Información interna de seguimiento", version = detalle.GetProperty("version").GetGuid() };
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/administracion/contacto/{id}", cambio)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync($"/api/administracion/contacto/{id}", cambio)).StatusCode);
        await using var verificar = fixture.CrearContext();
        var guardada = Assert.Single(await verificar.Set<SolicitudContacto>().AsNoTracking().ToListAsync());
        Assert.Equal(EstadoSolicitudContacto.Archivada, guardada.Estado);
        Assert.Contains(await verificar.EventosAuditoria.AsNoTracking().ToListAsync(), e => e.EntidadId == id && e.Accion == "Modified" && e.ActorId == OperacionesFactory.AdminId);
    }
}
