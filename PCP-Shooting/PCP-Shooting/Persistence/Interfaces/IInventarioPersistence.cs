using PCP_Shooting.Domain.Models;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Persistence.Interfaces;

public interface IInventarioPersistence
{
    Task<PaginaConsulta<MovimientoConsulta>> ConsultarMovimientosAsync(FiltroMovimientosInventario filtro, CancellationToken ct);
    Task<List<MovimientoConsulta>> ConsultarHistorialConAutorAsync(Guid productoId, CancellationToken ct);
    Task<long> ConsultarExistenciaAsync(Guid productoId, CancellationToken cancellationToken = default);
    Task<List<MovimientoInventario>> ListarMovimientosAsync(Guid productoId, CancellationToken cancellationToken = default);
    void AgregarMovimiento(MovimientoInventario movimiento);
    Task<PaginaConsulta<AlertaInventario>> ConsultarAlertasAsync(FiltroPagina filtro, CancellationToken ct);
    Task<PaginaConsulta<ExistenciaProducto>> ListarAsync(FiltroInventario filtro, CancellationToken ct);
}
