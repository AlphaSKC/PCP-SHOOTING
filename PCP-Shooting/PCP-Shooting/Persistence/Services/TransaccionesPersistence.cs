using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence.Interfaces;

namespace PCP_Shooting.Persistence.Services;

public sealed class TransaccionesPersistence(PcpShootingDbContext db) : ITransaccionesPersistence
{
    public async Task<T> EjecutarAsync<T>(Func<Task<T>> operacion, CancellationToken cancellationToken)
    {
        for (var intento = 0; ; intento++)
        {
            try
            {
                await using var transaccion = await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable, cancellationToken);
                var resultado = await operacion();
                await db.SaveChangesAsync(cancellationToken);
                await transaccion.CommitAsync(cancellationToken);
                return resultado;
            }
            catch (Exception exception) when (EsConflictoTransitorio(exception))
            {
                db.ChangeTracker.Clear();
                if (intento >= 2)
                    throw new OperacionException(409, "La operación coincidió con otro cambio. Reintenta con el mismo IdOperacion.");
                await Task.Delay(TimeSpan.FromMilliseconds(30 * (intento + 1)), cancellationToken);
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: "23505" })
            {
                db.ChangeTracker.Clear();
                throw new OperacionException(409, "Ya existe un registro con el mismo identificador o referencia.");
            }
            catch
            {
                db.ChangeTracker.Clear();
                throw;
            }
        }
    }

    private static bool EsConflictoTransitorio(Exception exception) =>
        exception.GetBaseException() is PostgresException { SqlState: "40001" or "40P01" };
}