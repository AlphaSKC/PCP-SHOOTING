namespace PCP_Shooting.Persistence.Interfaces;

public interface ITransaccionesPersistence
{
    Task<T> EjecutarAsync<T>(Func<Task<T>> operacion, CancellationToken cancellationToken);
}