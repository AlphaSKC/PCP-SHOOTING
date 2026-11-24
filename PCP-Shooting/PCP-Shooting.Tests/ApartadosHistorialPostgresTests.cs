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

public sealed class ApartadosHistorialPostgresTests(PostgresEscriturasFixture fixture) : IClassFixture<PostgresEscriturasFixture>
{
    [PostgresFact]
    public async Task HistorialMantieneUnCorteUnicoSiElRelojCruzaElVencimientoDuranteLaLectura()
    {
        var reloj = new RelojDisponibilidad(DateTimeOffset.Parse("2026-11-20T15:00:00Z"));
        await using var inicial = fixture.CrearContext();
        var usuario = Guid.NewGuid();
        var paquete = Guid.NewGuid();
        inicial.PerfilesUsuario.Add(new PerfilUsuario { Id = usuario, Nombre = "Cliente" });
        inicial.Paquetes.Add(new Paquete { Id = paquete, Codigo = "HIST-" + paquete.ToString("N").ToUpperInvariant(), Nombre = "Actividad",
            IncluyeMarkdown = "Actividad", Activo = true, PrecioPorPersona = 100m, Version = Guid.NewGuid(),
            CreadoEnUtc = reloj.Ahora.UtcDateTime, ActualizadoEnUtc = reloj.Ahora.UtcDateTime });
        await inicial.SaveChangesAsync();
        var linea = await inicial.LineasTiro.OrderBy(l => l.Numero).Select(l => l.Id).FirstAsync();
        ApartadosService Servicio(PcpShootingDbContext db)
        {
            var operacion = new OperacionService(db, reloj);
            var transacciones = new TransaccionesPersistence(db);
            return new ApartadosService(db, new DisponibilidadService(db, operacion, new PaquetesService(db, reloj, transacciones), reloj), operacion, transacciones, reloj);
        }
        var original = await Servicio(inicial).CrearAsync(usuario, new CrearApartado { IdOperacion = Guid.NewGuid(), PaqueteId = paquete,
            Fecha = new(2026, 11, 20), Hora = new(13, 0), Lineas = new() { linea } }, default);
        reloj.Ahora = new DateTimeOffset(original.VenceEnUtc).AddSeconds(-1);
        var corte = reloj.Ahora.UtcDateTime;
        var interceptor = new CruzarVencimiento(() => reloj.Ahora = new DateTimeOffset(original.VenceEnUtc).AddSeconds(1));
        var opciones = new DbContextOptionsBuilder<PcpShootingDbContext>().UseNpgsql(inicial.Database.GetConnectionString())
            .AddInterceptors(interceptor).Options;
        var auditoria = await inicial.EventosAuditoria.CountAsync();
        await using var consulta = new PcpShootingDbContext(opciones);
        var lista = await Servicio(consulta).ListarAsync(usuario, new FiltroApartados { Estado = EstadoApartado.Vigente }, default);
        Assert.True(interceptor.Ejecutado);
        Assert.Equal(corte, lista.ConsultadoEnUtc);
        Assert.Equal(1, lista.Total);
        Assert.Equal(EstadoApartado.Vigente, Assert.Single(lista.Elementos).Estado);
        await using var reinicio = fixture.CrearContext();
        Assert.Empty((await Servicio(reinicio).ListarAsync(usuario, new FiltroApartados { Estado = EstadoApartado.Vigente }, default)).Elementos);
        var vencido = await Servicio(reinicio).ConsultarAsync(usuario, original.Id, default);
        Assert.Equal(EstadoApartado.Vencido, vencido!.Estado);
        Assert.Equal(original.VenceEnUtc, vencido.VenceEnUtc);
        Assert.Equal(auditoria, await reinicio.EventosAuditoria.CountAsync());
        Assert.Equal(1, await reinicio.Apartados.CountAsync());
        Assert.Equal(1, await reinicio.LineasApartado.CountAsync());
    }

    private sealed class CruzarVencimiento(Action avanzarReloj) : DbCommandInterceptor
    {
        public bool Ejecutado { get; private set; }
        public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
            DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (!Ejecutado && command.CommandText.Contains("count(*)") && command.CommandText.Contains("Apartados"))
            {
                Ejecutado = true;
                avanzarReloj();
            }
            return ValueTask.FromResult(result);
        }
    }
}
