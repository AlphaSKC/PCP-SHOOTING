using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Services;

public sealed class AvisosPublicosService(PcpShootingDbContext db, TimeProvider reloj) : IAvisosPublicosService
{
    public Task<PaginaConsulta<AvisoPublicoDto>> ListarPublicosAsync(FiltroPagina filtro, CancellationToken ct)
    {
        var ahora = reloj.GetUtcNow().UtcDateTime;
        return PaginacionConsulta.CrearAsync(Ordenar(db.Set<AvisoPublico>().AsNoTracking()
            .Where(a => a.Activo && (a.InicioUtc == null || a.InicioUtc <= ahora) && (a.FinUtc == null || ahora < a.FinUtc)))
            .Select(a => new AvisoPublicoDto(a.Id, a.Titulo, a.Texto, a.Orden, a.InicioUtc, a.FinUtc)), filtro, ct);
    }

    public async Task<PaginaConsulta<AvisoPublicoGestion>> ListarGestionAsync(FiltroAvisosPublicos filtro, CancellationToken ct)
    {
        var ahora = reloj.GetUtcNow().UtcDateTime;
        var q = db.Set<AvisoPublico>().AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(filtro.Busqueda))
        {
            var texto = filtro.Busqueda.Trim().ToLower();
            q = q.Where(a => a.Texto.ToLower().Contains(texto) || (a.Titulo != null && a.Titulo.ToLower().Contains(texto)));
        }
        q = filtro.Estado switch {
            EstadoAvisoPublico.Inactivo => q.Where(a => !a.Activo),
            EstadoAvisoPublico.Programado => q.Where(a => a.Activo && a.InicioUtc > ahora),
            EstadoAvisoPublico.Vencido => q.Where(a => a.Activo && a.FinUtc <= ahora),
            EstadoAvisoPublico.Vigente => q.Where(a => a.Activo && (a.InicioUtc == null || a.InicioUtc <= ahora) && (a.FinUtc == null || ahora < a.FinUtc)),
            _ => q
        };
        var pagina = await PaginacionConsulta.CrearAsync(Ordenar(q), filtro, ct);
        return new(pagina.Elementos.Select(a => Gestion(a, ahora)).ToArray(), pagina.Total, pagina.Pagina, pagina.TamanoPagina);
    }

    public async Task<AvisoPublicoGestion> ObtenerAsync(Guid id, CancellationToken ct) =>
        Gestion(await Buscar(id, ct), reloj.GetUtcNow().UtcDateTime);

    public async Task<AvisoPublicoGestion> GuardarAsync(Guid? id, GuardarAvisoPublico datos, CancellationToken ct)
    {
        Validator.ValidateObject(datos, new ValidationContext(datos), true);
        if (id.HasValue && (datos.Version is null || datos.Version == Guid.Empty))
            throw new ValidationException("Envía la versión actual del aviso.");
        var ahora = reloj.GetUtcNow().UtcDateTime;
        var aviso = id.HasValue ? await Buscar(id.Value, ct) : new AvisoPublico { Id = Guid.NewGuid(), CreadoEnUtc = ahora };
        if (id.HasValue && aviso.Version != datos.Version) throw Conflicto();
        aviso.Titulo = string.IsNullOrWhiteSpace(datos.Titulo) ? null : datos.Titulo.Trim();
        aviso.Texto = datos.Texto.Trim(); aviso.Activo = datos.Activo; aviso.Orden = datos.Orden;
        aviso.InicioUtc = datos.InicioUtc?.UtcDateTime; aviso.FinUtc = datos.FinUtc?.UtcDateTime;
        aviso.Version = Guid.NewGuid(); aviso.ActualizadoEnUtc = ahora;
        if (!id.HasValue) db.Add(aviso);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); throw Conflicto(); }
        return Gestion(aviso, ahora);
    }

    private Task<AvisoPublico?> BuscarOpcional(Guid id, CancellationToken ct) => db.Set<AvisoPublico>().SingleOrDefaultAsync(a => a.Id == id, ct);
    private async Task<AvisoPublico> Buscar(Guid id, CancellationToken ct) => await BuscarOpcional(id, ct) ?? throw new OperacionException(404, "Aviso inexistente.");
    private static OperacionException Conflicto() => new(409, "El aviso cambió. Vuelve a consultarlo.");
    private static IOrderedQueryable<AvisoPublico> Ordenar(IQueryable<AvisoPublico> q) => q.OrderBy(a => a.Orden).ThenByDescending(a => a.CreadoEnUtc).ThenBy(a => a.Id);
    private static AvisoPublicoGestion Gestion(AvisoPublico a, DateTime ahora) => new(a.Id, a.Titulo, a.Texto, a.Activo, a.Orden,
        a.InicioUtc, a.FinUtc, !a.Activo ? EstadoAvisoPublico.Inactivo : a.InicioUtc > ahora ? EstadoAvisoPublico.Programado :
        a.FinUtc <= ahora ? EstadoAvisoPublico.Vencido : EstadoAvisoPublico.Vigente, a.Version, a.CreadoEnUtc, a.ActualizadoEnUtc);
}
