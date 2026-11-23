using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Services;

public sealed class MarcasService(PcpShootingDbContext db, TimeProvider reloj) : IMarcasService
{
    public async Task<IReadOnlyList<MarcaPublica>> ListarPublicasAsync(CancellationToken ct) => await db.MarcasAliadas.AsNoTracking().Where(m => m.Activo)
        .OrderBy(m => m.Orden).ThenBy(m => m.Nombre).ThenBy(m => m.Id)
        .Select(m => new MarcaPublica(m.Id, m.Nombre, m.Descripcion, m.Url, m.LogoUrl, m.Orden)).ToArrayAsync(ct);
    public async Task<IReadOnlyList<MarcaGestion>> ListarGestionAsync(CancellationToken ct) => await db.MarcasAliadas.AsNoTracking()
        .OrderBy(m => m.Orden).ThenBy(m => m.Nombre).ThenBy(m => m.Id)
        .Select(m => new MarcaGestion(m.Id, m.Nombre, m.Descripcion, m.Url, m.LogoUrl, m.Orden, m.Activo, m.Version)).ToArrayAsync(ct);
    public async Task<MarcaGestion> GuardarAsync(Guid? id, GuardarMarca datos, CancellationToken ct)
    {
        Validator.ValidateObject(datos, new ValidationContext(datos), true);
        var ahora = reloj.GetUtcNow().UtcDateTime;
        var marca = id.HasValue ? await Buscar(id.Value, ct) : new MarcaAliada { Id = Guid.NewGuid(), Nombre = "", CreadoEnUtc = ahora };
        if (id.HasValue && (datos.Version is null || datos.Version == Guid.Empty)) throw new ValidationException("Envía la versión actual de la marca.");
        if (id.HasValue && marca.Version != datos.Version) throw new OperacionException(409, "La marca cambió. Vuelve a consultarla.");
        marca.Nombre = datos.Nombre.Trim(); marca.Descripcion = string.IsNullOrWhiteSpace(datos.Descripcion) ? null : datos.Descripcion.Trim();
        marca.Url = string.IsNullOrWhiteSpace(datos.Url) ? null : datos.Url.Trim(); marca.Activo = datos.Activo; marca.Orden = datos.Orden;
        marca.Version = Guid.NewGuid(); marca.ActualizadoEnUtc = ahora;
        if (!id.HasValue) db.MarcasAliadas.Add(marca);
        await Guardar(ct);
        return new(marca.Id, marca.Nombre, marca.Descripcion, marca.Url, marca.LogoUrl, marca.Orden, marca.Activo, marca.Version);
    }
    public async Task QuitarLogoAsync(Guid id, CancellationToken ct)
    {
        var marca = await Buscar(id, ct); marca.LogoUrl = null; marca.Version = Guid.NewGuid(); marca.ActualizadoEnUtc = reloj.GetUtcNow().UtcDateTime;
        await Guardar(ct);
    }
    private async Task<MarcaAliada> Buscar(Guid id, CancellationToken ct) => await db.MarcasAliadas.SingleOrDefaultAsync(m => m.Id == id, ct) ?? throw new OperacionException(404, "Marca inexistente.");
    private async Task Guardar(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); throw new OperacionException(409, "La marca cambió. Vuelve a consultarla."); }
    }
}
