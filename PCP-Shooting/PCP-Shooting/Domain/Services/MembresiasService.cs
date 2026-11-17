using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence.Interfaces;

namespace PCP_Shooting.Domain.Services;

public sealed class MembresiasService(IMembresiasPersistence membresias) : IMembresiasService
{
    public async Task<MembresiaVigente?> ConsultarVigenteAsync(
        Guid usuarioId, DateOnly fechaLocal, CancellationToken cancellationToken = default)
    {
        var membresia = await membresias.BuscarVigenteAsync(usuarioId, fechaLocal, cancellationToken);
        return membresia is null
            ? null
            : new MembresiaVigente(membresia.Id, membresia.InicioVigencia, membresia.FinVigenciaExclusivo);
    }
}
