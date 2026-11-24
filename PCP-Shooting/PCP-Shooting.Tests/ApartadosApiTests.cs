using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class ApartadosApiTests
{
    private static Guid Linea(DisponibilidadTestApp app, int numero = 1) => app.Leer(db => db.LineasTiro.Single(l => l.Numero == numero).Id);
    private static object Datos(DisponibilidadTestApp app, Guid id, Guid[]? lineas = null, string hora = "11:00:00") => new
    {
        idOperacion = id, paqueteId = app.PaqueteId, fecha = "2026-11-20", hora, lineas = lineas ?? new[] { Linea(app) },
        usuarioId = PerfilesFactory.AdminId // El propietario se obtiene de la sesión, no del cuerpo.
    };

    [Fact]
    public async Task ApartaLineasConPlazoConfiguradoYLasOcultaEnDisponibilidadSinConsumirStock()
    {
        using var app = new DisponibilidadTestApp();
        using var cliente = app.ClientePara("cliente");
        var id = Guid.NewGuid();
        var respuesta = await cliente.PostAsJsonAsync("/api/apartados", Datos(app, id, new[] { Linea(app), Linea(app, 2) }));
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.NotNull(respuesta.Headers.Location);
        var apartado = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(id, apartado.GetProperty("id").GetGuid());
        Assert.Equal("Vigente", apartado.GetProperty("estado").GetString());
        Assert.Equal(app.Reloj.Ahora.AddMinutes(10).UtcDateTime, apartado.GetProperty("venceEnUtc").GetDateTime());
        Assert.Equal(2, apartado.GetProperty("lineas").GetArrayLength());
        var disponibilidad = await app.Cliente.GetFromJsonAsync<JsonElement>(app.Ruta() + "&hora=11:00");
        var turno = Assert.Single(disponibilidad.GetProperty("horarios").EnumerateArray());
        Assert.Equal(13, turno.GetProperty("lineasLibres").GetInt32());
        Assert.Equal("Apartada", turno.GetProperty("lineas")[0].GetProperty("estado").GetString());
        Assert.Equal(0, app.Leer(db => db.MovimientosInventario.Count()));
        Assert.Equal(0, app.Leer(db => db.Pagos.Count()));
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/apartados/" + id)).StatusCode);
        using var otro = app.ClientePara("admin");
        Assert.Equal(HttpStatusCode.NotFound, (await otro.GetAsync("/api/apartados/" + id)).StatusCode);
    }

    [Fact]
    public async Task ReintentoNoDuplicaNiAmpliaYRechazaReutilizarIdConOtraSeleccion()
    {
        using var app = new DisponibilidadTestApp();
        using var cliente = app.ClientePara("cliente");
        var id = Guid.NewGuid();
        var lineas = new[] { Linea(app), Linea(app, 2) };
        var primera = await cliente.PostAsJsonAsync("/api/apartados", Datos(app, id, lineas));
        var original = await primera.Content.ReadFromJsonAsync<JsonElement>();
        app.Reloj.Ahora = app.Reloj.Ahora.AddMinutes(2);
        var segunda = await cliente.PostAsJsonAsync("/api/apartados", Datos(app, id, lineas.Reverse().ToArray()));
        Assert.Equal(HttpStatusCode.Created, segunda.StatusCode);
        var repetido = await segunda.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(original.GetProperty("venceEnUtc").GetDateTime(), repetido.GetProperty("venceEnUtc").GetDateTime());
        Assert.Equal(HttpStatusCode.Conflict, (await cliente.PostAsJsonAsync("/api/apartados", Datos(app, id, hora: "11:15:00"))).StatusCode);
        using var otro = app.ClientePara("admin");
        Assert.Equal(HttpStatusCode.Conflict, (await otro.PostAsJsonAsync("/api/apartados", Datos(app, id, lineas))).StatusCode);
    }

    [Fact]
    public async Task VencimientoOriginalLiberaCapacidadSinQueReintentarLoRenueve()
    {
        using var app = new DisponibilidadTestApp();
        using var cliente = app.ClientePara("cliente");
        var id = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.Created, (await cliente.PostAsJsonAsync("/api/apartados", Datos(app, id, hora: "12:00:00"))).StatusCode);
        app.Modificar(db => db.ConfiguracionOperativa.Single().ApartadoPagoMinutos = 20);
        app.Reloj.Ahora = app.Reloj.Ahora.AddMinutes(10);
        var repetido = await cliente.PostAsJsonAsync("/api/apartados", Datos(app, id, hora: "12:00:00"));
        Assert.Equal("Vencido", (await repetido.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("estado").GetString());
        var disponible = await app.Cliente.GetFromJsonAsync<JsonElement>(app.Ruta() + "&hora=12:00");
        Assert.Equal(15, disponible.GetProperty("horarios")[0].GetProperty("lineasLibres").GetInt32());
        var nuevo = await cliente.PostAsJsonAsync("/api/apartados", Datos(app, Guid.NewGuid(), hora: "12:00:00"));
        Assert.Equal(HttpStatusCode.Created, nuevo.StatusCode);
        Assert.Equal(app.Reloj.Ahora.AddMinutes(20).UtcDateTime, (await nuevo.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("venceEnUtc").GetDateTime());
    }

    [Fact]
    public async Task RevalidaBloqueosYSeleccionCompletaAntesDeCrear()
    {
        using var app = new DisponibilidadTestApp();
        using var cliente = app.ClientePara("cliente");
        app.Bloquear(2, "2026-11-20T16:00:00Z", "2026-11-20T16:15:00Z");
        Assert.Equal(HttpStatusCode.Conflict, (await cliente.PostAsJsonAsync("/api/apartados", Datos(app, Guid.NewGuid(), new[] { Linea(app), Linea(app, 2) }))).StatusCode);
        var disponibilidad = await app.Cliente.GetFromJsonAsync<JsonElement>(app.Ruta() + "&hora=11:00");
        Assert.Equal("Libre", disponibilidad.GetProperty("horarios")[0].GetProperty("lineas")[0].GetProperty("estado").GetString());
        Assert.Equal(HttpStatusCode.Created, (await cliente.PostAsJsonAsync("/api/apartados", Datos(app, Guid.NewGuid()))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await cliente.PostAsJsonAsync("/api/apartados", Datos(app, Guid.NewGuid()))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await cliente.PostAsJsonAsync("/api/apartados", Datos(app, Guid.NewGuid(), hora: "11:15:00"))).StatusCode);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("inactivo", HttpStatusCode.Forbidden)]
    public async Task RequierePerfilActivo(string? identidad, HttpStatusCode esperado)
    {
        using var app = new DisponibilidadTestApp();
        using var cliente = app.ClientePara(identidad);
        Assert.Equal(esperado, (await cliente.PostAsJsonAsync("/api/apartados", Datos(app, Guid.NewGuid()))).StatusCode);
        Assert.Equal(esperado, (await cliente.GetAsync("/api/apartados/" + Guid.NewGuid())).StatusCode);
    }

    [Fact]
    public async Task RechazaSeleccionVaciaDuplicadaYLineaInexistente()
    {
        using var app = new DisponibilidadTestApp();
        using var cliente = app.ClientePara("cliente");
        foreach (var lineas in new[] { Array.Empty<Guid>(), new[] { Linea(app), Linea(app) }, new[] { Guid.NewGuid() } })
            Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync("/api/apartados", Datos(app, Guid.NewGuid(), lineas))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync("/api/apartados", Datos(app, Guid.Empty))).StatusCode);
    }
}
