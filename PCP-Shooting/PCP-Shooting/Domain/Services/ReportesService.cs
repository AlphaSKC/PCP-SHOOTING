using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Services;

public sealed class ReportesService(PcpShootingDbContext db, TimeProvider reloj) : IReportesService
{
    public async Task<Reporte<ReservaReporte>> ReservasAsync(FiltroReporteReservas filtro, bool exportar, CancellationToken ct)
    {
        Validar(filtro);
        var ahora = reloj.GetUtcNow().UtcDateTime;
        var consulta = db.ReservasWeb.AsNoTracking();
        if (filtro.UsuarioId.HasValue) consulta = consulta.Where(r => r.UsuarioId == filtro.UsuarioId);
        if (filtro.DesdeUtc.HasValue) consulta = consulta.Where(r => r.InicioUtc >= filtro.DesdeUtc.Value.UtcDateTime);
        if (filtro.HastaUtc.HasValue) consulta = consulta.Where(r => r.InicioUtc < filtro.HastaUtc.Value.UtcDateTime);
        if (filtro.Estado == EstadoReservaWeb.Pagada) consulta = consulta.Where(r => r.PagoConfirmadoEnUtc != null);
        if (filtro.Estado == EstadoReservaWeb.PendientePago) consulta = consulta.Where(r => r.PagoConfirmadoEnUtc == null &&
            (r.VencePagoEnUtc > ahora || r.Pagos.Any(p => p.Estado == EstadoPago.Pendiente || p.ConciliacionEnCurso)));
        if (filtro.Estado == EstadoReservaWeb.Vencida) consulta = consulta.Where(r => r.PagoConfirmadoEnUtc == null &&
            r.VencePagoEnUtc <= ahora && !r.Pagos.Any(p => p.Estado == EstadoPago.Pendiente || p.ConciliacionEnCurso));
        var confirmado = await consulta.Where(r => r.PagoConfirmadoEnUtc != null).SumAsync(r => (decimal?)r.Total, ct) ?? 0m;
        var filas = consulta.OrderByDescending(r => r.InicioUtc).ThenBy(r => r.Id)
            .Select(r => new ReservaReporte(r.Id, r.UsuarioId, r.Usuario.Nombre, r.Usuario.Telefono,
            r.NombrePaquete, r.Fecha, r.Hora, r.ZonaHoraria, r.InicioUtc,
            r.PagoConfirmadoEnUtc != null ? EstadoReservaWeb.Pagada :
                r.VencePagoEnUtc > ahora || r.Pagos.Any(p => p.Estado == EstadoPago.Pendiente || p.ConciliacionEnCurso)
                    ? EstadoReservaWeb.PendientePago : EstadoReservaWeb.Vencida,
            r.Participantes.Count, r.Total, r.PagoConfirmadoEnUtc));
        return await CrearAsync(filas, filtro, exportar, confirmado, ct);
    }

    public Task<Reporte<PedidoReporte>> PedidosAsync(FiltroPedidos filtro, bool exportar, CancellationToken ct)
    {
        Validar(filtro);
        var consulta = db.Pedidos.AsNoTracking();
        if (filtro.UsuarioId.HasValue) consulta = consulta.Where(p => p.UsuarioId == filtro.UsuarioId);
        if (filtro.Estado.HasValue) consulta = consulta.Where(p => p.Estado == filtro.Estado);
        if (filtro.DesdeUtc.HasValue) consulta = consulta.Where(p => p.CreadoEnUtc >= filtro.DesdeUtc.Value.UtcDateTime);
        if (filtro.HastaUtc.HasValue) consulta = consulta.Where(p => p.CreadoEnUtc < filtro.HastaUtc.Value.UtcDateTime);
        var busqueda = filtro.Busqueda?.Trim().ToUpperInvariant();
        if (!string.IsNullOrEmpty(busqueda)) consulta = consulta.Where(p => p.Detalles.Any(d =>
            d.Producto.Sku.ToUpper().Contains(busqueda) || d.Producto.Nombre.ToUpper().Contains(busqueda)));
        return CrearAsync(consulta.OrderByDescending(p => p.CreadoEnUtc).ThenBy(p => p.Id)
            .Select(p => new PedidoReporte(p.Id, p.UsuarioId, p.CreadoEnUtc, p.Estado,
                p.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario),
                db.PerfilesUsuario.Where(u => u.Id == p.UsuarioId).Select(u => u.Nombre).FirstOrDefault(),
                db.PerfilesUsuario.Where(u => u.Id == p.UsuarioId).Select(u => u.Telefono).FirstOrDefault(),
                p.Detalles.OrderBy(d => d.Id).Select(d => d.Producto.Nombre +
                    (d.Talla == TallaPlayera.S ? " · S" : d.Talla == TallaPlayera.M ? " · M" :
                        d.Talla == TallaPlayera.L ? " · L" : d.Talla == TallaPlayera.XL ? " · XL" : "")).ToArray())), filtro, exportar, null, ct);
    }

    public async Task<Reporte<PagoAdministrativo>> PagosAsync(FiltroReportePagos filtro, bool exportar, CancellationToken ct)
    {
        Validar(filtro);
        var consulta = db.Pagos.AsNoTracking();
        if (filtro.UsuarioId.HasValue) consulta = consulta.Where(p => p.Pedido.UsuarioId == filtro.UsuarioId);
        if (filtro.PedidoId.HasValue) consulta = consulta.Where(p => p.PedidoId == filtro.PedidoId);
        if (filtro.Estado.HasValue) consulta = consulta.Where(p => p.Estado == filtro.Estado);
        if (filtro.DesdeUtc.HasValue) consulta = consulta.Where(p => p.ConfirmadoEnUtc >= filtro.DesdeUtc.Value.UtcDateTime);
        if (filtro.HastaUtc.HasValue) consulta = consulta.Where(p => p.ConfirmadoEnUtc < filtro.HastaUtc.Value.UtcDateTime);
        var referencia = filtro.Referencia?.Trim();
        if (!string.IsNullOrEmpty(referencia)) consulta = consulta.Where(p => p.ReferenciaExterna != null && p.ReferenciaExterna.Contains(referencia));
        var confirmado = await consulta.Where(p => p.Estado == EstadoPago.Confirmado).SumAsync(p => (decimal?)p.Importe, ct) ?? 0m;
        return await CrearAsync(consulta.OrderByDescending(p => p.ConfirmadoEnUtc ?? p.Pedido.CreadoEnUtc).ThenBy(p => p.Id)
            .Select(p => new PagoAdministrativo(p.Id, p.PedidoId, p.Pedido.UsuarioId, p.Importe,
                p.ReferenciaExterna, p.Estado, p.ConfirmadoEnUtc,
                db.PerfilesUsuario.Where(u => u.Id == p.Pedido.UsuarioId).Select(u => u.Nombre).FirstOrDefault())), filtro, exportar, confirmado, ct);
    }

    public async Task<Reporte<MembresiaReporte>> MembresiasAsync(FiltroReporteMembresias filtro, bool exportar, CancellationToken ct)
    {
        Validar(filtro);
        var consulta = db.Membresias.AsNoTracking();
        if (filtro.UsuarioId.HasValue) consulta = consulta.Where(m => m.UsuarioId == filtro.UsuarioId);
        if (filtro.Desde.HasValue) consulta = consulta.Where(m => m.InicioVigencia >= filtro.Desde);
        if (filtro.Hasta.HasValue) consulta = consulta.Where(m => m.InicioVigencia < filtro.Hasta);
        if (filtro.VigenteEn.HasValue) consulta = consulta.Where(m => m.InicioVigencia <= filtro.VigenteEn && m.FinVigenciaExclusivo > filtro.VigenteEn);
        var confirmado = await consulta.Where(m => m.Pago.Estado == EstadoPago.Confirmado).SumAsync(m => (decimal?)m.Pago.Importe, ct) ?? 0m;
        return await CrearAsync(consulta.OrderByDescending(m => m.InicioVigencia).ThenBy(m => m.Id)
            .Select(m => new MembresiaReporte(m.Id, m.UsuarioId, m.PagoId, m.InicioVigencia,
                m.FinVigenciaExclusivo, m.Pago.Estado, m.Pago.Importe,
                db.PerfilesUsuario.Where(u => u.Id == m.UsuarioId).Select(u => u.Nombre).FirstOrDefault())), filtro, exportar, confirmado, ct);
    }

    public Task<Reporte<ExistenciaProducto>> InventarioAsync(FiltroInventario filtro, bool exportar, CancellationToken ct)
    {
        Validar(filtro);
        return CrearAsync(ConsultasInventario.Crear(db, filtro), filtro, exportar, null, ct);
    }

    private static async Task<Reporte<T>> CrearAsync<T>(IQueryable<T> consulta, FiltroPagina filtro,
        bool exportar, decimal? confirmado, CancellationToken ct)
    {
        if (!exportar) return new(await PaginacionConsulta.CrearAsync(consulta, filtro, ct), confirmado);
        // El límite se comprueba con una fila adicional y nunca se entrega un CSV truncado.
        var filas = await consulta.Take(10001).ToListAsync(ct);
        if (filas.Count > 10000) throw new ValidationException("La exportación supera 10000 filas. Acota los filtros y vuelve a intentarlo.");
        return new(new PaginaConsulta<T>(filas, filas.Count, 1, filas.Count), confirmado);
    }

    private static void Validar(object filtro) => Validator.ValidateObject(filtro, new ValidationContext(filtro), true);
}
