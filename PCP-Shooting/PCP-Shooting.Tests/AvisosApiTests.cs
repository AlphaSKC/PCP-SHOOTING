using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class AvisosApiTests
{
    private const string Ruta = "/api/administracion/avisos";
    [Fact]
    public async Task AdministradorGuardaPlantillasYVersionAnteriorNoSobrescribe()
    {
        using var factory = new PerfilesFactory();
        using var admin = factory.CrearCliente("admin");
        var originalRespuesta = await admin.GetAsync(Ruta);
        Assert.Equal(HttpStatusCode.OK, originalRespuesta.StatusCode);
        var original = await originalRespuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, original.GetProperty("notifyEmail").ValueKind);
        Assert.Equal("", original.GetProperty("waOrderReady").GetString());
        var version = original.GetProperty("version").GetGuid();
        var datos = new { version, notifyEmail = " avisos@ejemplo.test ", waOrderReady = "Hola [nombre], pedido [número] listo.", waReminder = "[nombre]: [hora], [línea]." };
        var guardado = await admin.PutAsJsonAsync(Ruta, datos);
        Assert.Equal(HttpStatusCode.OK, guardado.StatusCode);
        var actualizado = await guardado.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("avisos@ejemplo.test", actualizado.GetProperty("notifyEmail").GetString());
        Assert.Equal(datos.waOrderReady, actualizado.GetProperty("waOrderReady").GetString());
        Assert.NotEqual(version, actualizado.GetProperty("version").GetGuid());
        using var otraSesion = factory.CrearCliente("admin");
        var leido = await otraSesion.GetFromJsonAsync<JsonElement>(Ruta);
        Assert.Equal(datos.waReminder, leido.GetProperty("waReminder").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await otraSesion.PutAsJsonAsync(Ruta, datos)).StatusCode);
        Assert.Equal(actualizado.GetProperty("version").GetGuid(), (await admin.GetFromJsonAsync<JsonElement>(Ruta)).GetProperty("version").GetGuid());
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("cliente", HttpStatusCode.Forbidden)]
    [InlineData("recepcion", HttpStatusCode.Forbidden)]
    [InlineData("inactivo", HttpStatusCode.Forbidden)]
    public async Task SoloAdministradorActivoPuedeLeerYEscribir(string? actor, HttpStatusCode estado)
    {
        using var factory = new PerfilesFactory(); using var cliente = factory.CrearCliente(actor);
        Assert.Equal(estado, (await cliente.GetAsync(Ruta)).StatusCode);
        Assert.Equal(estado, (await cliente.PutAsJsonAsync(Ruta, new { })).StatusCode);
    }

    [Fact]
    public async Task RechazaCamposInvalidosYPermiteDeshabilitarSinInventarValores()
    {
        using var factory = new PerfilesFactory(); using var admin = factory.CrearCliente("admin");
        var respuesta = await admin.GetAsync(Ruta);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var version = (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("version").GetGuid();
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync(Ruta, new { version, notifyEmail = "no-es-correo", waOrderReady = "", waReminder = "" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync(Ruta, new { version, notifyEmail = (string?)null, waOrderReady = new string('X',4001), waReminder = "" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync(Ruta, new { version, notifyEmail = (string?)null })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync(Ruta, new { version = Guid.Empty, notifyEmail = (string?)null, waOrderReady = "", waReminder = "" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync(Ruta, new { version, notifyEmail = (string?)null, waOrderReady = "", waReminder = "" })).StatusCode);
    }
}
