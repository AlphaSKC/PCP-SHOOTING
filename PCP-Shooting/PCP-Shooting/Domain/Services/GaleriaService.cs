using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Services;

public sealed class GaleriaService(PcpShootingDbContext db, TimeProvider reloj) : IGaleriaService
{
    public Task<PaginaConsulta<AlbumGaleriaPublico>> AlbumsPublicosAsync(FiltroPagina filtro, CancellationToken ct)
    {
        Validar(filtro);
        return PaginacionConsulta.CrearAsync(db.Set<AlbumGaleria>().AsNoTracking()
            .Where(a => a.Publicado && a.Fotos.Any(f => f.Publicada && f.ImagenUrl != null))
            .OrderBy(a => a.Orden).ThenBy(a => a.Id).Select(a => new AlbumGaleriaPublico(a.Id, a.Nombre, a.Descripcion, a.Orden,
                a.Fotos.Where(f => f.Publicada && f.ImagenUrl != null).OrderBy(f => f.Orden).ThenBy(f => f.Id).Select(f => f.ImagenUrl).FirstOrDefault(),
                a.Fotos.Count(f => f.Publicada && f.ImagenUrl != null))), filtro, ct);
    }
    public Task<PaginaConsulta<FotoGaleriaPublica>> FotosPublicasAsync(FiltroFotosGaleria filtro, CancellationToken ct)
    {
        Validar(filtro);
        var fotos = db.Set<FotoGaleria>().AsNoTracking().Where(f => f.Publicada && f.ImagenUrl != null && f.Album.Publicado);
        if (filtro.AlbumId.HasValue) fotos = fotos.Where(f => f.AlbumId == filtro.AlbumId);
        return PaginacionConsulta.CrearAsync(fotos.OrderBy(f => f.Orden).ThenBy(f => f.Id)
            .Select(f => new FotoGaleriaPublica(f.Id, f.AlbumId, f.Titulo, f.TextoAlternativo, f.Descripcion, f.Orden, f.ImagenUrl!)), filtro, ct);
    }
    public Task<PaginaConsulta<AlbumGaleriaGestion>> AlbumsGestionAsync(FiltroPagina filtro, CancellationToken ct)
    {
        Validar(filtro);
        return PaginacionConsulta.CrearAsync(db.Set<AlbumGaleria>().AsNoTracking().OrderBy(a => a.Orden).ThenBy(a => a.Id)
            .Select(a => new AlbumGaleriaGestion(a.Id, a.Nombre, a.Descripcion, a.Orden, a.Publicado, a.Version)), filtro, ct);
    }
    public async Task<PaginaConsulta<FotoGaleriaGestion>> FotosGestionAsync(Guid albumId, FiltroPagina filtro, CancellationToken ct)
    {
        Validar(filtro);
        if (!await db.Set<AlbumGaleria>().AnyAsync(a => a.Id == albumId, ct)) throw new OperacionException(404, "Álbum inexistente.");
        return await PaginacionConsulta.CrearAsync(db.Set<FotoGaleria>().AsNoTracking().Where(f => f.AlbumId == albumId)
            .OrderBy(f => f.Orden).ThenBy(f => f.Id).Select(f => new FotoGaleriaGestion(f.Id, f.AlbumId, f.Titulo, f.TextoAlternativo,
                f.Descripcion, f.Orden, f.Publicada, f.ImagenUrl, f.Version)), filtro, ct);
    }
    public async Task<AlbumGaleriaGestion> GuardarAlbumAsync(Guid? id, GuardarAlbumGaleria datos, CancellationToken ct)
    {
        Validar(datos); var ahora = reloj.GetUtcNow().UtcDateTime;
        var a = id.HasValue ? await db.Set<AlbumGaleria>().SingleOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new OperacionException(404, "Álbum inexistente.") : new AlbumGaleria { Id = Guid.NewGuid(), Nombre = "", CreadoEnUtc = ahora };
        if (id.HasValue) Version(a.Version, datos.Version);
        a.Nombre = datos.Nombre.Trim(); a.Descripcion = Texto(datos.Descripcion); a.Orden = datos.Orden; a.Publicado = datos.Publicado;
        a.Version = Guid.NewGuid(); a.ActualizadoEnUtc = ahora;
        if (!id.HasValue) db.Add(a);
        await Guardar(ct);
        return new(a.Id, a.Nombre, a.Descripcion, a.Orden, a.Publicado, a.Version);
    }
    public async Task<FotoGaleriaGestion> GuardarFotoAsync(Guid? id, Guid? albumId, GuardarFotoGaleria datos, CancellationToken ct)
    {
        Validar(datos); var ahora = reloj.GetUtcNow().UtcDateTime;
        FotoGaleria f;
        if (id.HasValue) { f = await db.Set<FotoGaleria>().SingleOrDefaultAsync(f => f.Id == id, ct) ?? throw new OperacionException(404, "Foto inexistente."); Version(f.Version, datos.Version); }
        else
        {
            if (!albumId.HasValue || !await db.Set<AlbumGaleria>().AnyAsync(a => a.Id == albumId, ct)) throw new OperacionException(404, "Álbum inexistente.");
            if (datos.IdOperacion == Guid.Empty) throw new ValidationException("IdOperacion requiere un UUID distinto de cero.");
            var existente = datos.IdOperacion.HasValue ? await db.Set<FotoGaleria>().AsNoTracking().SingleOrDefaultAsync(f => f.Id == datos.IdOperacion, ct) : null;
            if (existente is not null)
            {
                if (existente.AlbumId != albumId || existente.Titulo != datos.Titulo.Trim() || existente.TextoAlternativo != datos.TextoAlternativo.Trim() ||
                    existente.Descripcion != Texto(datos.Descripcion) || existente.Orden != datos.Orden || existente.Publicada != datos.Publicada)
                    throw new OperacionException(409, "La operación corresponde a otra ficha o la foto cambió. Recarga el álbum.");
                return new(existente.Id, existente.AlbumId, existente.Titulo, existente.TextoAlternativo, existente.Descripcion, existente.Orden, existente.Publicada, existente.ImagenUrl, existente.Version);
            }
            f = new FotoGaleria { Id = datos.IdOperacion ?? Guid.NewGuid(), AlbumId = albumId.Value, Titulo = "", TextoAlternativo = "", CreadoEnUtc = ahora };
        }
        if (datos.Publicada && f.ImagenUrl is null) throw new ValidationException("Carga la imagen antes de publicar la foto.");
        f.Titulo = datos.Titulo.Trim(); f.TextoAlternativo = datos.TextoAlternativo.Trim(); f.Descripcion = Texto(datos.Descripcion);
        f.Orden = datos.Orden; f.Publicada = datos.Publicada; f.Version = Guid.NewGuid(); f.ActualizadoEnUtc = ahora;
        if (!id.HasValue) db.Add(f);
        await Guardar(ct);
        return new(f.Id, f.AlbumId, f.Titulo, f.TextoAlternativo, f.Descripcion, f.Orden, f.Publicada, f.ImagenUrl, f.Version);
    }
    private static void Validar(object datos) => Validator.ValidateObject(datos, new ValidationContext(datos), true);
    private static string? Texto(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
    private static void Version(Guid actual, Guid? recibida)
    {
        if (recibida is null || recibida == Guid.Empty) throw new ValidationException("Envía la versión actual.");
        if (actual != recibida) throw new OperacionException(409, "El contenido cambió. Recarga antes de guardar.");
    }
    private async Task Guardar(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); throw new OperacionException(409, "El contenido cambió. Recarga antes de guardar."); }
    }
}
