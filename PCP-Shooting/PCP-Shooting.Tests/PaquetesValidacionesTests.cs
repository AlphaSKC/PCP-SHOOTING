using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class PaquetesValidacionesTests
{
    [Theory]
    [InlineData("/api/paquetes?pagina=0")]
    [InlineData("/api/paquetes?tamanoPagina=101")]
    [InlineData("/api/administracion/paquetes?activo=invalido")]
    public async Task FiltrosInvalidosSeRechazan(string ruta)
    {
        using var factory = new PerfilesFactory();
        using var admin = factory.CrearCliente("admin");
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync(ruta)).StatusCode);
    }

    [Theory]
    [InlineData(null, 0d)]
    [InlineData(0d, 0d)]
    [InlineData(100d, -1d)]
    public async Task ActivoRequierePreciosPositivos(double? precio, double socio)
    {
        using var factory = new PerfilesFactory();
        using var admin = factory.CrearCliente("admin");
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/administracion/paquetes", new
        {
            codigo = "PRECIO", nombre = "Prueba", incluyeMarkdown = "Incluye", activo = true, precioPorPersona = precio, precioSocioCartaq = socio
        })).StatusCode);
    }

    [Fact]
    public async Task PermitePrecioPendienteEnPaqueteInactivoYDesactivarSinBorrar()
    {
        using var factory = new PerfilesFactory();
        using var admin = factory.CrearCliente("admin");
        var respuesta = await admin.PostAsJsonAsync("/api/administracion/paquetes", new { codigo = "BORRADOR", nombre = "Pendiente", incluyeMarkdown = "Por completar" });
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var detalle = await respuesta.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(System.Text.Json.JsonValueKind.Null, detalle.GetProperty("precioPorPersona").ValueKind);
        Assert.False(detalle.GetProperty("activo").GetBoolean());
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/paquetes/" + detalle.GetProperty("id").GetGuid())).StatusCode);
    }
}
