namespace PCP_Shooting.Domain.Interfaces;

public interface IEliminacionProductosService
{
    Task EliminarAsync(Guid actorId, Guid productoId, string? token, CancellationToken ct);
}
