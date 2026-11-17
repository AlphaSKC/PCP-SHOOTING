using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Persistence.Services;

public sealed class PagosPersistence(PcpShootingDbContext db) : IPagosPersistence
{
    public Task<Pago?> BuscarPorReferenciaAsync(string referencia, CancellationToken cancellationToken = default) =>
        db.Pagos.SingleOrDefaultAsync(pago => pago.ReferenciaExterna == referencia, cancellationToken);

    public void Agregar(Pago pago) => db.Pagos.Add(pago);
}
