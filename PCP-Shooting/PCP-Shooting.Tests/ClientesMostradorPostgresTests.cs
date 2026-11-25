using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class ClientesMostradorPostgresTests(PostgresEscriturasFixture fixture) : IClassFixture<PostgresEscriturasFixture>
{
    [PostgresFact]
    public async Task BusquedaSqlRestringeClientesActivosYRespetaPermisosYPaginacion()
    {
        var recepcionId = Guid.NewGuid();
        await using (var db = fixture.CrearContext())
        {
            await ClientesMostradorApiTests.Sembrar(db);
            db.PerfilesUsuario.Add(new PerfilUsuario { Id = recepcionId, Nombre = "Recepción HTTP", Rol = RolUsuario.Recepcion });
            await db.SaveChangesAsync();
        }
        using var app = new OperacionesFactory(fixture); using var recepcion = app.Cliente(recepcionId); using var admin = app.Cliente(OperacionesFactory.AdminId);
        const string ruta = "/api/reservas/mostrador/clientes";
        foreach (var cliente in new[] { recepcion, admin })
        {
            var pagina = await cliente.GetFromJsonAsync<JsonElement>(ruta + "?busqueda=CLIENTE%20MOSTRADOR&tamanoPagina=1");
            Assert.Equal(2, pagina.GetProperty("total").GetInt32());
            Assert.Equal("A Cliente Mostrador", Assert.Single(pagina.GetProperty("elementos").EnumerateArray()).GetProperty("nombre").GetString());
            var telefono = await cliente.GetFromJsonAsync<JsonElement>(ruta + "?busqueda=11122");
            Assert.Equal(1, telefono.GetProperty("total").GetInt32());
            Assert.Equal("9981112233", Assert.Single(telefono.GetProperty("elementos").EnumerateArray()).GetProperty("telefono").GetString());
            var segunda = await cliente.GetFromJsonAsync<JsonElement>(ruta + "?busqueda=mostrador&tamanoPagina=1&pagina=2");
            Assert.Equal(JsonValueKind.Null, Assert.Single(segunda.GetProperty("elementos").EnumerateArray()).GetProperty("telefono").ValueKind);
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await recepcion.GetAsync("/api/perfiles")).StatusCode);
        using var publico = app.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await publico.GetAsync(ruta + "?busqueda=mostrador")).StatusCode);
        await using (var db = fixture.CrearContext())
        {
            var cliente = await db.PerfilesUsuario.SingleAsync(p => p.Nombre == "A Cliente Mostrador");
            cliente.Activo = false; await db.SaveChangesAsync();
        }
        Assert.Equal(0, (await recepcion.GetFromJsonAsync<JsonElement>(ruta + "?busqueda=11122")).GetProperty("total").GetInt32());
    }
}
