using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Persistence.Services;

public sealed class MembresiasPersistence(PcpShootingDbContext db) : IMembresiasPersistence
{
    public Task<MembresiaCartaq?> BuscarVigenteAsync(Guid usuarioId, DateOnly fecha, CancellationToken cancellationToken = default) =>
        db.Membresias.AsNoTracking()
            .Where(membresia => membresia.UsuarioId == usuarioId && membresia.InicioVigencia <= fecha
                && fecha < membresia.FinVigenciaExclusivo && membresia.Pago.Estado == EstadoPago.Confirmado)
            .OrderByDescending(membresia => membresia.InicioVigencia).ThenBy(membresia => membresia.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public void Agregar(MembresiaCartaq membresia) => db.Membresias.Add(membresia);
}
