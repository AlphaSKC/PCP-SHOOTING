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
            _ => 0
        };
        if (status == 0) return false;
        context.Response.StatusCode = status;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails { Status = status, Title = exception.Message }
        });
    }
}