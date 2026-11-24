using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Domain.Services;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;
using PCP_Shooting.Persistence.Services;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class DisponibilidadPostgresTests(PostgresEscriturasFixture fixture) : IClassFixture<PostgresEscriturasFixture>
{
    private readonly RelojDisponibilidad reloj = new(DateTimeOffset.Parse("2026-11-20T15:00:00Z"));
    private DisponibilidadService Servicio(PcpShootingDbContext db) => new(db, new OperacionService(db, reloj),
        new PaquetesService(db, reloj, new TransaccionesPersistence(db)), reloj);

    private async Task<(Guid Paquete, Guid Autor)> Preparar()
    {
        await using var db = fixture.CrearContext();
        var autor = Guid.NewGuid();
        var paquete = Guid.NewGuid();
        db.PerfilesUsuario.Add(new PerfilUsuario { Id = autor, Nombre = "Personal de prueba" });
        db.Paquetes.Add(new Paquete { Id = paquete, Codigo = "DISP-" + paquete.ToString("N").ToUpperInvariant(), Nombre = "Actividad",
            IncluyeMarkdown = "Actividad", Activo = true, PrecioPorPersona = 100m, Version = Guid.NewGuid(),
            CreadoEnUtc = reloj.Ahora.UtcDateTime, ActualizadoEnUtc = reloj.Ahora.UtcDateTime });
        await db.SaveChangesAsync();
        return (paquete, autor);
    }

    [PostgresFact]
    public async Task ConsultaFiltraBloqueosDelDiaConMargenYNoGeneraAuditoriaNiApartados()
    {
        var (paquete, autor) = await Preparar();
        await using var db = fixture.CrearContext();
        db.BloqueosOperacion.Add(new BloqueoOperacion { Id = Guid.NewGuid(), InicioUtc = DateTime.Parse("2026-11-20T17:10:00Z").ToUniversalTime(),
            FinUtc = DateTime.Parse("2026-11-20T17:15:00Z").ToUniversalTime(), Motivo = "Privado", CreadoPorId = autor,
            CreadoEnUtc = reloj.Ahora.UtcDateTime, ActualizadoEnUtc = reloj.Ahora.UtcDateTime, Version = Guid.NewGuid() });
        await db.SaveChangesAsync();
        var auditoria = await db.EventosAuditoria.CountAsync();
        var resultado = await Servicio(db).ConsultarAsync(new ConsultaDisponibilidad { PaqueteId = paquete, Fecha = new(2026, 11, 20) }, default);
        Assert.Equal(19, resultado.Horarios.Count);
        Assert.Equal(0, resultado.Horarios.Single(h => h.Hora == new TimeOnly(12, 0)).LineasLibres);
        Assert.Equal(15, resultado.Horarios.Single(h => h.Hora == new TimeOnly(12, 15)).LineasLibres);
        Assert.Equal(auditoria, await db.EventosAuditoria.CountAsync());
    }

    [PostgresFact]
    public async Task EdicionDeBloqueosConcurrenteNoMezclaInstantaneasEnUnaConsulta()
    {
        var (paquete, autor) = await Preparar();
        await using var baseDatos = fixture.CrearContext();
        var inicio = new DateTime(2026, 11, 20, 16, 30, 0, DateTimeKind.Utc);
        var interceptor = new InsertarBloqueoDuranteLectura(async () =>
        {
            await using var otro = fixture.CrearContext();
            otro.BloqueosOperacion.Add(new BloqueoOperacion { Id = Guid.NewGuid(), InicioUtc = inicio, FinUtc = inicio.AddMinutes(15),
                Motivo = "Bloqueo concurrente", CreadoPorId = autor, CreadoEnUtc = reloj.Ahora.UtcDateTime,
                ActualizadoEnUtc = reloj.Ahora.UtcDateTime, Version = Guid.NewGuid() });
            await otro.SaveChangesAsync();
        });
        var opciones = new DbContextOptionsBuilder<PcpShootingDbContext>().UseNpgsql(baseDatos.Database.GetConnectionString())
            .AddInterceptors(interceptor).Options;
        await using var db = new PcpShootingDbContext(opciones);
        var filtro = new ConsultaDisponibilidad { PaqueteId = paquete, Fecha = new(2026, 11, 20), Hora = new TimeOnly(11, 30) };
        var anterior = await Servicio(db).ConsultarAsync(filtro, default);
        Assert.True(interceptor.Ejecutado);
        Assert.Equal(15, Assert.Single(anterior.Horarios).LineasLibres);
        var actual = await Servicio(db).ConsultarAsync(filtro, default);
        Assert.Equal(0, Assert.Single(actual.Horarios).LineasLibres);
    }

    private sealed class InsertarBloqueoDuranteLectura(Func<Task> insertar) : DbCommandInterceptor
    {
        public bool Ejecutado { get; private set; }
        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
            DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (!Ejecutado && command.CommandText.Contains("FROM \"Paquetes\""))
            {
                Ejecutado = true;
                await insertar();
            }
            return result;
        }
    }
}
