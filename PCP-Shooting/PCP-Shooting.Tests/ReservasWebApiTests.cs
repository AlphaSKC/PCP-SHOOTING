using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class ReservasWebApiTests
{
    private static Guid Linea(DisponibilidadTestApp app) => app.Leer(db => db.LineasTiro.Single(l => l.Numero == 1).Id);
    private static async Task<Guid> Apartar(DisponibilidadTestApp app, HttpClient cliente)
    {
        var id = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.Created, (await cliente.PostAsJsonAsync("/api/apartados", new { idOperacion = id, paqueteId = app.PaqueteId,
            fecha = "2026-11-20", hora = "13:00:00", lineas = new[] { Linea(app) } })).StatusCode);
        return id;
    }
    private static object Datos(DisponibilidadTestApp app, Guid id, Guid apartado, string nacimiento = "2000-01-01", string? tutor = null, string? nacimientoTutor = null) =>
        new { idOperacion = id, apartadoId = apartado, participantes = new[] { new { lineaId = Linea(app), nombre = "Participante", nacimiento, tutorNombre = tutor, tutorNacimiento = nacimientoTutor } } };

    [Fact]
    public async Task CreaPendienteConPlazoOriginalYConservaSnapshotSinCobrar()
    {
        using var app = new DisponibilidadTestApp();
        using var cliente = app.ClientePara("cliente");
        var apartado = await Apartar(app, cliente);
        app.Reloj.Ahora = app.Reloj.Ahora.AddMinutes(3);
        var id = Guid.NewGuid();
        var respuesta = await cliente.PostAsJsonAsync("/api/reservas", Datos(app, id, apartado));
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var reserva = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("PendientePago", reserva.GetProperty("estado").GetString());
        Assert.Equal(100m, reserva.GetProperty("total").GetDecimal());
        Assert.Equal(DateTimeOffset.Parse("2026-11-20T15:10:00Z").UtcDateTime, reserva.GetProperty("vencePagoEnUtc").GetDateTime());
        app.Modificar(db => { var p = db.Paquetes.Single(p => p.Id == app.PaqueteId); p.Nombre = "Otro nombre"; p.PrecioPorPersona = 500m; });
        var detalle = await cliente.GetFromJsonAsync<JsonElement>("/api/reservas/" + id);
        Assert.Equal("Actividad", detalle.GetProperty("nombrePaquete").GetString());
        Assert.Equal(100m, detalle.GetProperty("total").GetDecimal());
        Assert.Equal(0, app.Leer(db => db.Pagos.Count()));
        Assert.Equal(0, app.Leer(db => db.MovimientosInventario.Count()));
        using var otro = app.ClientePara("admin");
        Assert.Equal(HttpStatusCode.NotFound, (await otro.GetAsync("/api/reservas/" + id)).StatusCode);
        app.Reloj.Ahora = DateTimeOffset.Parse("2026-11-20T15:10:00Z");
        Assert.Equal("Vencida", (await cliente.GetFromJsonAsync<JsonElement>("/api/reservas/" + id)).GetProperty("estado").GetString());
    }

    [Fact]
    public async Task ReintentoEsEstableYUnApartadoNoCreaDosReservas()
    {
        using var app = new DisponibilidadTestApp();
        using var cliente = app.ClientePara("cliente");
        var apartado = await Apartar(app, cliente);
        var id = Guid.NewGuid();
        var datos = Datos(app, id, apartado);
        Assert.Equal(HttpStatusCode.Created, (await cliente.PostAsJsonAsync("/api/reservas", datos)).StatusCode);
        app.Reloj.Ahora = app.Reloj.Ahora.AddMinutes(10);
        Assert.Equal(HttpStatusCode.Created, (await cliente.PostAsJsonAsync("/api/reservas", datos)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await cliente.PostAsJsonAsync("/api/reservas", Datos(app, Guid.NewGuid(), apartado))).StatusCode);
        var lista = await cliente.GetFromJsonAsync<JsonElement>("/api/reservas");
        Assert.Equal(1, lista.GetProperty("total").GetInt32());
    }

    [Theory]
    [InlineData("2018-01-01", null, null, HttpStatusCode.BadRequest)]
    [InlineData("2017-11-20", null, null, HttpStatusCode.BadRequest)]
    [InlineData("2017-11-20", "Tutor", "2010-01-01", HttpStatusCode.BadRequest)]
    [InlineData("2017-11-20", "Tutor", "2000-01-01", HttpStatusCode.Created)]
    public async Task ValidaEdadMinimaYTutorAdulto(string nacimiento, string? tutor, string? nacimientoTutor, HttpStatusCode esperado)
    {
        using var app = new DisponibilidadTestApp();
        using var cliente = app.ClientePara("cliente");
        var apartado = await Apartar(app, cliente);
        Assert.Equal(esperado, (await cliente.PostAsJsonAsync("/api/reservas", Datos(app, Guid.NewGuid(), apartado, nacimiento, tutor, nacimientoTutor))).StatusCode);
    }

    [Fact]
    public async Task NoUsaApartadoAjenoNiVencido()
    {
        using var app = new DisponibilidadTestApp();
        using var cliente = app.ClientePara("cliente");
        using var otro = app.ClientePara("admin");
        var apartado = await Apartar(app, cliente);
        Assert.Equal(HttpStatusCode.NotFound, (await otro.PostAsJsonAsync("/api/reservas", Datos(app, Guid.NewGuid(), apartado))).StatusCode);
        app.Reloj.Ahora = app.Reloj.Ahora.AddMinutes(10);
        Assert.Equal(HttpStatusCode.Conflict, (await cliente.PostAsJsonAsync("/api/reservas", Datos(app, Guid.NewGuid(), apartado))).StatusCode);
    }

    [Theory]
    [InlineData("paquete")]
    [InlineData("linea")]
    [InlineData("bloqueo")]
    public async Task RevalidaCambiosAntesDeGuardarSinCrearReserva(string cambio)
    {
        using var app = new DisponibilidadTestApp();
        using var cliente = app.ClientePara("cliente");
        var apartado = await Apartar(app, cliente);
        if (cambio == "paquete") app.Modificar(db => db.Paquetes.Single().Version = Guid.NewGuid());
        if (cambio == "linea") app.Modificar(db => db.LineasTiro.Single(l => l.Numero == 1).Activa = false);
        if (cambio == "bloqueo") app.Bloquear(1, "2026-11-20T18:00:00Z", "2026-11-20T18:15:00Z");
        Assert.Equal(HttpStatusCode.Conflict, (await cliente.PostAsJsonAsync("/api/reservas", Datos(app, Guid.NewGuid(), apartado))).StatusCode);
        Assert.Equal(0, app.Leer(db => db.ReservasWeb.Count()));
    }

    [Fact]
    public async Task ExigeSesionYCorrespondenciaDeLineasYRechazaCambioEnReintento()
    {
        using var app = new DisponibilidadTestApp();
        using var cliente = app.ClientePara("cliente");
        var apartado = await Apartar(app, cliente);
        var id = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.Unauthorized, (await app.Cliente.PostAsJsonAsync("/api/reservas", Datos(app, id, apartado))).StatusCode);
        var lineaAjena = app.Leer(db => db.LineasTiro.Single(l => l.Numero == 2).Id);
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync("/api/reservas", new { idOperacion = id, apartadoId = apartado,
            participantes = new[] { new { lineaId = lineaAjena, nombre = "Adulto", nacimiento = "2000-01-01" } } })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await cliente.PostAsJsonAsync("/api/reservas", Datos(app, id, apartado))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await cliente.PostAsJsonAsync("/api/reservas", Datos(app, id, apartado, "1990-01-01"))).StatusCode);
    }
}
