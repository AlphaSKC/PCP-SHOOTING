using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence.Models;
namespace PCP_Shooting.Domain.Interfaces;

public sealed record MovimientoConsulta(Guid Id, DateTime RegistradoEnUtc, string Tipo, int VariacionCantidad, string? Motivo,
    Guid? ProductoId = null, string? Sku = null, string? NombreProducto = null, Guid? ActorId = null, string? ActorNombre = null,
    TallaPlayera? Talla = null);

public interface IInventarioService
{
    Task<IReadOnlyList<ExistenciaTalla>> ConsultarTallasAsync(Guid productoId, CancellationToken ct);
    Task<PaginaConsulta<MovimientoConsulta>> ListarMovimientosAsync(FiltroMovimientosInventario filtro, CancellationToken ct);
    Task<long> ConsultarExistenciaAsync(Guid productoId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MovimientoConsulta>> ConsultarHistorialAsync(Guid productoId, CancellationToken cancellationToken = default);
    Task<PaginaConsulta<AlertaInventario>> ConsultarAlertasAsync(FiltroPagina filtro, CancellationToken ct);
    Task<PaginaConsulta<ExistenciaProducto>> ListarAsync(FiltroInventario filtro, CancellationToken ct);
}
