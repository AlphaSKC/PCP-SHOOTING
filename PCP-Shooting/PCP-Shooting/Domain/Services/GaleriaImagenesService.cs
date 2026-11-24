using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Services;

public sealed class GaleriaImagenesService(PcpShootingDbContext db, ITransaccionesPersistence transacciones, IImagenStorage storage, TimeProvider reloj) : IGaleriaImagenesService
{
    public async Task<ImagenContenidoResultado> SubirAsync(Guid actorId, Guid fotoId, Guid idOperacion, Stream archivo, string nombre, string tipoContenido, string token, CancellationToken ct)
    {
        if (idOperacion == Guid.Empty) throw new OperacionException(400, "IdOperacion requiere un UUID distinto de cero.");
        var imagen = await ValidadorImagen.ValidarAsync(archivo, nombre, tipoContenido, ct);
        var registro = await transacciones.EjecutarAsync(async () => {
            var foto = await Autorizar(actorId, fotoId, ct);
            var anterior = await db.Set<CargaFotoGaleria>().SingleOrDefaultAsync(c => c.Id == idOperacion, ct);
            if (anterior is not null)
            {
                if (anterior.ActorId != actorId || anterior.FotoId != fotoId || anterior.Sha256 != imagen.Sha256) throw Cambio();
                if (!anterior.Disponible && anterior.VersionDestino != foto.Version) throw Cambio();
                return anterior;
            }
            var carga = new CargaFotoGaleria { Id = idOperacion, FotoId = fotoId, ActorId = actorId,
                Ruta = $"galeria/{fotoId:N}/{idOperacion:N}{imagen.Extension}", Sha256 = imagen.Sha256,
                TipoContenido = imagen.TipoContenido, Tamano = imagen.Contenido.Length, VersionDestino = foto.Version, CreadaEnUtc = reloj.GetUtcNow().UtcDateTime };
            db.Add(carga); return carga;
        }, ct);
        if (!registro.Disponible)
        {
            try { await storage.SubirAsync(registro.Ruta, imagen, token, ct); }
            catch (OperacionException) { if (!await db.Set<CargaFotoGaleria>().AsNoTracking().AnyAsync(c => c.Id == idOperacion && c.Disponible, ct)) throw; }
        }
        db.ChangeTracker.Clear();
        return await transacciones.EjecutarAsync(async () => {
            var foto = await Autorizar(actorId, fotoId, ct);
            var carga = await db.Set<CargaFotoGaleria>().SingleAsync(c => c.Id == idOperacion, ct);
            var url = storage.ObtenerUrlPublica(carga.Ruta);
            if (carga.Disponible)
            {
                if (foto.ImagenUrl != url) throw Cambio();
                return new ImagenContenidoResultado(url, foto.Version);
            }
            if (foto.Version != carga.VersionDestino) throw Cambio();
            foto.ImagenUrl = url; foto.Version = Guid.NewGuid(); foto.ActualizadoEnUtc = reloj.GetUtcNow().UtcDateTime;
            carga.Disponible = true; carga.SubidaEnUtc = foto.ActualizadoEnUtc;
            return new ImagenContenidoResultado(url, foto.Version);
        }, ct);
    }
    private async Task<FotoGaleria> Autorizar(Guid actorId, Guid id, CancellationToken ct)
    {
        if (!await db.PerfilesUsuario.AsNoTracking().AnyAsync(p => p.Id == actorId && p.Activo && p.Rol == RolUsuario.Administrador, ct))
            throw new OperacionException(403, "Solo Administración puede gestionar la galería.");
        return await db.Set<FotoGaleria>().SingleOrDefaultAsync(f => f.Id == id, ct) ?? throw new OperacionException(404, "Foto inexistente.");
    }
    private static OperacionException Cambio() => new(409, "La carga corresponde a otros datos o la foto cambió. Recarga y usa una operación nueva.");
}
