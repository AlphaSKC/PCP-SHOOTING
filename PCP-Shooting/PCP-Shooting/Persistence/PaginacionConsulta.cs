using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Persistence;

internal static class PaginacionConsulta
{
    public static async Task<PaginaConsulta<T>> CrearAsync<T>(
        IQueryable<T> consulta, FiltroPagina filtro, CancellationToken ct)
    {
        var total = await consulta.CountAsync(ct);
        var desplazamiento = ((long)filtro.Pagina - 1) * filtro.TamanoPagina;
        if (desplazamiento >= total)
            return new PaginaConsulta<T>(Array.Empty<T>(), total, filtro.Pagina, filtro.TamanoPagina);

        var elementos = await consulta.Skip((int)desplazamiento).Take(filtro.TamanoPagina).ToListAsync(ct);
        return new PaginaConsulta<T>(elementos, total, filtro.Pagina, filtro.TamanoPagina);
    }
}