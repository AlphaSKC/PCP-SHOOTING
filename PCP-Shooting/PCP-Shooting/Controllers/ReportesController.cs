using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Domain.Services;

namespace PCP_Shooting.Controllers;

[ApiController]
[Route("api/reportes")]
[Authorize(Policy = "Administrador")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ReportesController(IReportesService reportes) : ControllerBase
{
    [HttpGet("reservas")]
    public async Task<IActionResult> Reservas([FromQuery] FiltroReporteReservas filtro, CancellationToken ct,
        [FromQuery, RegularExpression("^(json|csv)$")] string formato = "json")
    {
        var reporte = await reportes.ReservasAsync(filtro, formato == "csv", ct);
        return Responder(reporte, formato, "reservas",
            ["Id", "Folio", "UsuarioId", "NombreCliente", "TelefonoCliente", "NombrePaquete", "Fecha", "Hora", "ZonaHoraria", "InicioUtc", "Estado", "CantidadParticipantes", "TotalReserva", "PagoConfirmadoEnUtc"],
            r => [r.Id, r.Folio, r.UsuarioId, r.NombreCliente, r.TelefonoCliente, r.NombrePaquete, r.Fecha, r.Hora,
                r.ZonaHoraria, r.InicioUtc, r.Estado, r.CantidadParticipantes, r.Total, r.PagoConfirmadoEnUtc]);
    }

    [HttpGet("pedidos")]
    public async Task<IActionResult> Pedidos([FromQuery] FiltroPedidos filtro, CancellationToken ct,
        [FromQuery, RegularExpression("^(json|csv)$")] string formato = "json")
    {
        var reporte = await reportes.PedidosAsync(filtro, formato == "csv", ct);
        return Responder(reporte, formato, "pedidos", ["Id", "UsuarioId", "CreadoEnUtc", "Estado", "TotalPedido", "NombreCliente", "Folio", "Productos"],
            p => [p.Id, p.UsuarioId, p.CreadoEnUtc, p.Estado, p.Total, p.NombreCliente, p.Folio, string.Join("; ", p.Productos)]);
    }

    [HttpGet("pagos")]
    public async Task<IActionResult> Pagos([FromQuery] FiltroReportePagos filtro, CancellationToken ct,
        [FromQuery, RegularExpression("^(json|csv)$")] string formato = "json")
    {
        var reporte = await reportes.PagosAsync(filtro, formato == "csv", ct);
        return Responder(reporte, formato, "pagos", ["Id", "PedidoId", "UsuarioId", "Importe", "ReferenciaExterna", "Estado", "ConfirmadoEnUtc", "NombreCliente", "FolioPedido"],
            p => [p.Id, p.PedidoId, p.UsuarioId, p.Importe, p.ReferenciaExterna, p.Estado, p.ConfirmadoEnUtc, p.NombreCliente, p.PedidoId.ToString("N")[..8].ToUpperInvariant()]);
    }

    [HttpGet("membresias")]
    public async Task<IActionResult> Membresias([FromQuery] FiltroReporteMembresias filtro, CancellationToken ct,
        [FromQuery, RegularExpression("^(json|csv)$")] string formato = "json")
    {
        var reporte = await reportes.MembresiasAsync(filtro, formato == "csv", ct);
        return Responder(reporte, formato, "membresias", ["Id", "UsuarioId", "PagoId", "InicioVigencia", "FinVigenciaExclusivo", "EstadoPago", "ImportePago", "NombreCliente"],
            m => [m.Id, m.UsuarioId, m.PagoId, m.InicioVigencia, m.FinVigenciaExclusivo, m.EstadoPago, m.Importe, m.NombreCliente]);
    }

    [HttpGet("inventario")]
    public async Task<IActionResult> Inventario([FromQuery] FiltroInventario filtro, CancellationToken ct,
        [FromQuery, RegularExpression("^(json|csv)$")] string formato = "json")
    {
        var reporte = await reportes.InventarioAsync(filtro, formato == "csv", ct);
        return Responder(reporte, formato, "inventario", ["ProductoId", "Sku", "Nombre", "Categoria", "Activo", "EsVendible", "Existencia", "StockMinimo", "BajoMinimo"],
            p => [p.ProductoId, p.Sku, p.Nombre, p.Categoria, p.Activo, p.EsVendible, p.Existencia, p.StockMinimo, p.BajoMinimo]);
    }

    private IActionResult Responder<T>(Reporte<T> reporte, string formato, string nombre,
        string[] encabezados, Func<T, object?[]> columnas) => formato == "csv"
        ? File(CsvReporte.Crear(encabezados, reporte.Datos.Elementos, columnas), "text/csv; charset=utf-8", $"reporte-{nombre}.csv")
        : Ok(reporte);
}
