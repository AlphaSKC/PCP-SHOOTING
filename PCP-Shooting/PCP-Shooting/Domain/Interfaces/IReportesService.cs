using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Domain.Interfaces;

public interface IReportesService
{
    Task<Reporte<PedidoReporte>> PedidosAsync(FiltroPedidos filtro, bool exportar, CancellationToken ct);
    Task<Reporte<PagoAdministrativo>> PagosAsync(FiltroReportePagos filtro, bool exportar, CancellationToken ct);
    Task<Reporte<MembresiaReporte>> MembresiasAsync(FiltroReporteMembresias filtro, bool exportar, CancellationToken ct);
    Task<Reporte<ExistenciaProducto>> InventarioAsync(FiltroInventario filtro, bool exportar, CancellationToken ct);
}
