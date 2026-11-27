using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PCP_Shooting.Persistence;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class AvisosWhatsAppApiTests
{
    private const string Ruta = "/api/avisos/whatsapp";

    [Theory]
    [InlineData("admin")]
    [InlineData("recepcion")]
    public async Task PersonalActivoLeeSoloTextosVaciosSinEscribir(string actor)
    {
        using var app = new PerfilesFactory(); using var c = app.CrearCliente(actor);
        using var scope = app.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>();
        var auditoria = db.EventosAuditoria.Count();
        var respuesta = await c.GetAsync(Ruta); Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("no-store", respuesta.Headers.CacheControl!.ToString());
        var datos = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(new[] { "waOrderReady", "waReminder" }, datos.EnumerateObject().Select(p => p.Name).Order().ToArray());
        Assert.Equal("", datos.GetProperty("waOrderReady").GetString()); Assert.Equal("", datos.GetProperty("waReminder").GetString());
        Assert.Equal(auditoria, db.EventosAuditoria.Count());
    }

    [Fact]
    public async Task RecepcionLeePlantillasActualizadasSinCorreoVersionNiAccesoDeEscritura()
    {
        using var app = new PerfilesFactory(); using var admin = app.CrearCliente("admin"); using var recepcion = app.CrearCliente("recepcion");
        var config = await admin.GetFromJsonAsync<JsonElement>("/api/administracion/avisos");
        var datos = new { version = config.GetProperty("version").GetGuid(), notifyEmail = "privado@example.test", waOrderReady = "Hola [nombre], tu pedido [número] está listo.", waReminder = "Tu actividad es a las [hora], línea [línea]." };
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync("/api/administracion/avisos", datos)).StatusCode);
        var leido = await recepcion.GetFromJsonAsync<JsonElement>(Ruta);
        Assert.Equal(datos.waOrderReady, leido.GetProperty("waOrderReady").GetString()); Assert.Equal(datos.waReminder, leido.GetProperty("waReminder").GetString());
        Assert.Equal(2, leido.EnumerateObject().Count());
        Assert.Equal(HttpStatusCode.Forbidden, (await recepcion.GetAsync("/api/administracion/avisos")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await recepcion.PutAsJsonAsync("/api/administracion/avisos", datos)).StatusCode);
        var completo = await admin.GetFromJsonAsync<JsonElement>("/api/administracion/avisos");
        Assert.Equal(datos.notifyEmail, completo.GetProperty("notifyEmail").GetString());
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("cliente", HttpStatusCode.Forbidden)]
    [InlineData("inactivo", HttpStatusCode.Forbidden)]
    public async Task RechazaAccesoAjenoALaOperacion(string? actor, HttpStatusCode esperado)
    {
        using var app = new PerfilesFactory(); using var c = app.CrearCliente(actor);
        Assert.Equal(esperado, (await c.GetAsync(Ruta)).StatusCode);
    }
}
