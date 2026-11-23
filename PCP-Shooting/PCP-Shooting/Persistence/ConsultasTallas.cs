using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Persistence;

internal static class ConsultasTallas
{
    internal static async Task<Dictionary<Guid, IReadOnlyList<ExistenciaTalla>>> CargarAsync(PcpShootingDbContext db,
        Guid[] productos, CancellationToken ct)
    {
        if (productos.Length == 0) return new();
        var cantidades = await db.MovimientosInventario.AsNoTracking().Where(m => productos.Contains(m.ProductoId) && m.Talla != null)
            .GroupBy(m => new { m.ProductoId, m.Talla })
            .Select(g => new { g.Key.ProductoId, Talla = g.Key.Talla!.Value, Cantidad = g.Sum(m => (long)m.VariacionCantidad) }).ToListAsync(ct);
        var porTalla = cantidades.ToDictionary(c => (c.ProductoId, c.Talla), c => c.Cantidad);
        return productos.ToDictionary(id => id, id => (IReadOnlyList<ExistenciaTalla>)Enum.GetValues<TallaPlayera>()
            .Select(talla => { var cantidad = porTalla.GetValueOrDefault((id, talla)); return new ExistenciaTalla(talla, cantidad, cantidad <= 0); }).ToArray());
    }
}
