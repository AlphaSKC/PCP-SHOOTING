using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;

namespace PCP_Shooting.Domain.Services;

public sealed class AdministracionService(PcpShootingDbContext db) : IAdministracionService
{
    public Task<PaginaConsulta<PedidoAdministrativo>> ListarPedidosAsync(FiltroPedidos filtro, CancellationToken ct)
    {
        Validar(filtro);
        var consulta = db.Pedidos.AsNoTracking();
        if (filtro.UsuarioId.HasValue) consulta = consulta.Where(p => p.UsuarioId == filtro.UsuarioId);
        if (filtro.Estado.HasValue) consulta = consulta.Where(p => p.Estado == filtro.Estado);
        if (filtro.DesdeUtc.HasValue) consulta = consulta.Where(p => p.CreadoEnUtc >= filtro.DesdeUtc.Value.UtcDateTime);
        if (filtro.HastaUtc.HasValue) consulta = consulta.Where(p => p.CreadoEnUtc < filtro.HastaUtc.Value.UtcDateTime);
        var busqueda = filtro.Busqueda?.Trim().ToUpperInvariant();
        if (!string.IsNullOrEmpty(busqueda))
            consulta = consulta.Where(p => p.Detalles.Any(d => d.Producto.Sku.ToUpper().Contains(busqueda) ||
                d.Producto.Nombre.ToUpper().Contains(busqueda)));
        return PaginacionConsulta.CrearAsync(consulta.OrderByDescending(p => p.CreadoEnUtc).ThenBy(p => p.Id)
            .Select(p => new PedidoAdministrativo(p.Id, p.UsuarioId, p.CreadoEnUtc, p.Estado,
                p.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario))), filtro, ct);
    }

    public async Task<PedidoDetalleAdministrativo?> ConsultarPedidoAsync(Guid id, CancellationToken ct)
    {
        var pedido = await db.Pedidos.AsNoTracking().Include(p => p.Detalles).Include(p => p.Pagos)
            .SingleOrDefaultAsync(p => p.Id == id, ct);
        if (pedido is null) return null;
        return new PedidoDetalleAdministrativo(
            new PedidoAdministrativo(pedido.Id, pedido.UsuarioId, pedido.CreadoEnUtc, pedido.Estado,
                pedido.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario)),
            pedido.Detalles.OrderBy(d => d.Id)
                .Select(d => new DetallePedidoConsulta(d.ProductoId, d.Cantidad, d.PrecioUnitario)).ToArray(),
            pedido.Pagos.OrderBy(p => p.Id).Select(p => new PagoAdministrativo(p.Id, p.PedidoId,
                pedido.UsuarioId, p.Importe, p.ReferenciaExterna, p.Estado, p.ConfirmadoEnUtc)).ToArray());
    }

    public Task<PaginaConsulta<PagoAdministrativo>> ListarPagosAsync(FiltroPagos filtro, CancellationToken ct)
    {
        Validar(filtro);
        var consulta = db.Pagos.AsNoTracking();
        if (filtro.UsuarioId.HasValue) consulta = consulta.Where(p => p.Pedido.UsuarioId == filtro.UsuarioId);
        if (filtro.PedidoId.HasValue) consulta = consulta.Where(p => p.PedidoId == filtro.PedidoId);
        if (filtro.Estado.HasValue) consulta = consulta.Where(p => p.Estado == filtro.Estado);
        var referencia = filtro.Referencia?.Trim();
        if (!string.IsNullOrEmpty(referencia))
            consulta = consulta.Where(p => p.ReferenciaExterna != null && p.ReferenciaExterna.Contains(referencia));
        return PaginacionConsulta.CrearAsync(consulta.OrderByDescending(p => p.Pedido.CreadoEnUtc).ThenBy(p => p.Id)
            .Select(p => new PagoAdministrativo(p.Id, p.PedidoId, p.Pedido.UsuarioId, p.Importe,
                p.ReferenciaExterna, p.Estado, p.ConfirmadoEnUtc)), filtro, ct);
    }

    public Task<PaginaConsulta<MembresiaHistorial>> HistorialMembresiasAsync(Guid usuarioId, FiltroPagina filtro, CancellationToken ct)
    {
        Validar(filtro);
        return PaginacionConsulta.CrearAsync(db.Membresias.AsNoTracking().Where(m => m.UsuarioId == usuarioId)
            .OrderByDescending(m => m.InicioVigencia).ThenBy(m => m.Id)
            .Select(m => new MembresiaHistorial(m.Id, m.PagoId, m.InicioVigencia, m.FinVigenciaExclusivo)), filtro, ct);
    }

    public Task<PaginaConsulta<AuditoriaConsulta>> ConsultarAuditoriaAsync(FiltroAuditoria filtro, CancellationToken ct)
    {
        Validar(filtro);
        var consulta = db.EventosAuditoria.AsNoTracking();
        if (filtro.ActorId.HasValue) consulta = consulta.Where(e => e.ActorId == filtro.ActorId);
        if (filtro.EntidadId.HasValue) consulta = consulta.Where(e => e.EntidadId == filtro.EntidadId);
        if (!string.IsNullOrWhiteSpace(filtro.Entidad)) consulta = consulta.Where(e => e.Entidad == filtro.Entidad.Trim());
        if (filtro.DesdeUtc.HasValue) consulta = consulta.Where(e => e.FechaUtc >= filtro.DesdeUtc.Value.UtcDateTime);
        if (filtro.HastaUtc.HasValue) consulta = consulta.Where(e => e.FechaUtc < filtro.HastaUtc.Value.UtcDateTime);
        return PaginacionConsulta.CrearAsync(consulta.OrderByDescending(e => e.FechaUtc).ThenBy(e => e.Id)
            .Select(e => new AuditoriaConsulta(e.Id, e.ActorId, e.Entidad, e.EntidadId, e.Accion,
                e.Campos, e.FechaUtc, e.SolicitudId)), filtro, ct);
    }

    private static void Validar(object filtro) =>
        Validator.ValidateObject(filtro, new ValidationContext(filtro), validateAllProperties: true);
}