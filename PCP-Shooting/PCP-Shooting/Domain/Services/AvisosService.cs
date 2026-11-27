using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Services;

public sealed class AvisosService(PcpShootingDbContext db, TimeProvider reloj) : IAvisosService
{
    public Task<PlantillasWhatsAppConsulta> ConsultarWhatsAppAsync(CancellationToken ct) => db.ConfiguracionAvisos.AsNoTracking()
        .Where(c => c.Id == ConfiguracionAvisos.IdUnico)
        .Select(c => new PlantillasWhatsAppConsulta(c.WaOrderReady, c.WaReminder)).SingleAsync(ct);

    public async Task<AvisosConsulta> ConsultarAsync(CancellationToken ct) => Consulta(
        await db.ConfiguracionAvisos.AsNoTracking().SingleAsync(c => c.Id == ConfiguracionAvisos.IdUnico, ct));

    public async Task<AvisosConsulta> GuardarAsync(GuardarAvisos datos, CancellationToken ct)
    {
        Validator.ValidateObject(datos, new ValidationContext(datos), true);
        var config = await db.ConfiguracionAvisos.SingleAsync(c => c.Id == ConfiguracionAvisos.IdUnico, ct);
        if (config.Version != datos.Version) throw new OperacionException(409, "Las plantillas cambiaron. Consulta la versión actual antes de guardar.");
        config.NotifyEmail = string.IsNullOrWhiteSpace(datos.NotifyEmail) ? null : datos.NotifyEmail.Trim();
        config.WaOrderReady = datos.WaOrderReady;
        config.WaReminder = datos.WaReminder;
        config.Version = Guid.NewGuid();
        config.ActualizadoEnUtc = reloj.GetUtcNow().UtcDateTime;
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            throw new OperacionException(409, "Las plantillas cambiaron. Consulta la versión actual antes de guardar.");
        }
        return Consulta(config);
    }
    private static AvisosConsulta Consulta(ConfiguracionAvisos c) => new(c.NotifyEmail, c.WaOrderReady, c.WaReminder, c.Version, c.ActualizadoEnUtc);
}
