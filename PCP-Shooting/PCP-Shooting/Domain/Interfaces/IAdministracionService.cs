using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Domain.Interfaces;

public interface IAdministracionService
{
    Task<PaginaConsulta<PedidoAdministrativo>> ListarPedidosAsync(FiltroPedidos filtro, CancellationToken ct);
    Task<PedidoDetalleAdministrativo?> ConsultarPedidoAsync(Guid id, CancellationToken ct);
    Task<PaginaConsulta<PagoAdministrativo>> ListarPagosAsync(FiltroPagos filtro, CancellationToken ct);
    Task<PaginaConsulta<MembresiaHistorial>> HistorialMembresiasAsync(Guid usuarioId, FiltroPagina filtro, CancellationToken ct);
    Task<PaginaConsulta<AuditoriaConsulta>> ConsultarAuditoriaAsync(FiltroAuditoria filtro, CancellationToken ct);
    Task<HistorialCliente?> ConsultarHistorialClienteAsync(Guid usuarioId, FiltroHistorialCliente filtro, CancellationToken ct);
}