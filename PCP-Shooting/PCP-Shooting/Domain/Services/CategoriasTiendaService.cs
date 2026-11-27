using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Models;
namespace PCP_Shooting.Domain.Services;

public sealed class CategoriasTiendaService(PcpShootingDbContext db, TimeProvider reloj, ITransaccionesPersistence transacciones) : ICategoriasTiendaService
{
    public async Task<IReadOnlyList<CategoriaTiendaPublica>> ListarPublicasAsync(CancellationToken ct) =>
        await db.Set<CategoriaTienda>().AsNoTracking().Where(c => c.Activa).OrderBy(c => c.Orden).ThenBy(c => c.Nombre).ThenBy(c => c.Id)
            .Select(c => new CategoriaTiendaPublica(c.Id, c.Nombre, c.Slug, c.Descripcion, c.ImagenUrl, c.Orden,
                db.Productos.Count(p => p.CategoriaTiendaId == c.Id && p.Activo && p.EsVendible && p.Precio != null && p.Categoria != CategoriaProducto.Consumible))).ToArrayAsync(ct);
    private IQueryable<CategoriaTiendaGestion> Gestion(Guid? id = null) => db.Set<CategoriaTienda>().AsNoTracking().Where(c => id == null || c.Id == id)
        .OrderBy(c => c.Orden).ThenBy(c => c.Nombre).ThenBy(c => c.Id)
        .Select(c => new CategoriaTiendaGestion(c.Id, c.Nombre, c.Slug, c.Descripcion, c.ImagenUrl, c.Orden, c.Activa, c.Version,
            db.Productos.Count(p => p.CategoriaTiendaId == c.Id)));
    public async Task<IReadOnlyList<CategoriaTiendaGestion>> ListarGestionAsync(CancellationToken ct) => await Gestion().ToArrayAsync(ct);
    public async Task<CategoriaTiendaGestion> GuardarAsync(Guid? id, GuardarCategoriaTienda datos, CancellationToken ct)
    {
        Validator.ValidateObject(datos, new ValidationContext(datos), true);
        if (new[] { "carrito", "checkout", "pedido", "producto" }.Contains(datos.Slug)) throw new OperacionException(409, "El slug está reservado por una ruta de la tienda.");
        if (id.HasValue && (datos.Version is null || datos.Version == Guid.Empty)) throw new ValidationException("Envía la versión actual de la categoría.");
        var categoria = id.HasValue ? await db.Set<CategoriaTienda>().SingleOrDefaultAsync(c => c.Id == id, ct) : new CategoriaTienda { Id = Guid.NewGuid() };
        if (categoria is null) throw new OperacionException(404, "Categoría inexistente.");
        if (id.HasValue && categoria.Version != datos.Version) throw Cambio();
        if (await db.Set<CategoriaTienda>().AnyAsync(c => c.Slug == datos.Slug && c.Id != categoria.Id, ct)) throw Duplicado();
        categoria.Nombre = datos.Nombre.Trim(); categoria.Slug = datos.Slug;
        categoria.Descripcion = string.IsNullOrWhiteSpace(datos.Descripcion) ? null : datos.Descripcion.Trim();
        categoria.Orden = datos.Orden; categoria.Activa = datos.Activa; categoria.Version = Guid.NewGuid(); categoria.ActualizadoEnUtc = reloj.GetUtcNow().UtcDateTime;
        if (!id.HasValue) db.Add(categoria);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); throw Cambio(); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505", ConstraintName: "IX_CategoriasTienda_Slug" })
        { db.ChangeTracker.Clear(); throw Duplicado(); }
        return await Gestion(categoria.Id).SingleAsync(ct);
    }
    public async Task EliminarAsync(Guid id, CancellationToken ct)
    {
        try
        {
            await transacciones.EjecutarAsync(async () => {
                var categoria = await db.Set<CategoriaTienda>().SingleOrDefaultAsync(c => c.Id == id, ct) ?? throw new OperacionException(404, "Categoría inexistente.");
                if (await db.Productos.AnyAsync(p => p.CategoriaTiendaId == id, ct)) throw Asignada();
                db.RemoveRange(await db.CargasImagenContenido.Where(c => c.CategoriaTiendaId == id).ToListAsync(ct));
                db.Remove(categoria); return true;
            }, ct);
        }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); throw Cambio(); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23503" })
        { db.ChangeTracker.Clear(); throw Asignada(); }
    }
    private static OperacionException Cambio() => new(409, "La categoría cambió. Vuelve a consultarla.");
    private static OperacionException Duplicado() => new(409, "El slug ya está registrado.");
    private static OperacionException Asignada() => new(409, "Mueve sus productos a otra categoría o desactívala.");
}
