using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Domain.Interfaces;

public interface IMembresiasService
{
    Task<MembresiaVigente?> ConsultarVigenteAsync(Guid usuarioId, DateOnly fechaLocal, CancellationToken cancellationToken = default);
}
