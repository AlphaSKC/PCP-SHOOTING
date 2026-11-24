using PCP_Shooting.Domain.Models;
namespace PCP_Shooting.Domain.Interfaces;
public interface IContactoService
{
    Task<ContactoRecibido> RecibirAsync(CrearSolicitudContacto datos, CancellationToken ct);
    Task<PaginaConsulta<ContactoResumen>> ListarAsync(FiltroSolicitudesContacto filtro, CancellationToken ct);
    Task<ContactoDetalle?> ConsultarAsync(Guid id, CancellationToken ct);
    Task<ContactoDetalle> ActualizarAsync(Guid id, ActualizarSolicitudContacto datos, CancellationToken ct);
}
