using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class DisponibilidadMostradorApiTests
{
    private static string Ruta(DisponibilidadTestApp app, string fecha = "2026-11-20", string hora = "11:00:00", int personas = 1) =>
        $"/api/reservas/mostrador/disponibilidad?paqueteId={app.PaqueteId}&fecha={fecha}&hora={hora}&personas={personas}";

    [Theory]
    [InlineData("admin")]
    [InlineData("recepcion")]
    public async Task PersonalVeBloqueosInactivasYLibresDentroDelMinimoSinCambiarConsultaPublica(string actor)
    {
        using var app = new DisponibilidadTestApp(); using var staff = app.ClientePara(actor);
        app.Reloj.Ahora = DateTimeOffset.Parse("2026-11-20T15:50:00Z");
        app.Bloquear(1, "2026-11-20T16:10:00Z", "2026-11-20T16:15:00Z");
        app.Modificar(db => db.LineasTiro.Single(l => l.Numero == 2).Activa = false);
        var auditoria = app.Leer(db => db.EventosAuditoria.Count());
        var respuesta = await staff.GetAsync(Ruta(app, personas: 14)); Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("no-store", respuesta.Headers.CacheControl!.ToString());
        var turno = Assert.Single((await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("horarios").EnumerateArray());
        Assert.Equal(13, turno.GetProperty("lineasLibres").GetInt32()); Assert.False(turno.GetProperty("disponible").GetBoolean());
        Assert.Equal("Bloqueada", turno.GetProperty("lineas")[0].GetProperty("estado").GetString());
        Assert.Equal("Inactiva", turno.GetProperty("lineas")[1].GetProperty("estado").GetString());
        Assert.Equal("Libre", turno.GetProperty("lineas")[2].GetProperty("estado").GetString());
        var publico = await app.Cliente.GetFromJsonAsync<JsonElement>(app.Ruta() + "&hora=11:00:00&mostrador=true");
        Assert.Equal("FueraDeAnticipacion", publico.GetProperty("horarios")[0].GetProperty("lineas")[0].GetProperty("estado").GetString());
        Assert.Equal(0, publico.GetProperty("horarios")[0].GetProperty("lineasLibres").GetInt32());
        Assert.Equal(auditoria, app.Leer(db => db.EventosAuditoria.Count()));
    }

    [Fact]
    public async Task CrearMostradorUsaLaExcepcionYUnaLineaYaReservadaSigueOcupada()
    {
        using var app = new DisponibilidadTestApp(); using var staff = app.ClientePara("recepcion");
        app.Reloj.Ahora = DateTimeOffset.Parse("2026-11-20T15:50:00Z");
        var linea = app.Leer(db => db.LineasTiro.Single(l => l.Numero == 1).Id);
        var datos = new { idOperacion = Guid.NewGuid(), clienteId = PerfilesFactory.ClienteId, paqueteId = app.PaqueteId,
            fecha = "2026-11-20", hora = "11:00:00", participantes = new[] { new { lineaId = linea, nombre = "Visita", nacimiento = "2000-01-01" } } };
        Assert.Equal(HttpStatusCode.Created, (await staff.PostAsJsonAsync("/api/reservas/mostrador", datos)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await staff.PostAsJsonAsync("/api/reservas/mostrador", datos)).StatusCode);
        var turno = Assert.Single((await staff.GetFromJsonAsync<JsonElement>(Ruta(app))).GetProperty("horarios").EnumerateArray());
        Assert.Equal("Apartada", turno.GetProperty("lineas")[0].GetProperty("estado").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await staff.PostAsJsonAsync("/api/reservas/mostrador", new { idOperacion = Guid.NewGuid(), datos.clienteId, datos.paqueteId, datos.fecha, datos.hora, datos.participantes })).StatusCode);
        Assert.Equal(1, app.Leer(db => db.ReservasWeb.Count()));
    }

    [Fact]
    public async Task ConservaOcupacionDePagosPendientesYConciliacionTrasVencerElApartado()
    {
        using var app = new DisponibilidadTestApp(); using var cliente = app.ClientePara("cliente"); using var staff = app.ClientePara("recepcion");
        var linea = app.Leer(db => db.LineasTiro.Single(l => l.Numero == 1).Id); var apartado = Guid.NewGuid(); var reserva = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.Created, (await cliente.PostAsJsonAsync("/api/apartados", new { idOperacion = apartado, paqueteId = app.PaqueteId,
            fecha = "2026-11-20", hora = "11:00:00", lineas = new[] { linea } })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await cliente.PostAsJsonAsync("/api/reservas", new { idOperacion = reserva, apartadoId = apartado,
            participantes = new[] { new { lineaId = linea, nombre = "Participante", nacimiento = "2000-01-01" } } })).StatusCode);
        var pagoId = Guid.NewGuid();
        app.Modificar(db => db.PagosReserva.Add(new PagoReserva { Id = pagoId, ReservaId = reserva, Importe = 100, Estado = EstadoPago.Pendiente, CreadoEnUtc = app.Reloj.Ahora.UtcDateTime }));
        app.Reloj.Ahora = DateTimeOffset.Parse("2026-11-20T15:50:00Z");
        async Task<string?> Estado() => (await staff.GetFromJsonAsync<JsonElement>(Ruta(app))).GetProperty("horarios")[0].GetProperty("lineas")[0].GetProperty("estado").GetString();
        Assert.Equal("Apartada", await Estado());
        app.Modificar(db => { var p = db.PagosReserva.Single(p => p.Id == pagoId); p.Estado = EstadoPago.Rechazado; p.ConciliacionEnCurso = true; });
        Assert.Equal("Apartada", await Estado());
        app.Modificar(db => db.PagosReserva.Single(p => p.Id == pagoId).ConciliacionEnCurso = false);
        Assert.Equal("Libre", await Estado());
        app.Modificar(db => db.ReservasWeb.Single(r => r.Id == reserva).PagoConfirmadoEnUtc = app.Reloj.Ahora.UtcDateTime);
        Assert.Equal("Apartada", await Estado());
    }

    [Fact]
    public async Task NoOfreceTurnosIniciadosNiAmpliaElMaximoYValidaFiltros()
    {
        using var app = new DisponibilidadTestApp(); using var staff = app.ClientePara("recepcion");
        app.Reloj.Ahora = DateTimeOffset.Parse("2026-11-20T16:00:00Z");
        var iniciado = await staff.GetFromJsonAsync<JsonElement>(Ruta(app));
        Assert.Equal(0, iniciado.GetProperty("horarios")[0].GetProperty("lineasLibres").GetInt32());
        Assert.Equal(15, (await staff.GetFromJsonAsync<JsonElement>(Ruta(app, hora: "11:15:00"))).GetProperty("horarios")[0].GetProperty("lineasLibres").GetInt32());
        Assert.Equal(15, (await staff.GetFromJsonAsync<JsonElement>(Ruta(app, "2026-11-22"))).GetProperty("horarios")[0].GetProperty("lineasLibres").GetInt32());
        Assert.Equal(0, (await staff.GetFromJsonAsync<JsonElement>(Ruta(app, "2026-11-22", "11:15:00"))).GetProperty("horarios")[0].GetProperty("lineasLibres").GetInt32());
        Assert.Empty((await staff.GetFromJsonAsync<JsonElement>(Ruta(app, "2026-11-19"))).GetProperty("horarios").EnumerateArray());
        Assert.False((await staff.GetFromJsonAsync<JsonElement>(Ruta(app, "2026-11-23"))).GetProperty("diaAbierto").GetBoolean());
        Assert.Equal(HttpStatusCode.BadRequest, (await staff.GetAsync(Ruta(app, hora: "11:01:00"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await staff.GetAsync(Ruta(app, personas: 16))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await staff.GetAsync("/api/reservas/mostrador/disponibilidad")).StatusCode);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("cliente", HttpStatusCode.Forbidden)]
    [InlineData("inactivo", HttpStatusCode.Forbidden)]
    public async Task SoloPersonalActivoConsulta(string? actor, HttpStatusCode esperado)
    {
        using var app = new DisponibilidadTestApp(); using var c = app.ClientePara(actor);
        Assert.Equal(esperado, (await c.GetAsync(Ruta(app))).StatusCode);
    }
}
