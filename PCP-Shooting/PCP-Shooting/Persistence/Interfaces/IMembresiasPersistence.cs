using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Persistence.Interfaces;

public interface IMembresiasPersistence
{
    Task<MembresiaCartaq?> BuscarVigenteAsync(Guid usuarioId, DateOnly fecha, CancellationToken cancellationToken = default);
    void Agregar(MembresiaCartaq membresia);
}
