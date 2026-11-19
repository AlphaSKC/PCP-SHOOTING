using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Persistence.Interfaces;

public interface IInventarioPersistence
{
    Task<long> ConsultarExistenciaAsync(Guid productoId, CancellationToken cancellationToken = default);
    Task<List<MovimientoInventario>> ListarMovimientosAsync(Guid productoId, CancellationToken cancellationToken = default);
    void AgregarMovimiento(MovimientoInventario movimiento);
    Task<PaginaConsulta<AlertaInventario>> ConsultarAlertasAsync(FiltroPagina filtro, CancellationToken ct);
    Task<PaginaConsulta<ExistenciaProducto>> ListarAsync(FiltroInventario filtro, CancellationToken ct);
}
