using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Services;

internal static class FotosProducto
{
    public static async Task<List<FotoProducto>> Cargar(PcpShootingDbContext db, Producto producto, CancellationToken ct)
    {
        var fotos=await db.Set<FotoProducto>().Where(f=>f.ProductoId==producto.Id).OrderBy(f=>f.Orden).ThenBy(f=>f.Id).ToListAsync(ct);
        if(fotos.Count==0 && !string.IsNullOrWhiteSpace(producto.ImagenUrl))
        {
            var foto=new FotoProducto { Id=Guid.NewGuid(),ProductoId=producto.Id,Url=producto.ImagenUrl,Orden=0 };
            db.Add(foto); fotos.Add(foto);
        }
        return fotos;
    }

    public static async Task Reemplazar(PcpShootingDbContext db, Producto producto, string? url, CancellationToken ct, Guid? id=null)
    {
        var fotos=await Cargar(db,producto,ct);
        if(url is null)
        {
            if(fotos.Count>0) throw new PCP_Shooting.Domain.Models.OperacionException(409,"No se puede vaciar la galería desde la ficha. Conserva una imagen principal o reemplázala.");
            producto.ImagenUrl=null; return;
        }
        var elegida=fotos.FirstOrDefault(f=>f.Url==url);
        if(elegida is null)
        {
            if(fotos.Count>0) { db.Remove(fotos[0]); fotos.RemoveAt(0); }
            elegida=new FotoProducto { Id=id??Guid.NewGuid(),ProductoId=producto.Id,Url=url };
            db.Add(elegida);
        }
        else fotos.Remove(elegida);
        fotos.Insert(0,elegida);
        for(var i=0;i<fotos.Count;i++) fotos[i].Orden=i;
        producto.ImagenUrl=url;
    }
}
