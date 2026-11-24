using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class ContenidoSitioApiTests
{
    [Fact]
    public async Task AdminActualizaContenidoPublicoConVersionSinTocarHorariosOperativos()
    {
        using var app = new PerfilesFactory(); using var admin = app.CrearCliente("admin"); using var publico = app.CrearCliente(null);
        var actual = await admin.GetFromJsonAsync<JsonObject>("/api/administracion/contenido-sitio");
        Assert.NotNull(actual);
        actual["contenido"]!["nosotrosTexto"] = "Texto institucional actualizado";
        actual["contenido"]!["horarioPublico"] = "Horario informativo de prueba";
        var guardado = await admin.PutAsJsonAsync("/api/administracion/contenido-sitio", actual);
        Assert.Equal(HttpStatusCode.OK, guardado.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync("/api/administracion/contenido-sitio", actual)).StatusCode);
        var datos = await publico.GetFromJsonAsync<JsonObject>("/api/contenido-sitio");
        Assert.Equal("Texto institucional actualizado", datos!["nosotrosTexto"]!.GetValue<string>());
        Assert.False(datos.ContainsKey("version"));
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("cliente", HttpStatusCode.Forbidden)]
    [InlineData("recepcion", HttpStatusCode.Forbidden)]
    public async Task SoloAdministradorGestiona(string? actor, HttpStatusCode esperado)
    {
        using var app = new PerfilesFactory(); using var c = app.CrearCliente(actor);
        Assert.Equal(esperado, (await c.GetAsync("/api/administracion/contenido-sitio")).StatusCode);
    }

    [Theory]
    [InlineData("instagramUrl", "javascript:alert(1)")]
    [InlineData("facebookUrl", "http://facebook.com/club")]
    [InlineData("correo", "correo@@ejemplo.com")]
    [InlineData("whatsAppNumero", "no es un número")]
    [InlineData("nosotrosTitulo", " ")]
    [InlineData("testimonio", "Texto sin autor")]
    [InlineData("autorTestimonio", "Autor sin testimonio")]
    public async Task RechazaDatosInvalidos(string campo, string valor)
    {
        using var app = new PerfilesFactory(); using var admin = app.CrearCliente("admin");
        var actual = await admin.GetFromJsonAsync<JsonObject>("/api/administracion/contenido-sitio");
        actual!["contenido"]![campo] = valor;
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync("/api/administracion/contenido-sitio", actual)).StatusCode);
    }

    [Fact]
    public async Task RechazaEdicionParcialParaNoRestaurarValoresIniciales()
    {
        using var app = new PerfilesFactory(); using var admin = app.CrearCliente("admin");
        var actual = await admin.GetFromJsonAsync<JsonObject>("/api/administracion/contenido-sitio");
        ((JsonObject)actual!["contenido"]!).Remove("direccion");
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync("/api/administracion/contenido-sitio", actual)).StatusCode);
    }
}
