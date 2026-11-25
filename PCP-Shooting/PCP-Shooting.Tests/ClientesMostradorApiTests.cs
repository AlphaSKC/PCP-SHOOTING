using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class ClientesMostradorApiTests
{
    private const string Ruta = "/api/reservas/mostrador/clientes";
    internal static async Task Sembrar(PcpShootingDbContext db)
    {
        db.PerfilesUsuario.AddRange(
            new PerfilUsuario { Id = Guid.NewGuid(), Nombre = "A Cliente Mostrador", Telefono = "9981112233" },
            new PerfilUsuario { Id = Guid.NewGuid(), Nombre = "B Cliente Mostrador", Telefono = null },
            new PerfilUsuario { Id = Guid.NewGuid(), Nombre = "C Cliente Mostrador", Telefono = "9981112233", Activo = false },
            new PerfilUsuario { Id = Guid.NewGuid(), Nombre = "D Cliente Mostrador", Telefono = "9981112233", Rol = RolUsuario.Administrador },
            new PerfilUsuario { Id = Guid.NewGuid(), Nombre = "E Cliente Mostrador", Telefono = "9981112233", Rol = RolUsuario.Recepcion });
        await db.SaveChangesAsync();
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("recepcion")]
    public async Task PersonalBuscaClientesActivosPorNombreYTelefonoSinExponerDatosAdicionales(string actor)
    {
        using var app = new PerfilesFactory();
        using (var scope = app.Services.CreateScope()) await Sembrar(scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>());
        using var c = app.CrearCliente(actor);
        var pagina = await c.GetFromJsonAsync<JsonElement>(Ruta + "?busqueda=CLIENTE%20MOSTRADOR&tamanoPagina=1");
        Assert.Equal(2, pagina.GetProperty("total").GetInt32());
        var cliente = Assert.Single(pagina.GetProperty("elementos").EnumerateArray());
        Assert.Equal("A Cliente Mostrador", cliente.GetProperty("nombre").GetString());
        Assert.Equal("9981112233", cliente.GetProperty("telefono").GetString());
        Assert.Equal(new[] { "id", "nombre", "telefono" }, cliente.EnumerateObject().Select(p => p.Name).Order().ToArray());
        var telefono = await c.GetFromJsonAsync<JsonElement>(Ruta + "?busqueda=11122");
        Assert.Equal(1, telefono.GetProperty("total").GetInt32());
        var segunda = await c.GetFromJsonAsync<JsonElement>(Ruta + "?busqueda=%20cliente%20mostrador%20&tamanoPagina=1&pagina=2");
        Assert.Equal(JsonValueKind.Null, Assert.Single(segunda.GetProperty("elementos").EnumerateArray()).GetProperty("telefono").ValueKind);
        Assert.Equal(0, (await c.GetFromJsonAsync<JsonElement>(Ruta + "?busqueda=sin-coincidencias")).GetProperty("total").GetInt32());
        Assert.Equal(0, (await c.GetFromJsonAsync<JsonElement>(Ruta + "?busqueda=mostrador&pagina=2147483647")).GetProperty("elementos").GetArrayLength());
        if (actor == "recepcion") Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/api/perfiles")).StatusCode);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("cliente", HttpStatusCode.Forbidden)]
    [InlineData("inactivo", HttpStatusCode.Forbidden)]
    public async Task BusquedaPrivadaRechazaOtrosActores(string? actor, HttpStatusCode esperado)
    {
        using var app = new PerfilesFactory(); using var c = app.CrearCliente(actor);
        Assert.Equal(esperado, (await c.GetAsync(Ruta + "?busqueda=cliente")).StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("?busqueda=a")]
    [InlineData("?busqueda=%20%20")]
    [InlineData("?busqueda=%20a%20")]
    [InlineData("?busqueda=cliente&pagina=0")]
    [InlineData("?busqueda=cliente&tamanoPagina=101")]
    public async Task RequiereBusquedaAcotadaYPaginacionValida(string filtro)
    {
        using var app = new PerfilesFactory(); using var c = app.CrearCliente("recepcion");
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync(Ruta + filtro)).StatusCode);
    }
}
