using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Services;

public sealed class BlogService(PcpShootingDbContext db, TimeProvider reloj) : IBlogService
{
    private IQueryable<ArticuloBlog> Publicos(DateTime ahora) => db.ArticulosBlog.AsNoTracking().Where(a =>
        (a.Estado == EstadoArticuloBlog.Publicado || a.Estado == EstadoArticuloBlog.Programado) && a.PublicarEnUtc <= ahora);

    public Task<PaginaConsulta<ArticuloBlogResumen>> ListarPublicosAsync(FiltroBlog filtro, CancellationToken ct)
    {
        Validar(filtro);
        return PaginacionConsulta.CrearAsync(Filtrar(Publicos(reloj.GetUtcNow().UtcDateTime), filtro)
            .OrderByDescending(a => a.PublicarEnUtc).ThenBy(a => a.Id)
            .Select(a => new ArticuloBlogResumen(a.Id, a.Slug, a.Titulo, a.Resumen, a.Categoria, a.ImagenUrl, a.Destacado, a.PublicarEnUtc)), filtro, ct);
    }

    public Task<ArticuloBlogDetalle?> ConsultarPublicoAsync(string slug, CancellationToken ct) =>
        Publicos(reloj.GetUtcNow().UtcDateTime).Where(a => a.Slug == slug)
            .Select(a => new ArticuloBlogDetalle(a.Id, a.Slug, a.Titulo, a.Resumen, a.ContenidoMarkdown,
                a.Categoria, a.ImagenUrl, a.Destacado, a.PublicarEnUtc)).SingleOrDefaultAsync(ct);

    public Task<PaginaConsulta<ArticuloBlogGestion>> ListarGestionAsync(FiltroBlogGestion filtro, CancellationToken ct)
    {
        Validar(filtro);
        var ahora = reloj.GetUtcNow().UtcDateTime;
        var consulta = Filtrar(db.ArticulosBlog.AsNoTracking(), filtro);
        if (filtro.Estado == EstadoArticuloBlog.Publicado) consulta = consulta.Where(a =>
            (a.Estado == EstadoArticuloBlog.Publicado || a.Estado == EstadoArticuloBlog.Programado) && a.PublicarEnUtc <= ahora);
        else if (filtro.Estado == EstadoArticuloBlog.Programado) consulta = consulta.Where(a => a.Estado == EstadoArticuloBlog.Programado && a.PublicarEnUtc > ahora);
        else if (filtro.Estado.HasValue) consulta = consulta.Where(a => a.Estado == filtro.Estado);
        return PaginacionConsulta.CrearAsync(consulta.OrderByDescending(a => a.ActualizadoEnUtc).ThenBy(a => a.Id)
            .Select(Proyectar(ahora)), filtro, ct);
    }

    public Task<ArticuloBlogGestion?> ConsultarGestionAsync(Guid id, CancellationToken ct) =>
        db.ArticulosBlog.AsNoTracking().Where(a => a.Id == id).Select(Proyectar(reloj.GetUtcNow().UtcDateTime)).SingleOrDefaultAsync(ct);

    public async Task<ArticuloBlogGestion> GuardarAsync(Guid? id, Guid actorId, GuardarArticuloBlog datos, CancellationToken ct)
    {
        Validar(datos);
        if (string.IsNullOrWhiteSpace(datos.Titulo) || string.IsNullOrWhiteSpace(datos.Resumen) || string.IsNullOrWhiteSpace(datos.ContenidoMarkdown))
            throw new ValidationException("Título, resumen y contenido son obligatorios.");
        var imagenUrl = string.IsNullOrWhiteSpace(datos.ImagenUrl) ? null : datos.ImagenUrl.Trim();
        if (imagenUrl is not null && (!Uri.TryCreate(imagenUrl, UriKind.Absolute, out var imagen) || imagen.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(imagen.UserInfo)))
            throw new ValidationException("La portada requiere una URL HTTPS válida sin credenciales.");
        var ahora = reloj.GetUtcNow().UtcDateTime;
        var articulo = id.HasValue ? await db.ArticulosBlog.SingleOrDefaultAsync(a => a.Id == id, ct)
            : new ArticuloBlog { Id = Guid.NewGuid(), AutorId = actorId, CreadoEnUtc = ahora,
                Slug = datos.Slug, Titulo = "", Resumen = "", ContenidoMarkdown = "" };
        if (articulo is null) throw new OperacionException(404, "Artículo inexistente.");
        if (id.HasValue && (datos.Version is null || datos.Version == Guid.Empty)) throw new ValidationException("Envía la versión actual del artículo para editarlo.");
        if (id.HasValue && datos.Version != articulo.Version) throw new OperacionException(409, "El artículo cambió. Vuelve a consultarlo antes de editarlo.");
        if (await db.ArticulosBlog.AnyAsync(a => a.Slug == datos.Slug && a.Id != articulo.Id, ct))
            throw new OperacionException(409, "El slug ya está registrado.");
        var fecha = datos.PublicarEnUtc?.UtcDateTime;
        if (datos.Estado == EstadoArticuloBlog.Programado && (!fecha.HasValue || fecha <= ahora))
            throw new ValidationException("Una publicación programada requiere una fecha futura.");
        if (datos.Estado == EstadoArticuloBlog.Publicado)
        {
            fecha ??= articulo.PublicarEnUtc is DateTime anterior && anterior <= ahora ? anterior : ahora;
            if (fecha > ahora) throw new ValidationException("Usa Programado para publicar en una fecha futura.");
        }
        if (datos.Estado == EstadoArticuloBlog.Borrador) fecha = null;
        if (datos.Estado == EstadoArticuloBlog.Archivado) fecha ??= articulo.PublicarEnUtc;
        articulo.Slug = datos.Slug;
        articulo.Titulo = datos.Titulo.Trim();
        articulo.Resumen = datos.Resumen.Trim();
        articulo.ContenidoMarkdown = datos.ContenidoMarkdown.Trim();
        articulo.Categoria = datos.Categoria;
        articulo.ImagenUrl = imagenUrl;
        articulo.Destacado = datos.Destacado;
        articulo.Estado = datos.Estado;
        articulo.PublicarEnUtc = fecha;
        articulo.ActualizadoEnUtc = ahora;
        articulo.Version = Guid.NewGuid();
        if (!id.HasValue) db.ArticulosBlog.Add(articulo);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new OperacionException(409, "El artículo cambió. Vuelve a consultarlo antes de editarlo."); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { throw new OperacionException(409, "El slug ya está registrado."); }
        return (await ConsultarGestionAsync(articulo.Id, ct))!;
    }

    private static IQueryable<ArticuloBlog> Filtrar(IQueryable<ArticuloBlog> consulta, FiltroBlog filtro)
    {
        if (filtro.Categoria.HasValue) consulta = consulta.Where(a => a.Categoria == filtro.Categoria);
        if (filtro.Destacado.HasValue) consulta = consulta.Where(a => a.Destacado == filtro.Destacado);
        var busqueda = filtro.Busqueda?.Trim().ToUpperInvariant();
        if (!string.IsNullOrEmpty(busqueda)) consulta = consulta.Where(a => a.Titulo.ToUpper().Contains(busqueda) || a.Resumen.ToUpper().Contains(busqueda));
        return consulta;
    }

    private static Expression<Func<ArticuloBlog, ArticuloBlogGestion>> Proyectar(DateTime ahora) => a => new(
        a.Id, a.Slug, a.Titulo, a.Resumen, a.ContenidoMarkdown, a.Categoria, a.ImagenUrl, a.Destacado,
        a.Estado == EstadoArticuloBlog.Programado && a.PublicarEnUtc <= ahora ? EstadoArticuloBlog.Publicado : a.Estado,
        a.PublicarEnUtc, a.CreadoEnUtc, a.ActualizadoEnUtc, a.AutorId, a.Version);

    private static void Validar(object datos) => Validator.ValidateObject(datos, new ValidationContext(datos), true);
}
