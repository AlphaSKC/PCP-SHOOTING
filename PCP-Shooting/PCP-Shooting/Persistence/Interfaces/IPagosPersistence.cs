using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Persistence.Interfaces;

public interface IPagosPersistence
{
    Task<Pago?> BuscarPorReferenciaAsync(string referencia, CancellationToken cancellationToken = default);
    void Agregar(Pago pago);
}
