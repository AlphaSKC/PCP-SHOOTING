using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Domain.Services;
using PCP_Shooting.Persistence;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class OperacionConsistenciaPostgresTests(PostgresEscriturasFixture fixture) : IClassFixture<PostgresEscriturasFixture>
{
    [PostgresFact]
    public async Task UnaLecturaNoMezclaConfiguracionAnteriorConHorariosDeUnaEdicionConcurrente()
    {
        await using var baseDatos = fixture.CrearContext();
        var original = await new OperacionService(baseDatos, TimeProvider.System).ConsultarConfiguracionAsync(default);
        var interceptor = new EditarDuranteLectura(async () =>
        {
            await using var otro = fixture.CrearContext();
            await new OperacionService(otro, TimeProvider.System).GuardarConfiguracionAsync(new GuardarConfiguracionOperativa
            {
                Version = original.Version, DuracionActividadMinutos = 25, MargenMinutos = 5,
                ApartadoPagoMinutos = original.ApartadoPagoMinutos, AnticipacionMinimaMinutos = original.AnticipacionMinimaMinutos,
                AnticipacionMaximaDias = original.AnticipacionMaximaDias, LlegadaAnticipadaMinutos = original.LlegadaAnticipadaMinutos,
                ToleranciaLlegadaMinutos = original.ToleranciaLlegadaMinutos,
                Horarios = original.Horarios.Select(h => new HorarioOperacionDatos
                {
                    DiaSemana = h.DiaSemana, Cerrado = h.Cerrado, Apertura = h.Apertura, Cierre = h.Cierre,
                    PrimerTurno = h.PrimerTurno, UltimoTurno = new TimeOnly(15, 0)
                }).ToList()
            }, default);
        });
        var opciones = new DbContextOptionsBuilder<PcpShootingDbContext>().UseNpgsql(baseDatos.Database.GetConnectionString())
            .AddInterceptors(interceptor).Options;
        await using var consulta = new PcpShootingDbContext(opciones);
        var resultado = await new OperacionService(consulta, TimeProvider.System).ConsultarConfiguracionAsync(default);
        Assert.True(interceptor.Ejecutado);
        Assert.Equal(original.Version, resultado.Version);
        Assert.Equal(original.DuracionActividadMinutos, resultado.DuracionActividadMinutos);
        Assert.All(resultado.Horarios, h => Assert.Equal(new TimeOnly(15, 30), h.UltimoTurno));
        await using var comprobacion = fixture.CrearContext();
        var actual = await new OperacionService(comprobacion, TimeProvider.System).ConsultarConfiguracionAsync(default);
        Assert.Equal(25, actual.DuracionActividadMinutos);
        Assert.All(actual.Horarios, h => Assert.Equal(new TimeOnly(15, 0), h.UltimoTurno));
    }

    private sealed class EditarDuranteLectura(Func<Task> editar) : DbCommandInterceptor
    {
        public bool Ejecutado { get; private set; }
        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
            DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (!Ejecutado && command.CommandText.Contains("ConfiguracionOperativa"))
            {
                Ejecutado = true;
                await editar();
            }
            return result;
        }
    }
}
