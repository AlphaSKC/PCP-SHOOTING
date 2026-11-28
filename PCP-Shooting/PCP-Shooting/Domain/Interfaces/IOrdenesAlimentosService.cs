using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Domain.Interfaces;

public interface IOrdenesAlimentosService
{
    Task<OrdenAlimentosConsulta> CrearAsync(Guid actor,CrearOrdenAlimentos datos,bool mostrador,CancellationToken ct);
    Task<OrdenAlimentosConsulta> ConsultarAsync(Guid actor,Guid id,bool operativa,CancellationToken ct);
    Task<PaginaConsulta<OrdenAlimentosConsulta>> ListarAsync(Guid actor,FiltroOrdenesAlimentos filtro,bool operativa,CancellationToken ct);
    Task<OrdenAlimentosConsulta> CambiarEstadoAsync(Guid actor,Guid id,CambiarEstadoOrdenAlimentos datos,CancellationToken ct);
    Task<OrdenAlimentosConsulta> CobrarAsync(Guid actor,Guid id,RegistrarCobroAlimentos datos,CancellationToken ct);
    Task<OrdenAlimentosConsulta> EntregarAsync(Guid actor,Guid id,MutacionOrdenAlimentos datos,CancellationToken ct);
}
