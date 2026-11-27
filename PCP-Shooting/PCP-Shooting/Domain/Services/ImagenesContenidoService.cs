using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Services;

public sealed class ImagenesContenidoService(PcpShootingDbContext db, ITransaccionesPersistence transacciones,
    IImagenStorage storage, TimeProvider reloj) : IImagenesContenidoService
{
    public Task<ImagenContenidoResultado> SubirAsync(Guid actorId, Guid destinoId, bool marca, Guid idOperacion,
        Stream archivo, string nombre, string tipoContenido, string token, CancellationToken ct) =>
        SubirInterno(actorId, destinoId, marca ? 0 : 1, idOperacion, archivo, nombre, tipoContenido, token, ct);
    public Task<ImagenContenidoResultado> SubirCategoriaAsync(Guid actorId, Guid destinoId, Guid idOperacion,
        Stream archivo, string nombre, string tipoContenido, string token, CancellationToken ct) =>
        SubirInterno(actorId, destinoId, 2, idOperacion, archivo, nombre, tipoContenido, token, ct);
    private async Task<ImagenContenidoResultado> SubirInterno(Guid actorId, Guid destinoId, int tipo, Guid idOperacion,
        Stream archivo, string nombre, string tipoContenido, string token, CancellationToken ct)
    {
        var marca = tipo == 0;
        if (idOperacion == Guid.Empty) throw new OperacionException(400, "IdOperacion requiere un UUID distinto de cero.");
        var imagen = await ValidadorImagen.ValidarAsync(archivo, nombre, tipoContenido, ct, marca ? 2 * 1024 * 1024 : ValidadorImagen.MaximoBytes);
        var registro = await transacciones.EjecutarAsync(async () =>
        {
            var destino = await Autorizar(actorId, destinoId, tipo, ct);
            var existente = await db.CargasImagenContenido.SingleOrDefaultAsync(c => c.Id == idOperacion, ct);
            if (existente is not null)
            {
                if (existente.CreadorId != actorId || existente.MarcaId != (marca ? destinoId : null) || existente.ArticuloBlogId != (tipo == 1 ? destinoId : null) ||
                    existente.CategoriaTiendaId != (tipo == 2 ? destinoId : null) || existente.Sha256 != imagen.Sha256)
                    throw new OperacionException(409, "El IdOperacion corresponde a otra carga.");
                if (!existente.Disponible && existente.VersionDestino != destino.Version) throw Cambio();
                return existente;
            }
            var nueva = new CargaImagenContenido
            {
                Id = idOperacion,
                MarcaId = marca ? destinoId : null,
                ArticuloBlogId = tipo == 1 ? destinoId : null,
                CategoriaTiendaId = tipo == 2 ? destinoId : null,
                CreadorId = actorId,
                Ruta = $"{(marca ? "marcas" : tipo == 1 ? "blog" : "tienda")}/{destinoId:N}/{idOperacion:N}{imagen.Extension}",
                Sha256 = imagen.Sha256,
                TipoContenido = imagen.TipoContenido,
                Tamano = imagen.Contenido.Length,
                VersionDestino = destino.Version,
                CreadaEnUtc = reloj.GetUtcNow().UtcDateTime
            };
            db.CargasImagenContenido.Add(nueva); return nueva;
        }, ct);
        if (!registro.Disponible)
        {
            try { await storage.SubirAsync(registro.Ruta, imagen, token, ct); }
            catch (OperacionException)
            {
                if (!await db.CargasImagenContenido.AsNoTracking().AnyAsync(c => c.Id == idOperacion && c.Disponible, ct)) throw;
            }
        }
        db.ChangeTracker.Clear();
        return await transacciones.EjecutarAsync(async () =>
        {
            var destino = await Autorizar(actorId, destinoId, tipo, ct);
            var carga = await db.CargasImagenContenido.SingleAsync(c => c.Id == idOperacion, ct);
            var url = storage.ObtenerUrlPublica(carga.Ruta);
            if (carga.Disponible)
            {
                if (destino.Url != url) throw Cambio();
                return new ImagenContenidoResultado(url, destino.Version);
            }
            if (destino.Version != carga.VersionDestino) throw Cambio();
            var ahora = reloj.GetUtcNow().UtcDateTime;
            destino.Asignar(url, ahora); carga.Disponible = true; carga.SubidaEnUtc = ahora;
            return new ImagenContenidoResultado(url, destino.Version);
        }, ct);
    }
    private static OperacionException Cambio() => new(409, "El contenido cambió o la imagen fue sustituida. Recarga y usa un IdOperacion nuevo.");
    private async Task<Destino> Autorizar(Guid actorId, Guid id, int tipo, CancellationToken ct)
    {
        if (!await db.PerfilesUsuario.AsNoTracking().AnyAsync(p => p.Id == actorId && p.Activo && p.Rol == RolUsuario.Administrador, ct))
            throw new OperacionException(403, "Solo Administración puede gestionar estas imágenes.");
        if (tipo == 2) return new(null, null, await db.Set<CategoriaTienda>().SingleOrDefaultAsync(c => c.Id == id, ct) ?? throw new OperacionException(404, "Categoría inexistente."));
        if (tipo == 0) return new(await db.MarcasAliadas.SingleOrDefaultAsync(m => m.Id == id, ct) ?? throw new OperacionException(404, "Marca inexistente."), null);
        return new(null, await db.ArticulosBlog.SingleOrDefaultAsync(a => a.Id == id, ct) ?? throw new OperacionException(404, "Artículo inexistente."));
    }
    private sealed class Destino(MarcaAliada? marca, ArticuloBlog? articulo, CategoriaTienda? categoria = null)
    {
        public Guid Version => marca?.Version ?? categoria?.Version ?? articulo!.Version;
        public string? Url => marca is not null ? marca.LogoUrl : categoria is not null ? categoria.ImagenUrl : articulo!.ImagenUrl;
        public void Asignar(string url, DateTime ahora)
        {
            if (marca is not null) { marca.LogoUrl = url; marca.Version = Guid.NewGuid(); marca.ActualizadoEnUtc = ahora; }
            else if (categoria is not null) { categoria.ImagenUrl = url; categoria.Version = Guid.NewGuid(); categoria.ActualizadoEnUtc = ahora; }
            else { articulo!.ImagenUrl = url; articulo.Version = Guid.NewGuid(); articulo.ActualizadoEnUtc = ahora; }
        }
    }
}
