namespace PCP_Shooting.Persistence.Interfaces;

public interface IEliminacionImagenStorage
{
    Task EliminarAsync(string ruta, string token, CancellationToken ct);
}
