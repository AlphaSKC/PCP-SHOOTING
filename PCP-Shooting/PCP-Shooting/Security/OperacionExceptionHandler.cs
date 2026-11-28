using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Security;

public sealed class OperacionExceptionHandler(IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var status = exception switch
        {
            OperacionException operacion => operacion.Status,
            ValidationException => StatusCodes.Status400BadRequest,
            Microsoft.EntityFrameworkCore.DbUpdateException { InnerException: Npgsql.PostgresException
                { ConstraintName: "CK_ImagenProducto_Eliminada" } } => StatusCodes.Status409Conflict,
            _ => 0
        };
        if (status == 0) return false;
        context.Response.StatusCode = status;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails { Status = status, Title = exception is Microsoft.EntityFrameworkCore.DbUpdateException
                ? "La imagen está compartida o fue eliminada; usa otra imagen u oculta el producto." : exception.Message }
        });
    }
}
