using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Services;

public sealed class ImagenesService(PcpShootingDbContext db, ITransaccionesPersistence transacciones, IImagenStorage storage)
    : IImagenesService
{
    public async Task<ImagenProductoResultado> SubirAsync(Guid actorId, Guid productoId, Guid idOperacion,
        Stream archivo, string nombre, string tipoContenido, string token, CancellationToken ct)
    {
        if (idOperacion == Guid.Empty)
            throw new OperacionException(400, "IdOperacion requiere un UUID distinto de cero.");
        var imagen = await ValidadorImagen.ValidarAsync(archivo, nombre, tipoContenido, ct);
        var registro = await transacciones.EjecutarAsync(async () =>
        {
            await Autorizar(actorId, productoId, ct);
            var existente = await db.ImagenesProducto.SingleOrDefaultAsync(i => i.Id == idOperacion, ct);
            if (existente is not null)
            {
                if (existente.ProductoId != productoId || existente.Sha256 != imagen.Sha256)
                    throw new OperacionException(409, "El IdOperacion ya corresponde a otra imagen.");
                return existente;
            }
            var nueva = new ImagenProducto
            {
                Id = idOperacion, ProductoId = productoId, CreadorId = actorId,
                Ruta = $"productos/{productoId:N}/{idOperacion:N}{imagen.Extension}",
                Sha256 = imagen.Sha256, TipoContenido = imagen.TipoContenido,
                Tamano = imagen.Contenido.Length, CreadaEnUtc = DateTime.UtcNow
            };
            db.ImagenesProducto.Add(nueva);
            return nueva;
        }, ct);
        if (!registro.Disponible)
        {
            try
            {
                await storage.SubirAsync(registro.Ruta, imagen, token, ct);
            }
            catch (OperacionException)
            {
                // Otro reintento concurrente pudo completar la misma carga.
                if (!await db.ImagenesProducto.AsNoTracking().AnyAsync(i => i.Id == idOperacion && i.Disponible, ct))
                    throw;
            }
        }

        // Recarga el estado después de la llamada externa, también cuando ya terminó un reintento.
        db.ChangeTracker.Clear();
        return await transacciones.EjecutarAsync(async () =>
        {
            var producto = await Autorizar(actorId, productoId, ct);
            var guardada = await db.ImagenesProducto.SingleAsync(i => i.Id == idOperacion, ct);
            var url = storage.ObtenerUrlPublica(guardada.Ruta);
            if (!guardada.Disponible)
            {
                guardada.Disponible = true;
                guardada.SubidaEnUtc = DateTime.UtcNow;
                producto.ImagenUrl = url;
            }
            return new ImagenProductoResultado(guardada.Id, productoId, url, guardada.TipoContenido,
                guardada.Tamano, producto.ImagenUrl == url);
        }, ct);
    }

    private async Task<Producto> Autorizar(Guid actorId, Guid productoId, CancellationToken ct)
    {
        var perfil = await db.PerfilesUsuario.AsNoTracking().SingleOrDefaultAsync(p => p.Id == actorId && p.Activo, ct);
        if (perfil is null || perfil.Rol is not (RolUsuario.Administrador or RolUsuario.Recepcion))
            throw new OperacionException(403, "No tienes permiso para gestionar imágenes.");
        var producto = await db.Productos.SingleOrDefaultAsync(p => p.Id == productoId, ct);
        if (producto is null) throw new OperacionException(404, "Producto inexistente.");
        if (perfil.Rol == RolUsuario.Recepcion && producto.Categoria != CategoriaProducto.Alimento)
            throw new OperacionException(403, "Recepción solo puede gestionar imágenes de alimentos.");
        return producto;
    }
}