using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class ContactoApiTests
{
    private static object Datos(Guid id, bool privacidad = true, string mensaje = "Necesito información del club.") =>
        new { idOperacion = id, nombre = "Visitante", email = "visitante@example.invalid", telefono = (string?)null, motivo = "otro", mensaje, privacidad };

    [Fact]
    public async Task PublicoEnviaUnaVezYAdminGestionaSinExponerNotasAlRemitente()
    {
        using var app = new PerfilesFactory(); using var publico = app.CrearCliente(null); using var admin = app.CrearCliente("admin");
        var id = Guid.NewGuid(); var datos = Datos(id);
        Assert.Equal(HttpStatusCode.Created, (await publico.PostAsJsonAsync("/api/contacto", datos)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await publico.PostAsJsonAsync("/api/contacto", datos)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await publico.PostAsJsonAsync("/api/contacto", Datos(id, mensaje: "Un mensaje diferente y válido."))).StatusCode);
        var lista = await admin.GetFromJsonAsync<JsonElement>("/api/administracion/contacto?estado=Pendiente&busqueda=visitante");
        Assert.Equal(1, lista.GetProperty("total").GetInt32());
        var detalle = await admin.GetFromJsonAsync<JsonElement>($"/api/administracion/contacto/{id}");
        Assert.Equal(JsonValueKind.Null, detalle.GetProperty("telefono").ValueKind);
        var cambio = new { estado = "Atendida", notasInternas = "Seguimiento interno", version = detalle.GetProperty("version").GetGuid() };
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/administracion/contacto/{id}", cambio)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync($"/api/administracion/contacto/{id}", cambio)).StatusCode);
        var repetido = await publico.PostAsJsonAsync("/api/contacto", datos);
        var respuesta = await repetido.Content.ReadAsStringAsync();
        Assert.Contains("Recibida", respuesta); Assert.DoesNotContain("Seguimiento", respuesta); Assert.DoesNotContain("Atendida", respuesta);
        Assert.Equal(HttpStatusCode.NotFound, (await publico.GetAsync($"/api/contacto/{id}")).StatusCode);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("cliente", HttpStatusCode.Forbidden)]
    [InlineData("recepcion", HttpStatusCode.Forbidden)]
    public async Task ListadoYDetalleSoloAdministracion(string? actor, HttpStatusCode esperado)
    {
        using var app = new PerfilesFactory(); using var cliente = app.CrearCliente(actor);
        Assert.Equal(esperado, (await cliente.GetAsync("/api/administracion/contacto")).StatusCode);
        Assert.Equal(esperado, (await cliente.GetAsync($"/api/administracion/contacto/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task LimitaEnviosPorOrigen()
    {
        using var app = new PerfilesFactory(); using var publico = app.CrearCliente(null);
        for (var i = 0; i < 5; i++) Assert.Equal(HttpStatusCode.Created, (await publico.PostAsJsonAsync("/api/contacto", Datos(Guid.NewGuid()))).StatusCode);
        publico.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.99");
        var response = await publico.PostAsJsonAsync("/api/contacto", Datos(Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.NotNull(response.Headers.RetryAfter);
    }

    [Fact]
    public async Task RequierePrivacidadYDatosValidos()
    {
        using var app = new PerfilesFactory(); using var publico = app.CrearCliente(null);
        Assert.Equal(HttpStatusCode.BadRequest, (await publico.PostAsJsonAsync("/api/contacto", Datos(Guid.NewGuid(), false))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await publico.PostAsJsonAsync("/api/contacto", Datos(Guid.NewGuid(), mensaje: "corto"))).StatusCode);
    }

    [Theory]
    [InlineData("email", "correo@@invalido")]
    [InlineData("motivo", "desconocido")]
    [InlineData("sitioWeb", "https://bot.example.invalid")]
    [InlineData("idOperacion", "00000000-0000-0000-0000-000000000000")]
    public async Task RechazaCamposInvalidosYCampoTrampa(string campo, string valor)
    {
        using var app = new PerfilesFactory(); using var publico = app.CrearCliente(null);
        var datos = JsonSerializer.SerializeToNode(Datos(Guid.NewGuid()))!.AsObject(); datos[campo] = valor;
        Assert.Equal(HttpStatusCode.BadRequest, (await publico.PostAsJsonAsync("/api/contacto", datos)).StatusCode);
    }
}
