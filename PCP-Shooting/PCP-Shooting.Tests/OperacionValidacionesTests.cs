using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class OperacionValidacionesTests
{
    private const string Ruta = "/api/administracion/operacion";
    [Theory]
    [InlineData("apartadoPagoMinutos", 0)]
    [InlineData("margenMinutos", -1)]
    [InlineData("anticipacionMinimaMinutos", 4000)]
    public async Task RechazaLimitesInvalidos(string campo, int valor)
    {
        using var factory = new PerfilesFactory();
        using var admin = factory.CrearCliente("admin");
        var datos = (await admin.GetFromJsonAsync<Dictionary<string, JsonElement>>(Ruta + "/configuracion"))!;
        datos[campo] = JsonSerializer.SerializeToElement(valor);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync(Ruta + "/configuracion", datos)).StatusCode);
    }

    [Theory]
    [InlineData("cierre", "15:40:00")]
    [InlineData("ultimoTurno", "15:31:00")]
    [InlineData("primerTurno", "07:00:00")]
    public async Task RechazaHorariosQueNoRespetanTurnosYMargen(string campo, string valor)
    {
        using var factory = new PerfilesFactory();
        using var admin = factory.CrearCliente("admin");
        var datos = (await admin.GetFromJsonAsync<Dictionary<string, JsonElement>>(Ruta + "/configuracion"))!;
        var horarios = datos["horarios"].Deserialize<List<Dictionary<string, JsonElement>>>()!;
        horarios[0][campo] = JsonSerializer.SerializeToElement(valor);
        datos["horarios"] = JsonSerializer.SerializeToElement(horarios);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync(Ruta + "/configuracion", datos)).StatusCode);
    }

    [Theory]
    [InlineData("pagina=0")]
    [InlineData("tamanoPagina=101")]
    [InlineData("desdeUtc=2030-02-01&hastaUtc=2030-01-01")]
    public async Task FiltrosInvalidosDevuelven400(string filtro)
    {
        using var factory = new PerfilesFactory();
        using var admin = factory.CrearCliente("admin");
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync(Ruta + "/bloqueos?" + filtro)).StatusCode);
    }
}
