using Microsoft.Extensions.Options;
using PCP_Shooting.Domain.Interfaces;

namespace PCP_Shooting.Domain.Services;

public sealed class ConciliacionProgramadaWorker(IServiceScopeFactory scopes, IOptions<ConciliacionProgramadaOptions> options,
    IOptions<MercadoPagoOptions> mercado, TimeProvider reloj, ILogger<ConciliacionProgramadaWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        if (!options.Value.Habilitada) return;
        if (string.IsNullOrWhiteSpace(mercado.Value.AccessToken))
        { log.LogWarning("Conciliación programada sin credenciales; configura Mercado Pago y reinicia la API."); return; }
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var resultado = await scope.ServiceProvider.GetRequiredService<IConciliacionProgramadaService>().EjecutarCicloAsync(stoppingToken);
                log.LogInformation("Conciliación programada: {Revisadas} revisadas, {Fallidas} fallidas, {Paginas} páginas.", resultado.Revisadas, resultado.Fallidas, resultado.Paginas);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { log.LogWarning("No se pudo completar el ciclo de conciliación ({TipoError}); se reintentará.", ex.GetType().Name); }
            try { await Task.Delay(TimeSpan.FromSeconds(options.Value.IntervaloSegundos), reloj, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
