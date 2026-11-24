using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Services;

public sealed class AdministracionService(PcpShootingDbContext db, IPerfilesService perfiles, TimeProvider reloj) : IAdministracionService
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
                p.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario),
                db.PerfilesUsuario.Where(u => u.Id == p.UsuarioId).Select(u => u.Nombre).FirstOrDefault(),
                db.PerfilesUsuario.Where(u => u.Id == p.UsuarioId).Select(u => u.Telefono).FirstOrDefault())), filtro, ct);
    }

    public async Task<PedidoDetalleAdministrativo?> ConsultarPedidoAsync(Guid id, CancellationToken ct)
    {
        var pedido = await db.Pedidos.AsNoTracking().Include(p => p.Detalles).Include(p => p.Pagos)
            .SingleOrDefaultAsync(p => p.Id == id, ct);
        if (pedido is null) return null;
        var cliente = await db.PerfilesUsuario.AsNoTracking().Where(u => u.Id == pedido.UsuarioId)
            .Select(u => new { u.Nombre, u.Telefono }).SingleOrDefaultAsync(ct);
        return new PedidoDetalleAdministrativo(
            new PedidoAdministrativo(pedido.Id, pedido.UsuarioId, pedido.CreadoEnUtc, pedido.Estado,
                pedido.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario), cliente?.Nombre, cliente?.Telefono),
            pedido.Detalles.OrderBy(d => d.Id)
                .Select(d => new DetallePedidoConsulta(d.ProductoId, d.Cantidad, d.PrecioUnitario, d.Talla)).ToArray(),
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
                p.ReferenciaExterna, p.Estado, p.ConfirmadoEnUtc,
                db.PerfilesUsuario.Where(u => u.Id == p.Pedido.UsuarioId).Select(u => u.Nombre).FirstOrDefault())), filtro, ct);
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

    public async Task<HistorialCliente?> ConsultarHistorialClienteAsync(
        Guid usuarioId, FiltroHistorialCliente filtro, CancellationToken ct)
    {
        Validar(filtro);
        var perfil = await perfiles.ConsultarAsync(usuarioId, ct);
        if (perfil is null) return null;

        // El DbContext es compartido; estas consultas se ejecutan secuencialmente.
        var pedidos = await ListarPedidosAsync(new FiltroPedidos
        {
            UsuarioId = usuarioId, Pagina = filtro.PaginaPedidos, TamanoPagina = filtro.TamanoPagina
        }, ct);
        var pagos = await ListarPagosAsync(new FiltroPagos
        {
            UsuarioId = usuarioId, Pagina = filtro.PaginaPagos, TamanoPagina = filtro.TamanoPagina
        }, ct);
        var membresias = await HistorialMembresiasAsync(usuarioId, new FiltroPagina
        {
            Pagina = filtro.PaginaMembresias, TamanoPagina = filtro.TamanoPagina
        }, ct);
        var ahora = reloj.GetUtcNow().UtcDateTime;
        var reservas = await PaginacionConsulta.CrearAsync(db.ReservasWeb.AsNoTracking().Where(r => r.UsuarioId == usuarioId)
            .OrderByDescending(r => r.CreadoEnUtc).ThenBy(r => r.Id)
            .Select(r => new ReservaWebResumen(r.Id, r.ApartadoId, r.NombrePaquete, r.InicioUtc, r.VencePagoEnUtc, r.Total,
                r.PagoConfirmadoEnUtc != null ? EstadoReservaWeb.Pagada :
                    r.VencePagoEnUtc > ahora || r.Pagos.Any(p => p.Estado == EstadoPago.Pendiente || p.ConciliacionEnCurso)
                        ? EstadoReservaWeb.PendientePago : EstadoReservaWeb.Vencida,
                r.Participantes.Count)), new FiltroPagina
                {
                    Pagina = filtro.PaginaReservas, TamanoPagina = filtro.TamanoPagina
                }, ct);
        return new HistorialCliente(perfil, pedidos, pagos, membresias, reservas);
    }

    private static void Validar(object filtro) =>
        Validator.ValidateObject(filtro, new ValidationContext(filtro), validateAllProperties: true);
}
