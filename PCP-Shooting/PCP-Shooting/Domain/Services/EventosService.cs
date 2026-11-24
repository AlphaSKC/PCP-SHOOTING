using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Services;

public sealed class EventosService(PcpShootingDbContext db, TimeProvider reloj) : IEventosService
{
    private IQueryable<Evento> Publicos() => db.Set<Evento>().AsNoTracking().Where(e => e.Estado == EstadoEvento.Publicado);

    public Task<PaginaConsulta<EventoResumen>> ListarPublicosAsync(FiltroEventos filtro, CancellationToken ct)
    {
        Validar(filtro);
        return PaginacionConsulta.CrearAsync(Ordenar(Filtrar(Publicos(), filtro))
            .Select(e => new EventoResumen(e.Id, e.Slug, e.Titulo, e.Resumen, e.Ubicacion, e.ImagenUrl, e.InicioUtc, e.FinUtc)), filtro, ct);
    }

    public Task<EventoDetalle?> ConsultarPublicoAsync(string slug, CancellationToken ct) => Publicos().Where(e => e.Slug == slug)
        .Select(e => new EventoDetalle(e.Id, e.Slug, e.Titulo, e.Resumen, e.DescripcionMarkdown, e.Ubicacion, e.ImagenUrl, e.InicioUtc, e.FinUtc))
        .SingleOrDefaultAsync(ct);

    public Task<PaginaConsulta<EventoGestion>> ListarGestionAsync(FiltroEventosGestion filtro, CancellationToken ct)
    {
        Validar(filtro);
        var q = Filtrar(db.Set<Evento>().AsNoTracking(), filtro);
        if (filtro.Estado.HasValue) q = q.Where(e => e.Estado == filtro.Estado);
        return PaginacionConsulta.CrearAsync(Ordenar(q).Select(Proyectar), filtro, ct);
    }

    public Task<EventoGestion?> ConsultarGestionAsync(Guid id, CancellationToken ct) =>
        db.Set<Evento>().AsNoTracking().Where(e => e.Id == id).Select(Proyectar).SingleOrDefaultAsync(ct);

    public async Task<EventoGestion> GuardarAsync(Guid? id, GuardarEvento datos, CancellationToken ct)
    {
        Validar(datos);
        if (id.HasValue && (datos.Version is null || datos.Version == Guid.Empty)) throw new ValidationException("Envía la versión actual del evento.");
        var ahora = reloj.GetUtcNow().UtcDateTime;
        var evento = id.HasValue ? await db.Set<Evento>().SingleOrDefaultAsync(e => e.Id == id, ct)
            : new Evento { Id = Guid.NewGuid(), CreadoEnUtc = ahora };
        if (evento is null) throw new OperacionException(404, "Evento inexistente.");
        if (id.HasValue && evento.Version != datos.Version) throw Conflicto();
        if (await db.Set<Evento>().AnyAsync(e => e.Slug == datos.Slug && e.Id != evento.Id, ct))
            throw new OperacionException(409, "El slug ya está registrado.");
        evento.Slug = datos.Slug; evento.Titulo = datos.Titulo.Trim(); evento.Resumen = datos.Resumen.Trim();
        evento.DescripcionMarkdown = datos.DescripcionMarkdown.Trim(); evento.Ubicacion = Opcional(datos.Ubicacion); evento.ImagenUrl = Opcional(datos.ImagenUrl);
        evento.InicioUtc = datos.InicioUtc.UtcDateTime; evento.FinUtc = datos.FinUtc.UtcDateTime; evento.Estado = datos.Estado;
        evento.Version = Guid.NewGuid(); evento.ActualizadoEnUtc = ahora;
        if (!id.HasValue) db.Add(evento);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); throw Conflicto(); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_Eventos_Slug" })
        { db.ChangeTracker.Clear(); throw new OperacionException(409, "El slug ya está registrado."); }
        return (await ConsultarGestionAsync(evento.Id, ct))!;
    }

    private IQueryable<Evento> Filtrar(IQueryable<Evento> q, FiltroEventos filtro)
    {
        if (filtro.DesdeUtc.HasValue) { var desde = filtro.DesdeUtc.Value.UtcDateTime; q = q.Where(e => e.InicioUtc >= desde); }
        if (filtro.HastaUtc.HasValue) { var hasta = filtro.HastaUtc.Value.UtcDateTime; q = q.Where(e => e.InicioUtc < hasta); }
        if (filtro.Proximos.HasValue)
        {
            var ahora = reloj.GetUtcNow().UtcDateTime;
            q = filtro.Proximos.Value ? q.Where(e => e.FinUtc > ahora) : q.Where(e => e.FinUtc <= ahora);
        }
        if (!string.IsNullOrWhiteSpace(filtro.Busqueda))
        {
            var texto = filtro.Busqueda.Trim().ToUpperInvariant();
            q = q.Where(e => e.Titulo.ToUpper().Contains(texto) || e.Resumen.ToUpper().Contains(texto));
        }
        return q;
    }

    private static IOrderedQueryable<Evento> Ordenar(IQueryable<Evento> q) => q.OrderBy(e => e.InicioUtc).ThenBy(e => e.Id);
    private static readonly Expression<Func<Evento, EventoGestion>> Proyectar = e => new(e.Id, e.Slug, e.Titulo, e.Resumen,
        e.DescripcionMarkdown, e.Ubicacion, e.ImagenUrl, e.InicioUtc, e.FinUtc, e.Estado, e.Version, e.CreadoEnUtc, e.ActualizadoEnUtc);
    private static string? Opcional(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    private static OperacionException Conflicto() => new(409, "El evento cambió. Vuelve a consultarlo antes de editarlo.");
    private static void Validar(object datos) => Validator.ValidateObject(datos, new ValidationContext(datos), true);
}
