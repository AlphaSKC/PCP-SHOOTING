using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Services;

public sealed class EscriturasService(PcpShootingDbContext db, ITransaccionesPersistence transacciones)
    : IEscriturasService
{
    private const decimal ImporteMaximo = 9999999999.99m;

    public Task<EscrituraResultado> GuardarProductoAsync(Guid? id, GuardarProducto datos, CancellationToken ct)
    {
        Validar(datos);
        Exigir(datos.ImagenUrl is null ||
            Uri.TryCreate(datos.ImagenUrl, UriKind.Absolute, out var imagen) &&
            (imagen.Scheme == Uri.UriSchemeHttp || imagen.Scheme == Uri.UriSchemeHttps),
            400, "La imagen requiere una URL HTTP o HTTPS válida.");
        Exigir(!string.IsNullOrWhiteSpace(datos.Sku) && !string.IsNullOrWhiteSpace(datos.Nombre), 400, "SKU y nombre son obligatorios.");
        Exigir(datos.Precio is null || EsImporteValido(datos.Precio.Value), 400, "El precio debe tener hasta dos decimales y ser mayor o igual a cero.");
        Exigir(!datos.EsVendible || datos.Precio > 0, 400, "Un artículo vendible requiere un precio positivo.");
        Exigir(datos.Categoria != CategoriaProducto.Consumible || !datos.EsVendible, 400, "Los consumibles son internos y no se venden en la tienda.");
        Exigir(datos.EspecificacionesTecnicas is null || datos.EspecificacionesTecnicas.Value.ValueKind == JsonValueKind.Object,
            400, "Las especificaciones deben ser un objeto JSON.");
        return transacciones.EjecutarAsync(async () =>
        {
            var sku = datos.Sku.Trim();
            Exigir(!await db.Productos.AnyAsync(p => p.Sku == sku && (!id.HasValue || p.Id != id.Value), ct),
                409, "El SKU ya está registrado.");
            var producto = id.HasValue
                ? await db.Productos.SingleOrDefaultAsync(p => p.Id == id.Value, ct)
                : new Producto { Id = Guid.NewGuid(), Sku = sku, Nombre = datos.Nombre.Trim() };
            Exigir(producto is not null, 404, "Producto inexistente.");
            Exigir(datos.Categoria == CategoriaProducto.Consumible || !await db.ConsumiblesPaquete.AnyAsync(c => c.ProductoId == producto!.Id, ct),
                409, "No se puede cambiar la categoría de un consumible asociado a paquetes.");
            producto!.Sku = sku;
            producto.Nombre = datos.Nombre.Trim();
            producto.Descripcion = datos.Descripcion?.Trim();
            producto.Categoria = datos.Categoria;
            producto.ImagenUrl = datos.ImagenUrl;
            producto.NotasMarkdown = datos.NotasMarkdown;
            producto.Precio = datos.Precio;
            producto.EsVendible = datos.EsVendible;
            producto.EspecificacionesTecnicas = datos.EspecificacionesTecnicas?.Clone();
            if (!id.HasValue) db.Productos.Add(producto);
            return new EscrituraResultado(producto.Id, "Guardado");
        }, ct);
    }

    public Task<EscrituraResultado> CrearPedidoAsync(Guid usuarioId, CrearPedido datos, CancellationToken ct)
    {
        Validar(datos);
        ValidarId(datos.IdOperacion);
        Exigir(datos.Partidas is { Count: > 0 }, 400, "El pedido requiere partidas.");
        foreach (var partida in datos.Partidas)
        {
            Exigir(partida is not null, 400, "La partida es obligatoria.");
            Validar(partida!);
            ValidarId(partida!.ProductoId);
        }
        Exigir(datos.Partidas.Select(p => p.ProductoId).Distinct().Count() == datos.Partidas.Count,
            400, "Agrupa cada producto en una sola partida.");
        return transacciones.EjecutarAsync(async () =>
        {
            var existente = await db.Pedidos.Include(p => p.Detalles).SingleOrDefaultAsync(p => p.Id == datos.IdOperacion, ct);
            if (existente is not null)
            {
                Exigir(existente.UsuarioId == usuarioId && existente.Detalles.Count == datos.Partidas.Count &&
                    existente.Detalles.All(d => datos.Partidas.Any(p => p.ProductoId == d.ProductoId && p.Cantidad == d.Cantidad)),
                    409, "El IdOperacion ya corresponde a otro pedido.");
                return new EscrituraResultado(existente.Id, existente.Estado.ToString());
            }
            Exigir(await db.PerfilesUsuario.AnyAsync(p => p.Id == usuarioId && p.Activo, ct), 403, "El perfil requiere estar activo.");
            var pedido = new Pedido { Id = datos.IdOperacion, UsuarioId = usuarioId, CreadoEnUtc = DateTime.UtcNow };
            foreach (var partida in datos.Partidas)
            {
                var producto = await db.Productos.SingleOrDefaultAsync(p => p.Id == partida.ProductoId, ct);
                Exigir(producto is not null, 404, "Producto inexistente.");
                Exigir(producto!.Activo && producto.EsVendible && producto.Precio > 0 && EsImporteValido(producto.Precio.Value),
                    409, "El producto requiere estar disponible para venta con un precio válido.");
                Exigir(await Existencia(producto.Id, ct) >= partida.Cantidad,
                    409, "Existencias insuficientes para crear el pedido.");
                pedido.Detalles.Add(new DetallePedido
                {
                    Id = Guid.NewGuid(), ProductoId = producto.Id,
                    Cantidad = partida.Cantidad, PrecioUnitario = producto.Precio!.Value
                });
            }
            Total(pedido);
            db.Pedidos.Add(pedido);
            return new EscrituraResultado(pedido.Id, pedido.Estado.ToString());
        }, ct);
    }

    public Task<EscrituraResultado> RegistrarPagoAsync(Guid pedidoId, RegistrarPago datos, CancellationToken ct)
    {
        Validar(datos);
        ValidarId(datos.IdOperacion);
        Exigir(!string.IsNullOrWhiteSpace(datos.ReferenciaExterna), 400, "La referencia es obligatoria.");
        return transacciones.EjecutarAsync(async () =>
        {
            var referencia = datos.ReferenciaExterna.Trim();
            var existente = await db.Pagos.SingleOrDefaultAsync(p => p.Id == datos.IdOperacion || p.ReferenciaExterna == referencia, ct);
            if (existente is not null)
            {
                Exigir(existente.Id == datos.IdOperacion && existente.PedidoId == pedidoId && existente.ReferenciaExterna == referencia,
                    409, "El identificador o referencia ya corresponde a otro pago.");
                return new EscrituraResultado(existente.Id, existente.Estado.ToString());
            }
            var pedido = await CargarPedido(pedidoId, ct);
            Exigir(pedido.Estado == EstadoPedido.PendientePago, 409, "El pedido requiere estar pendiente de pago.");
            var pago = new Pago
            {
                Id = datos.IdOperacion, PedidoId = pedido.Id,
                Importe = Total(pedido), ReferenciaExterna = referencia
            };
            db.Pagos.Add(pago);
            return new EscrituraResultado(pago.Id, pago.Estado.ToString());
        }, ct);
    }

    public Task<EscrituraResultado> ConfirmarPagoAsync(Guid pagoId, CancellationToken ct) =>
        transacciones.EjecutarAsync(async () =>
        {
            var pago = await db.Pagos.SingleOrDefaultAsync(p => p.Id == pagoId, ct);
            Exigir(pago is not null, 404, "Pago inexistente.");
            var pedido = await CargarPedido(pago!.PedidoId, ct);
            if (pago.Estado == EstadoPago.Confirmado)
                return new EscrituraResultado(pago.Id, pago.Estado.ToString());
            Exigir(pago.Estado == EstadoPago.Pendiente && pedido.Estado == EstadoPedido.PendientePago, 409, "El pago o pedido ya cambió de estado.");
            Exigir(pago.Importe == Total(pedido), 409, "El pago debe cubrir el total del pedido.");
            Exigir(!pedido.Pagos.Any(p => p.Estado == EstadoPago.Confirmado), 409, "El pedido ya tiene un pago confirmado.");
            foreach (var detalle in pedido.Detalles)
            {
                var existencia = await Existencia(detalle.ProductoId, ct);
                Exigir(existencia >= detalle.Cantidad, 409, "Existencias insuficientes para confirmar el pago.");
                db.MovimientosInventario.Add(new MovimientoInventario
                {
                    Id = Guid.NewGuid(), ProductoId = detalle.ProductoId, DetallePedidoId = detalle.Id,
                    Tipo = TipoMovimientoInventario.Salida, VariacionCantidad = -detalle.Cantidad,
                    RegistradoEnUtc = DateTime.UtcNow, Motivo = "Pedido pagado"
                });
            }
            pago.Estado = EstadoPago.Confirmado;
            pago.ConfirmadoEnUtc = DateTime.UtcNow;
            pedido.Estado = EstadoPedido.Pagado;
            return new EscrituraResultado(pago.Id, pago.Estado.ToString());
        }, ct);

    public Task<EscrituraResultado> RecogerPedidoAsync(Guid pedidoId, CancellationToken ct) =>
        transacciones.EjecutarAsync(async () =>
        {
            var pedido = await CargarPedido(pedidoId, ct);
            if (pedido.Estado == EstadoPedido.Recogido)
                return new EscrituraResultado(pedido.Id, pedido.Estado.ToString());
            Exigir(pedido.Estado == EstadoPedido.Pagado &&
                pedido.Pagos.Where(p => p.Estado == EstadoPago.Confirmado).Sum(p => p.Importe) == Total(pedido),
                409, "La recogida requiere el pago completo confirmado.");
            pedido.Estado = EstadoPedido.Recogido;
            pedido.RecogidoEnUtc = DateTime.UtcNow;
            return new EscrituraResultado(pedido.Id, pedido.Estado.ToString());
        }, ct);

    public Task<EscrituraResultado> RegistrarMovimientoAsync(Guid productoId, RegistrarMovimiento datos, CancellationToken ct)
    {
        Validar(datos);
        ValidarId(datos.IdOperacion);
        Exigir(!string.IsNullOrWhiteSpace(datos.Motivo), 400, "El motivo es obligatorio.");
        Exigir(datos.VariacionCantidad != 0 &&
            (datos.Tipo == TipoMovimientoInventario.Ajuste ||
             datos.Tipo == TipoMovimientoInventario.Entrada && datos.VariacionCantidad > 0 ||
             datos.Tipo == TipoMovimientoInventario.Salida && datos.VariacionCantidad < 0),
            400, "El signo de la cantidad debe corresponder al tipo de movimiento.");
        return transacciones.EjecutarAsync(async () =>
        {
            var existente = await db.MovimientosInventario.SingleOrDefaultAsync(m => m.Id == datos.IdOperacion, ct);
            if (existente is not null)
            {
                Exigir(existente.ProductoId == productoId && existente.Tipo == datos.Tipo &&
                    existente.VariacionCantidad == datos.VariacionCantidad && existente.Motivo == datos.Motivo.Trim() &&
                    existente.DetallePedidoId is null, 409, "El IdOperacion corresponde a otro movimiento.");
                return new EscrituraResultado(existente.Id, "Registrado");
            }
            Exigir(await db.Productos.AnyAsync(p => p.Id == productoId, ct), 404, "Producto inexistente.");
            Exigir(await Existencia(productoId, ct) + datos.VariacionCantidad >= 0, 409, "El movimiento produciría existencias negativas.");
            db.MovimientosInventario.Add(new MovimientoInventario
            {
                Id = datos.IdOperacion, ProductoId = productoId, Tipo = datos.Tipo,
                VariacionCantidad = datos.VariacionCantidad, Motivo = datos.Motivo.Trim(), RegistradoEnUtc = DateTime.UtcNow
            });
            return new EscrituraResultado(datos.IdOperacion, "Registrado");
        }, ct);
    }

    public Task<EscrituraResultado> RenovarMembresiaAsync(RenovarMembresia datos, CancellationToken ct)
    {
        ValidarId(datos.IdOperacion);
        ValidarId(datos.UsuarioId);
        ValidarId(datos.PagoId);
        Exigir(datos.InicioVigencia != default && datos.FinVigenciaExclusivo > datos.InicioVigencia, 400, "La vigencia requiere un inicio y un fin posterior.");
        return transacciones.EjecutarAsync(async () =>
        {
            var existente = await db.Membresias.SingleOrDefaultAsync(m => m.Id == datos.IdOperacion, ct);
            if (existente is not null)
            {
                Exigir(existente.UsuarioId == datos.UsuarioId && existente.PagoId == datos.PagoId &&
                    existente.InicioVigencia == datos.InicioVigencia && existente.FinVigenciaExclusivo == datos.FinVigenciaExclusivo,
                    409, "El IdOperacion corresponde a otra membresía.");
                return new EscrituraResultado(existente.Id, "Registrada");
            }
            Exigir(await db.PerfilesUsuario.AnyAsync(p => p.Id == datos.UsuarioId && p.Activo, ct), 409, "El usuario requiere un perfil activo.");
            var pago = await db.Pagos.Include(p => p.Pedido).ThenInclude(p => p.Detalles)
                .SingleOrDefaultAsync(p => p.Id == datos.PagoId, ct);
            Exigir(pago is not null, 404, "Pago inexistente.");
            Exigir(pago!.Estado == EstadoPago.Confirmado && pago.Pedido.UsuarioId == datos.UsuarioId &&
                pago.Importe == Total(pago.Pedido), 409, "La membresía requiere un pago completo confirmado del mismo usuario.");
            Exigir(!await db.Membresias.AnyAsync(m => m.PagoId == datos.PagoId, ct), 409, "El pago ya fue utilizado para una membresía.");
            Exigir(!await db.Membresias.AnyAsync(m => m.UsuarioId == datos.UsuarioId &&
                m.InicioVigencia < datos.FinVigenciaExclusivo && datos.InicioVigencia < m.FinVigenciaExclusivo, ct),
                409, "La vigencia se superpone con otra membresía.");
            db.Membresias.Add(new MembresiaCartaq
            {
                Id = datos.IdOperacion, UsuarioId = datos.UsuarioId, PagoId = datos.PagoId,
                InicioVigencia = datos.InicioVigencia, FinVigenciaExclusivo = datos.FinVigenciaExclusivo
            });
            return new EscrituraResultado(datos.IdOperacion, "Registrada");
        }, ct);
    }

    public Task<EscrituraResultado> CancelarPedidoAsync(Guid actorId, Guid pedidoId, CancellationToken ct) =>
        transacciones.EjecutarAsync(async () =>
        {
            var actor = await db.PerfilesUsuario.AsNoTracking().SingleOrDefaultAsync(p => p.Id == actorId && p.Activo, ct);
            Exigir(actor is not null, 403, "El perfil requiere estar activo.");
            var pedido = await CargarPedido(pedidoId, ct);
            Exigir(pedido.UsuarioId == actorId || actor!.Rol is RolUsuario.Administrador or RolUsuario.Recepcion, 404, "Pedido inexistente.");
            if (pedido.Estado == EstadoPedido.Cancelado)
                return new EscrituraResultado(pedido.Id, pedido.Estado.ToString());
            Exigir(pedido.Estado == EstadoPedido.PendientePago &&
                !pedido.Pagos.Any(p => p.Estado == EstadoPago.Confirmado),
                409, "Solo se pueden cancelar pedidos pendientes de pago.");
            pedido.Estado = EstadoPedido.Cancelado;
            foreach (var pago in pedido.Pagos.Where(p => p.Estado == EstadoPago.Pendiente))
                pago.Estado = EstadoPago.Cancelado;
            return new EscrituraResultado(pedido.Id, pedido.Estado.ToString());
        }, ct);

    private async Task<Pedido> CargarPedido(Guid id, CancellationToken ct)
    {
        var pedido = await db.Pedidos.Include(p => p.Detalles).Include(p => p.Pagos).SingleOrDefaultAsync(p => p.Id == id, ct);
        Exigir(pedido is not null, 404, "Pedido inexistente.");
        return pedido!;
    }

    private Task<long> Existencia(Guid productoId, CancellationToken ct) =>
        db.MovimientosInventario.Where(m => m.ProductoId == productoId).SumAsync(m => (long)m.VariacionCantidad, ct);

    private static decimal Total(Pedido pedido)
    {
        var total = pedido.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario);
        Exigir(pedido.Detalles.Count > 0 && pedido.Detalles.All(d => d.Cantidad > 0 && d.PrecioUnitario > 0) &&
            total > 0 && EsImporteValido(total), 409, "El pedido requiere partidas y un importe total válido.");
        return total;
    }

    private static bool EsImporteValido(decimal importe) =>
        importe >= 0 && importe <= ImporteMaximo && decimal.Round(importe, 2) == importe;

    private static void Validar(object datos) =>
        Validator.ValidateObject(datos, new ValidationContext(datos), validateAllProperties: true);

    private static void ValidarId(Guid id) =>
        Exigir(id != Guid.Empty, 400, "El identificador debe ser un UUID válido y distinto de cero.");

    private static void Exigir(bool condicion, int status, string mensaje)
    {
        if (!condicion) throw new OperacionException(status, mensaje);
    }
    public Task<EscrituraResultado> CambiarEstadoProductoAsync(Guid id, CambiarEstadoProducto datos, CancellationToken ct) =>
        transacciones.EjecutarAsync(async () =>
        {
            var producto = await db.Productos.SingleOrDefaultAsync(p => p.Id == id, ct);
            Exigir(producto is not null, 404, "Producto inexistente.");
            producto!.Activo = datos.Activo;
            return new EscrituraResultado(producto.Id, producto.Activo ? "Activo" : "Inactivo");
        }, ct);

    public Task<EscrituraResultado> ConfigurarStockMinimoAsync(Guid id, ConfigurarStockMinimo datos, CancellationToken ct)
    {
        Validar(datos);
        return transacciones.EjecutarAsync(async () =>
        {
            var producto = await db.Productos.SingleOrDefaultAsync(p => p.Id == id, ct);
            Exigir(producto is not null, 404, "Producto inexistente.");
            producto!.StockMinimo = datos.StockMinimo;
            return new EscrituraResultado(producto.Id, "Configurado");
        }, ct);
    }

}
