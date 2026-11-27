using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class DisponibilidadMostradorPostgresTests(PostgresEscriturasFixture fixture) : IClassFixture<PostgresEscriturasFixture>
{
    [PostgresFact]
    public async Task ConsultaMostradorYCreacionCompartenDisponibilidadDentroDelMinimoConTransaccionesReales()
    {
        var reloj = new RelojDisponibilidad(DateTimeOffset.Parse("2026-11-20T15:50:00Z")); var paquete = Guid.NewGuid();
        using var origen = new OperacionesFactory(fixture);
        using var app = origen.WithWebHostBuilder(b => b.ConfigureServices(s => { s.RemoveAll<TimeProvider>(); s.AddSingleton<TimeProvider>(reloj); }));
        using var admin = app.CreateClient(); admin.DefaultRequestHeaders.Add("X-Usuario-Prueba", OperacionesFactory.AdminId.ToString());
        using var publico = app.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/usuarios/yo")).StatusCode);
        Guid lineaLibre;
        await using (var db = fixture.CrearContext())
        {
            db.Paquetes.Add(new Paquete { Id = paquete, Codigo = "F19-PG", Nombre = "Actividad", IncluyeMarkdown = "Actividad", Activo = true,
                PrecioPorPersona = 100, Version = Guid.NewGuid(), CreadoEnUtc = reloj.Ahora.UtcDateTime, ActualizadoEnUtc = reloj.Ahora.UtcDateTime });
            var lineaBloqueada = await db.LineasTiro.SingleAsync(l => l.Numero == 1);
            lineaLibre = (await db.LineasTiro.SingleAsync(l => l.Numero == 3)).Id;
            (await db.LineasTiro.SingleAsync(l => l.Numero == 2)).Activa = false;
            db.BloqueosOperacion.Add(new BloqueoOperacion { Id = Guid.NewGuid(), LineaId = lineaBloqueada.Id, Motivo = "Privado",
                InicioUtc = DateTimeOffset.Parse("2026-11-20T16:10:00Z").UtcDateTime, FinUtc = DateTimeOffset.Parse("2026-11-20T16:15:00Z").UtcDateTime,
                Activo = true, Version = Guid.NewGuid(), CreadoPorId = OperacionesFactory.AdminId, CreadoEnUtc = reloj.Ahora.UtcDateTime, ActualizadoEnUtc = reloj.Ahora.UtcDateTime });
            await db.SaveChangesAsync();
        }
        var filtro = $"?paqueteId={paquete}&fecha=2026-11-20&hora=11:00:00";
        const string ruta = "/api/reservas/mostrador/disponibilidad";
        var consulta = await admin.GetFromJsonAsync<JsonElement>(ruta + filtro);
        var turno = Assert.Single(consulta.GetProperty("horarios").EnumerateArray());
        Assert.Equal(13, turno.GetProperty("lineasLibres").GetInt32());
        Assert.Equal("Bloqueada", turno.GetProperty("lineas")[0].GetProperty("estado").GetString());
        Assert.Equal("Inactiva", turno.GetProperty("lineas")[1].GetProperty("estado").GetString());
        Assert.Equal(0, (await publico.GetFromJsonAsync<JsonElement>("/api/disponibilidad" + filtro)).GetProperty("horarios")[0].GetProperty("lineasLibres").GetInt32());
        var datos = new { idOperacion = Guid.NewGuid(), clienteId = OperacionesFactory.ClienteId, paqueteId = paquete,
            fecha = "2026-11-20", hora = "11:00:00", participantes = new[] { new { lineaId = lineaLibre, nombre = "Visitante", nacimiento = "2000-01-01" } } };
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync("/api/reservas/mostrador", datos)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync("/api/reservas/mostrador", datos)).StatusCode);
        var ocupada = (await admin.GetFromJsonAsync<JsonElement>(ruta + filtro)).GetProperty("horarios")[0].GetProperty("lineas")[2];
        Assert.Equal("Apartada", ocupada.GetProperty("estado").GetString());
        using var cliente = app.CreateClient(); cliente.DefaultRequestHeaders.Add("X-Usuario-Prueba", OperacionesFactory.ClienteId.ToString());
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync(ruta + filtro)).StatusCode);
        await using var verificar = fixture.CrearContext();
        Assert.Equal(1, await verificar.ReservasWeb.CountAsync(r => r.Id == datos.idOperacion));
        Assert.Contains(await verificar.EventosAuditoria.ToListAsync(), e => e.EntidadId == datos.idOperacion && e.ActorId == OperacionesFactory.AdminId);
    }
}
