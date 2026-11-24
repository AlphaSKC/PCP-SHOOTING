using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;
using PCP_Shooting.Persistence.Interfaces;

namespace PCP_Shooting.Domain.Services;

public sealed class PaquetesService(PcpShootingDbContext db, TimeProvider reloj, ITransaccionesPersistence transacciones) : IPaquetesService
{
    private IQueryable<Paquete> Publicos() => db.Paquetes.AsNoTracking().Where(p => p.Activo && p.PrecioPorPersona > 0 &&
        (p.PrecioSocioCartaq == null || p.PrecioSocioCartaq > 0) &&
        p.Consumibles.All(c => c.Producto.Activo && c.Producto.Categoria == CategoriaProducto.Consumible && !c.Producto.EsVendible));

    public Task<PaginaConsulta<PaquetePublico>> ListarPublicosAsync(FiltroPaquetes filtro, CancellationToken ct)
    {
        Validar(filtro);
        return PaginacionConsulta.CrearAsync(Filtrar(Publicos(), filtro).OrderBy(p => p.Nombre).ThenBy(p => p.Id).Select(Publico), filtro, ct);
    }

    public Task<PaquetePublico?> ConsultarPublicoAsync(Guid id, CancellationToken ct) =>
        Publicos().Where(p => p.Id == id).Select(Publico).SingleOrDefaultAsync(ct);

    public Task<PaginaConsulta<PaqueteGestion>> ListarGestionAsync(FiltroPaquetesGestion filtro, CancellationToken ct)
    {
        Validar(filtro);
        var consulta = Filtrar(db.Paquetes.AsNoTracking().AsSingleQuery(), filtro);
        if (filtro.Activo.HasValue) consulta = consulta.Where(p => p.Activo == filtro.Activo);
        return PaginacionConsulta.CrearAsync(consulta.OrderBy(p => p.Nombre).ThenBy(p => p.Id).Select(Gestion), filtro, ct);
    }

    public Task<PaqueteGestion?> ConsultarGestionAsync(Guid id, CancellationToken ct) =>
        db.Paquetes.AsNoTracking().AsSingleQuery().Where(p => p.Id == id).Select(Gestion).SingleOrDefaultAsync(ct);

    public Task<PaqueteGestion> GuardarAsync(Guid? id, GuardarPaquete datos, CancellationToken ct) =>
        db.Database.IsRelational()
            ? transacciones.EjecutarAsync(() => GuardarInternoAsync(id, datos, ct), ct)
            : GuardarInternoAsync(id, datos, ct);

    private async Task<PaqueteGestion> GuardarInternoAsync(Guid? id, GuardarPaquete datos, CancellationToken ct)
    {
        Validar(datos);
        if (string.IsNullOrWhiteSpace(datos.Nombre) || string.IsNullOrWhiteSpace(datos.IncluyeMarkdown))
            throw new ValidationException("El nombre y lo que incluye el paquete son obligatorios.");
        if (!PrecioValido(datos.PrecioPorPersona) || !PrecioValido(datos.PrecioSocioCartaq))
            throw new ValidationException("Los precios deben ser positivos o cero, con hasta dos decimales y dentro del límite permitido.");
        if (datos.Activo && (!(datos.PrecioPorPersona > 0) || datos.PrecioSocioCartaq is <= 0))
            throw new ValidationException("Un paquete activo requiere precio general positivo y tarifa CARTAQ positiva cuando esté configurada.");
        if (datos.Consumibles.Any(c => c is null)) throw new ValidationException("Cada consumible debe contener un producto y una cantidad.");
        foreach (var consumible in datos.Consumibles)
        {
            Validar(consumible);
            if (consumible.ProductoId == Guid.Empty) throw new ValidationException("El identificador del consumible es obligatorio.");
        }
        var productosIds = datos.Consumibles.Select(c => c.ProductoId).ToArray();
        if (productosIds.Distinct().Count() != productosIds.Length)
            throw new ValidationException("Agrupa cada consumible en una sola cantidad por persona.");
        var codigo = datos.Codigo.ToUpperInvariant();
        var paquete = id.HasValue ? await db.Paquetes.Include(p => p.Consumibles).AsSingleQuery().SingleOrDefaultAsync(p => p.Id == id, ct)
            : new Paquete { Id = Guid.NewGuid(), Codigo = codigo, Nombre = "", IncluyeMarkdown = "", CreadoEnUtc = reloj.GetUtcNow().UtcDateTime };
        if (paquete is null) throw new OperacionException(404, "Paquete inexistente.");
        if (id.HasValue && (datos.Version is null || datos.Version == Guid.Empty)) throw new ValidationException("La versión actual es obligatoria para editar.");
        if (id.HasValue && paquete.Version != datos.Version) throw new OperacionException(409, "El paquete cambió. Consulta su versión actual.");
        if (await db.Paquetes.AnyAsync(p => p.Codigo == codigo && p.Id != paquete.Id, ct))
            throw new OperacionException(409, "El código del paquete ya está registrado.");
        var productos = await db.Productos.AsNoTracking().Where(p => productosIds.Contains(p.Id)).ToListAsync(ct);
        if (productos.Count != productosIds.Length) throw new OperacionException(404, "Algún consumible no existe en inventario.");
        if (productos.Any(p => p.Categoria != CategoriaProducto.Consumible || p.EsVendible))
            throw new ValidationException("Los consumibles deben ser productos internos de categoría Consumible, no vendibles.");
        if (datos.Activo && productos.Any(p => !p.Activo))
            throw new ValidationException("No se puede activar un paquete con consumibles desactivados.");
        paquete.Codigo = codigo;
        paquete.Nombre = datos.Nombre.Trim();
        paquete.Descripcion = datos.Descripcion?.Trim();
        paquete.IncluyeMarkdown = datos.IncluyeMarkdown.Trim();
        paquete.RequisitosMarkdown = datos.RequisitosMarkdown?.Trim();
        paquete.PrecioPorPersona = datos.PrecioPorPersona;
        paquete.PrecioSocioCartaq = datos.PrecioSocioCartaq;
        paquete.Activo = datos.Activo;
        paquete.ActualizadoEnUtc = reloj.GetUtcNow().UtcDateTime;
        paquete.Version = Guid.NewGuid();
        foreach (var anterior in paquete.Consumibles.Where(c => !productosIds.Contains(c.ProductoId)).ToArray())
            db.ConsumiblesPaquete.Remove(anterior);
        foreach (var componente in datos.Consumibles)
        {
            var existente = paquete.Consumibles.SingleOrDefault(c => c.ProductoId == componente.ProductoId);
            if (existente is not null) existente.CantidadPorPersona = componente.CantidadPorPersona;
            else
            {
                var nuevo = new ConsumiblePaquete
                {
                    Id = Guid.NewGuid(), Paquete = paquete, PaqueteId = paquete.Id,
                    ProductoId = componente.ProductoId, CantidadPorPersona = componente.CantidadPorPersona
                };
                // Con un GUID ya asignado debe registrarse explícitamente como inserción.
                db.ConsumiblesPaquete.Add(nuevo);
            }
        }
        if (!id.HasValue) db.Paquetes.Add(paquete);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new OperacionException(409, "El paquete cambió. Consulta su versión actual."); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { throw new OperacionException(409, "El código o consumible ya está registrado."); }
        return (await ConsultarGestionAsync(paquete.Id, ct))!;
    }

    private static bool PrecioValido(decimal? precio) => precio is null ||
        precio >= 0 && precio <= 9999999999.99m && decimal.Round(precio.Value, 2) == precio;

    private static IQueryable<Paquete> Filtrar(IQueryable<Paquete> consulta, FiltroPaquetes filtro)
    {
        var texto = filtro.Busqueda?.Trim().ToUpperInvariant();
        if (!string.IsNullOrEmpty(texto)) consulta = consulta.Where(p => p.Codigo.Contains(texto) || p.Nombre.ToUpper().Contains(texto) ||
            p.Descripcion != null && p.Descripcion.ToUpper().Contains(texto));
        return consulta;
    }

    private static readonly Expression<Func<Paquete, PaquetePublico>> Publico = p => new(
        p.Id, p.Codigo, p.Nombre, p.Descripcion, p.IncluyeMarkdown, p.RequisitosMarkdown, p.PrecioPorPersona, p.PrecioSocioCartaq);

    private static readonly Expression<Func<Paquete, PaqueteGestion>> Gestion = p => new(
        p.Id, p.Codigo, p.Nombre, p.Descripcion, p.IncluyeMarkdown, p.RequisitosMarkdown, p.PrecioPorPersona, p.PrecioSocioCartaq, p.Activo,
        p.Activo && p.PrecioPorPersona > 0 && (p.PrecioSocioCartaq == null || p.PrecioSocioCartaq > 0) &&
            p.Consumibles.All(c => c.Producto.Activo && c.Producto.Categoria == CategoriaProducto.Consumible && !c.Producto.EsVendible),
        p.CreadoEnUtc, p.ActualizadoEnUtc, p.Version, p.Consumibles.OrderBy(c => c.Producto.Sku).Select(c =>
            new ConsumiblePaqueteConsulta(c.ProductoId, c.Producto.Sku, c.Producto.Nombre, c.CantidadPorPersona, c.Producto.Activo)).ToList());

    private static void Validar(object datos) => Validator.ValidateObject(datos, new ValidationContext(datos), true);
}
