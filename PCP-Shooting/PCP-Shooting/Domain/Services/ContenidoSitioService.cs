using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Services;

public sealed class ContenidoSitioService(PcpShootingDbContext db, TimeProvider reloj) : IContenidoSitioService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public async Task<ContenidoSitioConsulta> ConsultarAsync(CancellationToken ct) => Consulta(await db.Set<ConfiguracionContenidoSitio>().AsNoTracking().SingleAsync(c => c.Id == ConfiguracionContenidoSitio.IdUnico, ct));
    public async Task<ContenidoSitioConsulta> GuardarAsync(GuardarContenidoSitio datos, CancellationToken ct)
    {
        Validator.ValidateObject(datos, new ValidationContext(datos), true);
        Validator.ValidateObject(datos.Contenido, new ValidationContext(datos.Contenido), true);
        if (datos.Version == Guid.Empty) throw new ValidationException("La versión actual es obligatoria.");
        var config = await db.Set<ConfiguracionContenidoSitio>().SingleAsync(c => c.Id == ConfiguracionContenidoSitio.IdUnico, ct);
        if (config.Version != datos.Version) throw new OperacionException(409, "El contenido cambió. Recarga y revisa antes de guardar.");
        // Solo valores tipados de contenido público; no admite claves de configuración ni HTML ejecutable.
        config.DatosJson = JsonSerializer.Serialize(datos.Contenido, Json);
        config.Version = Guid.NewGuid(); config.ActualizadoEnUtc = reloj.GetUtcNow().UtcDateTime;
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); throw new OperacionException(409, "El contenido cambió. Recarga y revisa antes de guardar."); }
        return Consulta(config);
    }
    private static ContenidoSitioConsulta Consulta(ConfiguracionContenidoSitio c) => new(JsonSerializer.Deserialize<ContenidoSitioDatos>(c.DatosJson, Json)!, c.Version, c.ActualizadoEnUtc);
}
