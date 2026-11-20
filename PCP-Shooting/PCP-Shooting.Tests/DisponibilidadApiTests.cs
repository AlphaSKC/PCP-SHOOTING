using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class DisponibilidadApiTests
{
    [Fact]
    public async Task GeneraTurnosEnHoraLocalConMargenYQuinceLineasSinEscrituras()
    {
        using var app = new DisponibilidadTestApp();
        var antes = app.Leer(db => db.EventosAuditoria.Count());
        var respuesta = await app.Cliente.GetAsync(app.Ruta());
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("no-store", respuesta.Headers.CacheControl!.ToString());
        var datos = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("America/Cancun", datos.GetProperty("zonaHoraria").GetString());
        var turnos = datos.GetProperty("horarios");
        Assert.Equal(19, turnos.GetArrayLength());
        Assert.Equal("11:00:00", turnos[0].GetProperty("hora").GetString());
        Assert.Equal("15:30:00", turnos[18].GetProperty("hora").GetString());
        Assert.Equal(DateTime.Parse("2026-11-20T16:00:00Z").ToUniversalTime(), turnos[0].GetProperty("inicioUtc").GetDateTime());
        Assert.Equal(TimeSpan.FromMinutes(10), turnos[0].GetProperty("finActividadUtc").GetDateTime() - turnos[0].GetProperty("inicioUtc").GetDateTime());
        Assert.Equal(TimeSpan.FromMinutes(15), turnos[0].GetProperty("finOcupacionUtc").GetDateTime() - turnos[0].GetProperty("inicioUtc").GetDateTime());
        Assert.All(turnos.EnumerateArray(), turno =>
        {
            Assert.True(turno.GetProperty("disponible").GetBoolean());
            Assert.Equal(15, turno.GetProperty("lineasLibres").GetInt32());
            Assert.Equal(15, turno.GetProperty("lineas").GetArrayLength());
        });
        Assert.Equal(antes, app.Leer(db => db.EventosAuditoria.Count()));
    }

    [Fact]
    public async Task BloqueoDuranteMargenImpideTurnoPeroNoElSiguiente()
    {
        using var app = new DisponibilidadTestApp();
        app.Bloquear(1, "2026-11-20T16:10:00Z", "2026-11-20T16:15:00Z");
        var datos = await app.Cliente.GetFromJsonAsync<JsonElement>(app.Ruta());
        var turnos = datos.GetProperty("horarios");
        Assert.Equal(14, turnos[0].GetProperty("lineasLibres").GetInt32());
        var linea = turnos[0].GetProperty("lineas")[0];
        Assert.Equal("Bloqueada", linea.GetProperty("estado").GetString());
        Assert.False(linea.TryGetProperty("motivo", out _));
        Assert.Equal(15, turnos[1].GetProperty("lineasLibres").GetInt32());
    }

    [Fact]
    public async Task BloqueoGlobalYLineaInactivaSeAplicanSinContarBloqueosCancelados()
    {
        using var app = new DisponibilidadTestApp();
        app.Modificar(db => db.LineasTiro.Single(l => l.Numero == 2).Activa = false);
        app.Bloquear(null, "2026-11-20T17:00:00Z", "2026-11-20T17:15:00Z");
        app.Bloquear(null, "2026-11-20T16:00:00Z", "2026-11-20T16:15:00Z", false);
        var datos = await app.Cliente.GetFromJsonAsync<JsonElement>(app.Ruta());
        var turnos = datos.GetProperty("horarios");
        Assert.Equal(14, turnos[0].GetProperty("lineasLibres").GetInt32());
        Assert.Equal("Inactiva", turnos[0].GetProperty("lineas")[1].GetProperty("estado").GetString());
        Assert.Equal(0, turnos[4].GetProperty("lineasLibres").GetInt32());
        Assert.False(turnos[4].GetProperty("disponible").GetBoolean());
        Assert.Equal(14, turnos[5].GetProperty("lineasLibres").GetInt32());
    }

    [Fact]
    public async Task AplicaAnticipacionMinimaYMaximaInclusivasConRelojControlado()
    {
        using var app = new DisponibilidadTestApp();
        app.Reloj.Ahora = DateTimeOffset.Parse("2026-11-20T15:01:00Z");
        var minimo = await app.Cliente.GetFromJsonAsync<JsonElement>(app.Ruta());
        Assert.False(minimo.GetProperty("horarios")[0].GetProperty("disponible").GetBoolean());
        Assert.True(minimo.GetProperty("horarios")[1].GetProperty("disponible").GetBoolean());
        app.Reloj.Ahora = DateTimeOffset.Parse("2026-11-20T16:00:00Z");
        var maximo = await app.Cliente.GetFromJsonAsync<JsonElement>(app.Ruta(fecha: "2026-11-22"));
        Assert.True(maximo.GetProperty("horarios")[0].GetProperty("disponible").GetBoolean());
        Assert.False(maximo.GetProperty("horarios")[1].GetProperty("disponible").GetBoolean());
    }

    [Fact]
    public async Task LunesCerradoNoGeneraHorarios()
    {
        using var app = new DisponibilidadTestApp();
        var datos = await app.Cliente.GetFromJsonAsync<JsonElement>(app.Ruta(fecha: "2026-11-23"));
        Assert.False(datos.GetProperty("diaAbierto").GetBoolean());
        Assert.Empty(datos.GetProperty("horarios").EnumerateArray());
    }

    [Fact]
    public async Task ConsultarUnHorarioYGrupoExigeLineasSuficientesSinAsignarlas()
    {
        using var app = new DisponibilidadTestApp();
        app.Modificar(db => db.LineasTiro.Single(l => l.Numero == 1).Activa = false);
        var datos = await app.Cliente.GetFromJsonAsync<JsonElement>(app.Ruta() + "&hora=11:15&personas=15");
        var turno = Assert.Single(datos.GetProperty("horarios").EnumerateArray());
        Assert.Equal(14, turno.GetProperty("lineasLibres").GetInt32());
        Assert.False(turno.GetProperty("disponible").GetBoolean());
    }

    [Theory]
    [InlineData("hora=11:01")]
    [InlineData("hora=08:00")]
    [InlineData("hora=invalida")]
    [InlineData("personas=0")]
    [InlineData("personas=16")]
    public async Task RechazaHoraOGrupoInvalido(string filtro)
    {
        using var app = new DisponibilidadTestApp();
        Assert.Equal(HttpStatusCode.BadRequest, (await app.Cliente.GetAsync(app.Ruta() + "&" + filtro)).StatusCode);
    }

    [Theory]
    [InlineData("/api/disponibilidad")]
    [InlineData("/api/disponibilidad?fecha=2026-11-20")]
    [InlineData("/api/disponibilidad?paqueteId=00000000-0000-0000-0000-000000000000&fecha=2026-11-20")]
    public async Task ParametrosObligatoriosInvalidosDevuelven400(string ruta)
    {
        using var app = new DisponibilidadTestApp();
        Assert.Equal(HttpStatusCode.BadRequest, (await app.Cliente.GetAsync(ruta)).StatusCode);
    }

    [Fact]
    public async Task PaquetesOcultosONoExistentesDevuelven404()
    {
        using var app = new DisponibilidadTestApp();
        Assert.Equal(HttpStatusCode.NotFound, (await app.Cliente.GetAsync($"/api/disponibilidad?paqueteId={Guid.NewGuid()}&fecha=2026-11-20")).StatusCode);
        app.Modificar(db => db.Paquetes.Single(p => p.Id == app.PaqueteId).Activo = false);
        Assert.Equal(HttpStatusCode.NotFound, (await app.Cliente.GetAsync(app.Ruta())).StatusCode);
    }

    [Fact]
    public async Task UsaCambiosDeConfiguracionSinFijarLosQuinceMinutosEnCodigo()
    {
        using var app = new DisponibilidadTestApp();
        app.Modificar(db =>
        {
            db.ConfiguracionOperativa.Single().DuracionActividadMinutos = 15;
            foreach (var horario in db.HorariosOperacion) horario.UltimoTurno = new TimeOnly(15, 0);
        });
        var datos = await app.Cliente.GetFromJsonAsync<JsonElement>(app.Ruta());
        var turnos = datos.GetProperty("horarios");
        Assert.Equal(13, turnos.GetArrayLength());
        Assert.Equal("11:20:00", turnos[1].GetProperty("hora").GetString());
        Assert.Equal(TimeSpan.FromMinutes(20), turnos[0].GetProperty("finOcupacionUtc").GetDateTime() - turnos[0].GetProperty("inicioUtc").GetDateTime());
    }

    [Theory]
    [InlineData("2026-11-19")]
    [InlineData("9999-12-31")]
    public async Task FechasFueraDeVentanaNoOfrecenTurnos(string fecha)
    {
        using var app = new DisponibilidadTestApp();
        var datos = await app.Cliente.GetFromJsonAsync<JsonElement>(app.Ruta(fecha));
        Assert.Empty(datos.GetProperty("horarios").EnumerateArray());
    }
}

internal sealed class RelojDisponibilidad(DateTimeOffset ahora) : TimeProvider
{
    public DateTimeOffset Ahora { get; set; } = ahora;
    public override DateTimeOffset GetUtcNow() => Ahora;
}

internal sealed class DisponibilidadTestApp : IDisposable
{
    private readonly PerfilesFactory baseFactory = new();
    private readonly WebApplicationFactory<Program> app;
    public HttpClient Cliente { get; }
    public Guid PaqueteId { get; } = Guid.NewGuid();
    public RelojDisponibilidad Reloj { get; } = new(DateTimeOffset.Parse("2026-11-20T15:00:00Z"));

    public DisponibilidadTestApp()
    {
        app = baseFactory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Reloj);
        }));
        Cliente = app.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        Modificar(db => db.Paquetes.Add(new Paquete { Id = PaqueteId, Codigo = "DISP-PRUEBA", Nombre = "Actividad", IncluyeMarkdown = "Actividad",
            Activo = true, PrecioPorPersona = 100m, Version = Guid.NewGuid(), CreadoEnUtc = Reloj.Ahora.UtcDateTime, ActualizadoEnUtc = Reloj.Ahora.UtcDateTime }));
    }

    public string Ruta(string fecha = "2026-11-20") => $"/api/disponibilidad?paqueteId={PaqueteId}&fecha={fecha}";
    public T Leer<T>(Func<PcpShootingDbContext, T> leer)
    {
        using var scope = app.Services.CreateScope();
        return leer(scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>());
    }
    public void Modificar(Action<PcpShootingDbContext> modificar)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>();
        modificar(db);
        db.SaveChanges();
    }
    public void Bloquear(int? numero, string inicio, string fin, bool activo = true) => Modificar(db =>
        db.BloqueosOperacion.Add(new BloqueoOperacion { Id = Guid.NewGuid(), LineaId = numero.HasValue ? db.LineasTiro.Single(l => l.Numero == numero).Id : null,
            InicioUtc = DateTimeOffset.Parse(inicio).UtcDateTime, FinUtc = DateTimeOffset.Parse(fin).UtcDateTime,
            Motivo = "Motivo privado", Activo = activo, CreadoPorId = PerfilesFactory.AdminId, CreadoEnUtc = Reloj.Ahora.UtcDateTime,
            ActualizadoEnUtc = Reloj.Ahora.UtcDateTime, Version = Guid.NewGuid() }));
    public void Dispose() { Cliente.Dispose(); app.Dispose(); baseFactory.Dispose(); }
}
