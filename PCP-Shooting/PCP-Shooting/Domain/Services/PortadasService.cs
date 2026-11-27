using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Models;
namespace PCP_Shooting.Domain.Services;

public sealed class PortadasService(PcpShootingDbContext db, TimeProvider reloj, ITransaccionesPersistence tx, IConfiguration config) : IPortadasService
{
    public async Task<IReadOnlyList<PortadaPublica>> ListarPublicasAsync(CancellationToken ct) => await db.Set<PortadaPagina>().AsNoTracking()
        .Where(p => p.Configurada).OrderBy(p => p.Pagina).Select(p => new PortadaPublica(p.Pagina,p.Activa,p.ImagenUrl,p.Posicion,p.Velo)).ToArrayAsync(ct);
    public async Task<IReadOnlyList<PortadaGestion>> ListarGestionAsync(CancellationToken ct) => await db.Set<PortadaPagina>().AsNoTracking()
        .Where(p => p.Configurada).OrderBy(p => p.Pagina).Select(p => new PortadaGestion(p.Pagina,p.Activa,p.ImagenUrl,p.Posicion,p.Velo,p.Version,p.ActualizadoEnUtc)).ToArrayAsync(ct);
    public async Task<PortadaGestion> GuardarAsync(string pagina, GuardarPortada datos, CancellationToken ct)
    {
        ValidarPagina(pagina); Validator.ValidateObject(datos,new ValidationContext(datos),true);
        var imagen = string.IsNullOrWhiteSpace(datos.ImagenUrl) ? null : datos.ImagenUrl;
        if (datos.Activa && imagen is null) throw new ValidationException("Una portada activa requiere imagen.");
        if (imagen is not null && !ImagenPermitida(imagen)) throw new ValidationException("Usa una imagen del bucket pcp-portadas del proyecto o una ruta /img/ del sitio.");
        var p = await Buscar(pagina,ct);
        if (p is { Configurada: true } && (datos.Version is null || datos.Version != p.Version)) throw Cambio();
        if (p is not { Configurada: true } && datos.Version.HasValue) throw Cambio();
        if (p is null) { p = Nueva(pagina); db.Add(p); }
        p.Configurada = true; p.Activa = datos.Activa; p.ImagenUrl = imagen; p.Posicion = datos.Posicion; p.Velo = datos.Velo;
        p.Version = Guid.NewGuid(); p.ActualizadoEnUtc = reloj.GetUtcNow().UtcDateTime;
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); throw Cambio(); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" }) { db.ChangeTracker.Clear(); throw Cambio(); }
        return Gestion(p);
    }
    public async Task<ImagenPortadaResultado> SubirAsync(string pagina,Guid actorId,Guid idOperacion,Stream archivo,
        string nombre,string tipoContenido,string token,IImagenStorage storage,CancellationToken ct)
    {
        ValidarPagina(pagina);
        if (idOperacion == Guid.Empty) throw new ValidationException("IdOperacion requiere un UUID distinto de cero.");
        var imagen = await ValidadorImagen.ValidarAsync(archivo,nombre,tipoContenido,ct,8*1024*1024);
        try
        {
            var reserva = await tx.EjecutarAsync(async () => {
                await Autorizar(actorId,ct);
                var p = await Buscar(pagina,ct);
                if (p is null) { p = Nueva(pagina); p.Activa = true; db.Add(p); }
                var anterior = await db.Set<CargaPortadaPagina>().SingleOrDefaultAsync(c => c.Id == idOperacion,ct);
                if (anterior is not null)
                {
                    if (anterior.PortadaId != p.Id || anterior.CreadorId != actorId || anterior.Sha256 != imagen.Sha256) throw Cambio();
                    if (!anterior.Disponible && anterior.VersionDestino != p.Version) throw Cambio();
                    return anterior;
                }
                var carga = new CargaPortadaPagina { Id=idOperacion,PortadaId=p.Id,CreadorId=actorId,
                    Ruta=$"portadas/{pagina}/{idOperacion:N}{imagen.Extension}",Sha256=imagen.Sha256,TipoContenido=imagen.TipoContenido,Tamano=imagen.Contenido.Length,
                    VersionDestino=p.Version,CreadaEnUtc=reloj.GetUtcNow().UtcDateTime };
                db.Add(carga); return carga;
            },ct);
            if (!reserva.Disponible)
            {
                try { await storage.SubirAsync(reserva.Ruta,imagen,token,ct); }
                catch (OperacionException) { if (!await db.Set<CargaPortadaPagina>().AsNoTracking().AnyAsync(c => c.Id == idOperacion && c.Disponible,ct)) throw; }
            }
            db.ChangeTracker.Clear();
            return await tx.EjecutarAsync(async () => {
                await Autorizar(actorId,ct);
                var p = await Buscar(pagina,ct) ?? throw Cambio();
                var carga = await db.Set<CargaPortadaPagina>().SingleAsync(c => c.Id == idOperacion,ct);
                var url = storage.ObtenerUrlPublica(carga.Ruta);
                if (carga.Disponible)
                {
                    if (p.ImagenUrl != url) throw Cambio();
                    return new ImagenPortadaResultado(url,p.Version);
                }
                if (p.Version != carga.VersionDestino) throw Cambio();
                p.ImagenUrl=url; p.Configurada=true; p.Version=Guid.NewGuid(); p.ActualizadoEnUtc=reloj.GetUtcNow().UtcDateTime;
                carga.Disponible=true; carga.SubidaEnUtc=p.ActualizadoEnUtc;
                return new ImagenPortadaResultado(url,p.Version);
            },ct);
        }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); throw Cambio(); }
    }
    private bool ImagenPermitida(string valor)
    {
        if (valor.StartsWith("/img/",StringComparison.Ordinal))
            return valor.Length>5 && !valor.Any(c => c is '\\' or '%' or '?' or '#' || char.IsControl(c)) && !valor.Split('/').Any(s => s is "." or "..");
        if (!Uri.TryCreate(config["Supabase:Url"],UriKind.Absolute,out var origen) ||
            !Uri.TryCreate(valor,UriKind.Absolute,out var url) || url.Scheme != "https" || url.Host != origen.Host || url.Port != origen.Port ||
            url.UserInfo.Length != 0 || url.Query.Length != 0 || url.Fragment.Length != 0) return false;
        var ruta = url.GetComponents(UriComponents.Path,UriFormat.Unescaped);
        const string prefijo = "storage/v1/object/public/pcp-portadas/";
        return ruta.StartsWith(prefijo,StringComparison.Ordinal) && ruta.Length>prefijo.Length &&
            !ruta.Contains('\\') && !ruta.Split('/').Any(s => s is "." or "..");
    }
    private async Task Autorizar(Guid id,CancellationToken ct)
    { if (!await db.PerfilesUsuario.AsNoTracking().AnyAsync(p => p.Id==id && p.Activo && p.Rol==RolUsuario.Administrador,ct)) throw new OperacionException(403,"Solo Administración puede cargar portadas."); }
    private Task<PortadaPagina?> Buscar(string pagina,CancellationToken ct) => db.Set<PortadaPagina>().SingleOrDefaultAsync(p => p.Pagina==pagina,ct);
    private PortadaPagina Nueva(string pagina) => new() { Id=Guid.NewGuid(),Pagina=pagina,Version=Guid.NewGuid(),ActualizadoEnUtc=reloj.GetUtcNow().UtcDateTime };
    private static PortadaGestion Gestion(PortadaPagina p) => new(p.Pagina,p.Activa,p.ImagenUrl,p.Posicion,p.Velo,p.Version,p.ActualizadoEnUtc);
    private static void ValidarPagina(string pagina) { if (!PaginasPortada.Valida(pagina)) throw new OperacionException(404,"Página sin portada configurable."); }
    private static OperacionException Cambio() => new(409,"La portada cambió o la operación corresponde a otra carga. Recarga antes de reintentar.");
}
