using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Services;

public sealed class EliminacionProductosService(PcpShootingDbContext db, ITransaccionesPersistence tx,
    IServiceProvider servicios, TimeProvider reloj) : IEliminacionProductosService
{
    public async Task EliminarAsync(Guid actorId, Guid productoId, string? token, CancellationToken ct)
    {
        try
        {
            await tx.EjecutarAsync(async () =>
            {
                var perfil = await db.PerfilesUsuario.AsNoTracking().SingleOrDefaultAsync(p => p.Id == actorId && p.Activo, ct);
                if (perfil?.Rol is not (RolUsuario.Administrador or RolUsuario.Recepcion))
                    throw new OperacionException(403, "No tienes permiso para eliminar productos.");
                var anterior = await db.EliminacionesProducto.Include(e => e.Archivos).SingleOrDefaultAsync(e => e.Id == productoId, ct);
                var producto = await db.Productos.SingleOrDefaultAsync(p => p.Id == productoId, ct);
                if (anterior is null && producto is null) throw new OperacionException(404, "Producto inexistente.");
                if (perfil.Rol == RolUsuario.Recepcion && (producto?.Categoria ?? anterior!.Categoria) != CategoriaProducto.Alimento)
                    throw new OperacionException(403, "Recepción solo puede eliminar alimentos.");
                if (anterior is not null) return true;
                if (await db.DetallesPedido.AnyAsync(d => d.ProductoId == productoId, ct) ||
                    await db.MovimientosInventario.AnyAsync(m => m.ProductoId == productoId, ct) ||
                    await db.ConsumiblesPaquete.AnyAsync(c => c.ProductoId == productoId, ct) ||
                    await db.ConsumosReserva.AnyAsync(c => c.ProductoId == productoId, ct))
                    throw Historial();
                var imagenes = await db.ImagenesProducto.Where(i => i.ProductoId == productoId).ToListAsync(ct);
                if (imagenes.Any(i => !i.Disponible))
                    throw new OperacionException(409, "Tiene una carga de imagen pendiente; complétala antes de eliminarlo u ocúltalo.");
                foreach (var imagen in imagenes)
                    if (!Regex.IsMatch(imagen.Ruta, $@"\Aproductos/{productoId:N}/[0-9a-f]{{32}}\.(jpg|png|webp)\z"))
                        throw new OperacionException(409, "Una imagen no tiene una ruta propia verificable; oculta el producto.");
                var claves = imagenes.Select(i => i.Ruta).ToList();
                if (!string.IsNullOrWhiteSpace(producto!.ImagenUrl)) claves.Add(producto.ImagenUrl);
                if (producto.ImagenUrl?.Contains("/pcp-productos/", StringComparison.Ordinal) == true &&
                    !imagenes.Any(i => Normalizar(producto.ImagenUrl).EndsWith("/" + i.Ruta, StringComparison.Ordinal)))
                    throw new OperacionException(409, "La imagen de Storage no tiene una carga propia registrada; oculta el producto.");
                if (await ImagenCompartida(productoId, claves, ct))
                    throw new OperacionException(409, "Tiene imágenes en uso por otro registro; ocúltalo.");
                if (imagenes.Count > 0 && string.IsNullOrWhiteSpace(token))
                    throw new OperacionException(401, "Se requiere una sesión Bearer vigente para limpiar Storage.");
                db.EliminacionesProducto.Add(new EliminacionProducto
                {
                    Id = productoId, Categoria = producto.Categoria, ActorId = actorId, CreadaEnUtc = Ahora(),
                    CompletadaEnUtc = imagenes.Count == 0 ? Ahora() : null,
                    Archivos = imagenes.Select(i => new ArchivoProductoEliminado
                        { Id = Guid.NewGuid(), EliminacionId = productoId, Ruta = i.Ruta }).ToList()
                });
                // Explícito: nunca eliminar pedidos, movimientos, consumos ni otras entidades en cascada.
                db.ImagenesProducto.RemoveRange(imagenes);
                db.Productos.Remove(producto);
                return true;
            }, ct);
        }
        catch (DbUpdateException error) when (error.InnerException is Npgsql.PostgresException { SqlState: "23503" })
        {
            throw Historial();
        }

        var pendientes = await db.ArchivosProductoEliminado.AsNoTracking()
            .Where(a => a.EliminacionId == productoId && a.EliminadoEnUtc == null).OrderBy(a => a.Ruta).ToListAsync(ct);
        if (pendientes.Count == 0) return;
        if (string.IsNullOrWhiteSpace(token)) throw new OperacionException(401, "Inicia sesión para terminar la limpieza de imágenes.");
        var storage = servicios.GetRequiredService<IEliminacionImagenStorage>();
        foreach (var archivo in pendientes)
        {
            await storage.EliminarAsync(archivo.Ruta, token, ct);
            db.ChangeTracker.Clear();
            await tx.EjecutarAsync(async () =>
            {
                var fila = await db.ArchivosProductoEliminado.SingleAsync(a => a.Id == archivo.Id, ct);
                fila.EliminadoEnUtc ??= Ahora();
                var restantes = await db.ArchivosProductoEliminado.AnyAsync(a => a.EliminacionId == productoId &&
                    a.Id != fila.Id && a.EliminadoEnUtc == null, ct);
                if (!restantes)
                {
                    var eliminacion = await db.EliminacionesProducto.SingleAsync(e => e.Id == productoId, ct);
                    eliminacion.CompletadaEnUtc ??= Ahora();
                }
                return true;
            }, ct);
        }
    }

    private DateTime Ahora() => reloj.GetUtcNow().UtcDateTime;
    private static OperacionException Historial() => new(409, "El producto tiene historial; ocúltalo en lugar de eliminarlo.");

    private async Task<bool> ImagenCompartida(Guid id, List<string> claves, CancellationToken ct)
    {
        if (claves.Count == 0) return false;
        // Incluye Markdown/JSON y registros inactivos: también deben conservar sus imágenes.
        return await Comparte(db.Productos.Where(p => p.Id != id), claves, ct) ||
            await Comparte(db.ArticulosBlog, claves, ct) || await Comparte(db.Set<Evento>(), claves, ct) ||
            await Comparte(db.MarcasAliadas, claves, ct) || await Comparte(db.Set<CategoriaTienda>(), claves, ct) ||
            await Comparte(db.Set<FotoGaleria>(), claves, ct) || await Comparte(db.Set<PortadaPagina>(), claves, ct) ||
            await Comparte(db.Paquetes, claves, ct) || await Comparte(db.ReservasWeb, claves, ct) ||
            await Comparte(db.Set<ConfiguracionContenidoSitio>(), claves, ct);
    }

    private static async Task<bool> Comparte<T>(IQueryable<T> consulta, List<string> claves, CancellationToken ct) where T : class
    {
        var opciones = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        var normalizadas = claves.Select(Normalizar).ToArray();
        foreach (var fila in await consulta.AsNoTracking().ToListAsync(ct))
        {
            var texto = Normalizar(JsonSerializer.Serialize(fila, opciones));
            if (normalizadas.Any(texto.Contains)) return true;
        }
        return false;
    }

    private static string Normalizar(string texto)
    {
        for (var i = 0; i < 3; i++) texto = Uri.UnescapeDataString(texto);
        return texto.Replace("\\/", "/").Replace("\\u002F", "/", StringComparison.OrdinalIgnoreCase);
    }
}
