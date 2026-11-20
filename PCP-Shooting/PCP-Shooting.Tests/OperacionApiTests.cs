using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class OperacionApiTests
{
    private const string Ruta = "/api/administracion/operacion";
    private static object Bloqueo(Guid? lineaId = null, Guid? version = null, bool activo = true) => new
    {
        lineaId, inicioUtc = "2030-01-02T16:00:00Z", finUtc = "2030-01-02T16:15:00Z", motivo = "Mantenimiento", activo, version
    };

    [Fact]
    public async Task ConfiguracionInicialTieneHorariosYQuinceLineas()
    {
        using var factory = new PerfilesFactory();
        using var admin = factory.CrearCliente("admin");
        var configuracion = await admin.GetFromJsonAsync<JsonElement>(Ruta + "/configuracion");
        Assert.Equal("America/Cancun", configuracion.GetProperty("zonaHoraria").GetString());
        Assert.Equal(10, configuracion.GetProperty("duracionActividadMinutos").GetInt32());
        Assert.Equal(5, configuracion.GetProperty("margenMinutos").GetInt32());
        Assert.Equal(15, configuracion.GetProperty("intervaloTurnosMinutos").GetInt32());
        Assert.Equal(10, configuracion.GetProperty("apartadoPagoMinutos").GetInt32());
        Assert.Equal(60, configuracion.GetProperty("anticipacionMinimaMinutos").GetInt32());
        Assert.Equal(2, configuracion.GetProperty("anticipacionMaximaDias").GetInt32());
        var horarios = configuracion.GetProperty("horarios").EnumerateArray().ToArray();
        Assert.Equal(7, horarios.Length);
        Assert.True(horarios.Single(h => h.GetProperty("diaSemana").GetInt32() == 1).GetProperty("cerrado").GetBoolean());
        Assert.All(horarios.Where(h => h.GetProperty("diaSemana").GetInt32() != 1), h =>
        {
            Assert.Equal("08:00:00", h.GetProperty("apertura").GetString());
            Assert.Equal("18:00:00", h.GetProperty("cierre").GetString());
            Assert.Equal("11:00:00", h.GetProperty("primerTurno").GetString());
            Assert.Equal("15:30:00", h.GetProperty("ultimoTurno").GetString());
        });
        var lineas = await admin.GetFromJsonAsync<JsonElement>(Ruta + "/lineas");
        Assert.Equal(15, lineas.GetArrayLength());
        Assert.Equal(Enumerable.Range(1, 15), lineas.EnumerateArray().Select(l => l.GetProperty("numero").GetInt32()));
        Assert.All(lineas.EnumerateArray(), l => Assert.Equal(1, l.GetProperty("capacidad").GetInt32()));
    }

    [Fact]
    public async Task ConfiguracionEditableValidaTodosLosDiasYVersion()
    {
        using var factory = new PerfilesFactory();
        using var admin = factory.CrearCliente("admin");
        var original = await admin.GetFromJsonAsync<Dictionary<string, JsonElement>>(Ruta + "/configuracion");
        original!["apartadoPagoMinutos"] = JsonSerializer.SerializeToElement(12);
        var respuesta = await admin.PutAsJsonAsync(Ruta + "/configuracion", original);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var actualizado = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(12, actualizado.GetProperty("apartadoPagoMinutos").GetInt32());
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync(Ruta + "/configuracion", original)).StatusCode);
        var invalido = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(actualizado.GetRawText())!;
        invalido["horarios"] = JsonSerializer.SerializeToElement(Array.Empty<object>());
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync(Ruta + "/configuracion", invalido)).StatusCode);
        invalido = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(actualizado.GetRawText())!;
        invalido["duracionActividadMinutos"] = JsonSerializer.SerializeToElement(480);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync(Ruta + "/configuracion", invalido)).StatusCode);
    }

    [Fact]
    public async Task LineaSePuedeDesactivarPeroNoCambiarNumeroNiCapacidad()
    {
        using var factory = new PerfilesFactory();
        using var admin = factory.CrearCliente("admin");
        var lineas = await admin.GetFromJsonAsync<JsonElement>(Ruta + "/lineas");
        var linea = lineas[0];
        var response = await admin.PutAsJsonAsync(Ruta + "/lineas/" + linea.GetProperty("id").GetGuid(), new
        {
            nombre = "Línea de prueba", activa = false, version = linea.GetProperty("version").GetGuid(), numero = 99, capacidad = 10
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var editada = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(editada.GetProperty("activa").GetBoolean());
        Assert.Equal(1, editada.GetProperty("numero").GetInt32());
        Assert.Equal(1, editada.GetProperty("capacidad").GetInt32());
    }

    [Fact]
    public async Task BloqueosGlobalesYPorLineaRespetanInterseccionYSePuedenCancelar()
    {
        using var factory = new PerfilesFactory();
        using var admin = factory.CrearCliente("admin");
        var lineas = await admin.GetFromJsonAsync<JsonElement>(Ruta + "/lineas");
        var linea = lineas[0].GetProperty("id").GetGuid();
        var creado = await admin.PostAsJsonAsync(Ruta + "/bloqueos", Bloqueo());
        Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
        var global = await creado.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync(Ruta + "/bloqueos", Bloqueo(linea))).StatusCode);
        var consulta = await admin.GetFromJsonAsync<JsonElement>(Ruta + $"/bloqueos?lineaId={linea}&desdeUtc=2030-01-02T16:00:00Z&hastaUtc=2030-01-02T16:15:00Z");
        Assert.Equal(2, consulta.GetProperty("total").GetInt32());
        var limite = await admin.GetFromJsonAsync<JsonElement>(Ruta + "/bloqueos?desdeUtc=2030-01-02T16:15:00Z");
        Assert.Equal(0, limite.GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync(Ruta + "/bloqueos/" + global.GetProperty("id").GetGuid(),
            Bloqueo(version: global.GetProperty("version").GetGuid(), activo: false))).StatusCode);
        var activos = await admin.GetFromJsonAsync<JsonElement>(Ruta + "/bloqueos");
        Assert.Equal(1, activos.GetProperty("total").GetInt32());
        var todos = await admin.GetFromJsonAsync<JsonElement>(Ruta + "/bloqueos?activo=false");
        Assert.Equal(1, todos.GetProperty("total").GetInt32());
    }

    [Theory]
    [InlineData("2029-01-01T10:00:00Z", "2029-01-01T10:00:00Z", "Motivo")]
    [InlineData("2029-01-02T10:00:00Z", "2029-01-01T10:00:00Z", "Motivo")]
    [InlineData("2029-01-01T10:00:00Z", "2029-01-02T10:00:00Z", "   ")]
    public async Task BloqueosInvalidosDevuelven400(string inicio, string fin, string motivo)
    {
        using var factory = new PerfilesFactory();
        using var admin = factory.CrearCliente("admin");
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(Ruta + "/bloqueos", new { inicioUtc = inicio, finUtc = fin, motivo })).StatusCode);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("cliente", HttpStatusCode.Forbidden)]
    [InlineData("inactivo", HttpStatusCode.Forbidden)]
    public async Task ConfiguracionYBloqueosSonSoloParaPersonal(string? identidad, HttpStatusCode esperado)
    {
        using var factory = new PerfilesFactory();
        using var cliente = factory.CrearCliente(identidad);
        foreach (var recurso in new[] { "configuracion", "lineas", "bloqueos" })
            Assert.Equal(esperado, (await cliente.GetAsync(Ruta + "/" + recurso)).StatusCode);
        Assert.Equal(esperado, (await cliente.PostAsJsonAsync(Ruta + "/bloqueos", Bloqueo())).StatusCode);
    }

    [Fact]
    public async Task RecepcionConsultaYCreaBloqueosPeroNoConfigura()
    {
        using var factory = new JwtBackendFactory();
        using var recepcion = factory.Cliente(factory.Token(JwtBackendFactory.RecepcionId));
        Assert.Equal(HttpStatusCode.OK, (await recepcion.GetAsync(Ruta + "/configuracion")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await recepcion.GetAsync(Ruta + "/lineas")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await recepcion.PostAsJsonAsync(Ruta + "/bloqueos", Bloqueo())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await recepcion.PutAsJsonAsync(Ruta + "/configuracion", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await recepcion.PutAsJsonAsync(Ruta + "/lineas/" + Guid.NewGuid(), new { })).StatusCode);
    }
}
