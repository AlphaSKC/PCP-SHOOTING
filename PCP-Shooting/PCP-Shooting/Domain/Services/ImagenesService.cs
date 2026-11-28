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
        Stream archivo, string nombre, string tipoContenido, string token, CancellationToken ct, bool agregar = false)
    {
        if (idOperacion == Guid.Empty)
            throw new OperacionException(400, "IdOperacion requiere un UUID distinto de cero.");
        var imagen = await ValidadorImagen.ValidarAsync(archivo, nombre, tipoContenido, ct);
        var registro = await transacciones.EjecutarAsync(async () =>
        {
            var producto=await Autorizar(actorId, productoId, ct);
            var existente = await db.ImagenesProducto.SingleOrDefaultAsync(i => i.Id == idOperacion, ct);
            if (existente is not null)
            {
                if (existente.ProductoId != productoId || existente.Sha256 != imagen.Sha256 || existente.AgregarGaleria != agregar)
                    throw new OperacionException(409, "El IdOperacion ya corresponde a otra imagen.");
                return existente;
            }
            var fotos=await FotosProducto.Cargar(db,producto,ct);
            var pendientes=await db.ImagenesProducto.CountAsync(i=>i.ProductoId==productoId && !i.Disponible && i.AgregarGaleria,ct);
            if(agregar && fotos.Count+pendientes>=8)
                throw new OperacionException(409,"La galería admite un máximo de 8 fotos, incluyendo las cargas pendientes de agregar.");
            var nueva = new ImagenProducto
            {
                Id = idOperacion, ProductoId = productoId, CreadorId = actorId, AgregarGaleria=agregar,
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
                if(guardada.AgregarGaleria)
                {
                    var fotos=await FotosProducto.Cargar(db,producto,ct);
                    if(fotos.Count>=8) throw new OperacionException(409,"La galería admite un máximo de 8 fotos. Quita otra foto y reintenta la misma carga.");
                    db.Add(new FotoProducto { Id=guardada.Id,ProductoId=productoId,Url=url,Orden=fotos.Count });
                    if(fotos.Count==0) producto.ImagenUrl=url;
                }
                else await FotosProducto.Reemplazar(db,producto,url,ct,guardada.Id);
                guardada.Disponible = true;
                guardada.SubidaEnUtc = DateTime.UtcNow;
            }
            return new ImagenProductoResultado(guardada.Id, productoId, url, guardada.TipoContenido,
                guardada.Tamano, producto.ImagenUrl == url);
        }, ct);
    }

    public Task<IReadOnlyList<FotoProductoResultado>> OrdenarAsync(Guid actorId,Guid productoId,OrdenarImagenesProducto datos,CancellationToken ct) =>
        transacciones.EjecutarAsync<IReadOnlyList<FotoProductoResultado>>(async ()=>
        {
            var producto=await Autorizar(actorId,productoId,ct);
            if(datos.Ids is null || datos.Ids.Count is <1 or >8 || datos.Ids.Distinct().Count()!=datos.Ids.Count || datos.Ids.Contains(Guid.Empty))
                throw new OperacionException(400,"Envía entre 1 y 8 identificadores de imagen distintos.");
            var fotos=await FotosProducto.Cargar(db,producto,ct);
            if(fotos.Count!=datos.Ids.Count || fotos.Any(f=>!datos.Ids.Contains(f.Id)))
                throw new OperacionException(409,"La galería cambió o contiene imágenes ajenas; recarga y envía todos sus identificadores.");
            var ordenadas=datos.Ids.Select(id=>fotos.Single(f=>f.Id==id)).ToArray();
            for(var i=0;i<ordenadas.Length;i++) ordenadas[i].Orden=i;
            producto.ImagenUrl=ordenadas[0].Url;
            return ordenadas.Select(f=>new FotoProductoResultado(f.Id,f.Url,f.Orden)).ToArray();
        },ct);

    public Task QuitarAsync(Guid actorId,Guid productoId,Guid imagenId,CancellationToken ct) =>
        transacciones.EjecutarAsync(async ()=>
        {
            var producto=await Autorizar(actorId,productoId,ct);
            var fotos=await FotosProducto.Cargar(db,producto,ct);
            var foto=fotos.SingleOrDefault(f=>f.Id==imagenId);
            if(foto is null) throw new OperacionException(404,"Imagen inexistente en este producto.");
            if(fotos.Count==1 || fotos[0].Id==imagenId)
                throw new OperacionException(409,"Elige primero otra imagen principal mediante el orden de la galería; no se puede quitar la única imagen.");
            db.Remove(foto); fotos.Remove(foto);
            for(var i=0;i<fotos.Count;i++) fotos[i].Orden=i;
            return true;
        },ct);

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
