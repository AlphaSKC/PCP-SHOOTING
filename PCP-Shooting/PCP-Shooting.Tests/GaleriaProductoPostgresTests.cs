using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Migrations;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class GaleriaProductoPostgresTests(PostgresEscriturasFixture fixture) : IClassFixture<PostgresEscriturasFixture>
{
    [PostgresFact]
    public async Task ProvisionRealConservaPrincipalesYRestringeAccesoDirecto()
    {
        await using var db=fixture.CrearContext();
        Assert.True(await db.Database.SqlQueryRaw<bool>("""
            SELECT c.relrowsecurity AND NOT has_table_privilege('anon',c.oid,'SELECT')
                AND NOT has_table_privilege('authenticated',c.oid,'SELECT') AS "Value"
            FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='public' AND c.relname='FotosProducto'
            """).SingleAsync());
        Assert.False(await db.Database.SqlQueryRaw<bool>("""
            SELECT EXISTS(SELECT 1 FROM public."Productos" p WHERE p."ImagenUrl" IS NOT NULL AND length(trim(p."ImagenUrl"))>0
                AND NOT EXISTS(SELECT 1 FROM public."FotosProducto" f WHERE f."ProductoId"=p."Id" AND f."Orden"=0 AND f."Url"=p."ImagenUrl")) AS "Value"
            """).SingleAsync());
        Assert.True(await db.Database.SqlQueryRaw<bool>("""
            SELECT EXISTS(SELECT 1 FROM pg_trigger t JOIN pg_class c ON c.oid=t.tgrelid JOIN pg_namespace n ON n.oid=c.relnamespace
                WHERE n.nspname='public' AND c.relname='Productos' AND t.tgname='pcp_sincronizar_fotos_producto' AND t.tgdeferrable AND t.tginitdeferred) AS "Value"
            """).SingleAsync());
    }

    [PostgresFact]
    public async Task MigracionConservaPrincipalConsultasOrdenRetiradaCompatibilidadYBorrado()
    {
        var id=Guid.NewGuid(); const string original="https://ejemplo.test/original.png";
        await using(var db=fixture.CrearContext())
        {
            Assert.Matches("^pcp_test_[a-f0-9]{32}$",await db.Database.SqlQueryRaw<string>("SELECT current_schema() AS \"Value\"").SingleAsync());
            await db.Database.ExecuteSqlRawAsync("DROP TABLE \"FotosProducto\"; ALTER TABLE \"ImagenesProducto\" DROP COLUMN \"AgregarGaleria\"");
            db.Productos.Add(new Producto { Id=id,Sku=id.ToString(),Nombre="Anterior",ImagenUrl=original }); await db.SaveChangesAsync();
            await db.Database.ExecuteSqlRawAsync(EliminacionSeguraProductos.ProteccionReferencias);
            foreach(var cmd in db.GetService<IMigrationsSqlGenerator>().Generate(new GaleriaImagenesProducto().UpOperations,db.Model))
                await db.Database.ExecuteSqlRawAsync(cmd.CommandText);
            Assert.Equal(original,(await db.Set<FotoProducto>().SingleAsync()).Url);
        }
        var storage=new ContenidoImagenesApiTests.StorageSimulado(); using var origen=new OperacionesFactory(fixture);
        using var app=origen.WithWebHostBuilder(b=>b.ConfigureServices(s=>
        {
            s.RemoveAll<IImagenStorage>(); s.AddSingleton<IImagenStorage>(storage);
            s.RemoveAll<IEliminacionImagenStorage>(); s.AddSingleton<IEliminacionImagenStorage>(storage);
        }));
        using var admin=app.CreateClient(); admin.DefaultRequestHeaders.Add("X-Usuario-Prueba",OperacionesFactory.AdminId.ToString());
        admin.DefaultRequestHeaders.Authorization=new("Bearer","prueba");
        var a=Guid.NewGuid(); var b=Guid.NewGuid();
        Assert.Equal(HttpStatusCode.OK,(await GaleriaProductoTests.Subir(admin,id,a,true)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await GaleriaProductoTests.Subir(admin,id,b,true)).StatusCode);
        var ficha=await admin.GetFromJsonAsync<JsonElement>($"/api/productos/{id}");
        var legado=ficha.GetProperty("imagenes")[0].GetProperty("id").GetGuid();
        Assert.Equal(original,ficha.GetProperty("imagenUrl").GetString());
        Assert.Equal(HttpStatusCode.OK,(await admin.PutAsJsonAsync($"/api/productos/{id}/imagenes/orden",new {ids=new[]{b,legado,a}})).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,(await admin.DeleteAsync($"/api/productos/{id}/imagenes/{a}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await GaleriaProductoTests.Subir(admin,id,a,true)).StatusCode);
        foreach(var ruta in new[]{"/api/productos","/api/administracion/productos"})
        {
            var pagina=await admin.GetFromJsonAsync<JsonElement>(ruta);
            var producto=pagina.GetProperty("elementos").EnumerateArray().Single(p=>p.GetProperty("id").GetGuid()==id);
            Assert.Equal(2,producto.GetProperty("imagenes").GetArrayLength());
            Assert.Equal(b,producto.GetProperty("imagenes")[0].GetProperty("id").GetGuid());
        }
        await using(var db=fixture.CrearContext())
        {
            // Simula la escritura del backend publicado anterior, sin usar el servicio nuevo.
            var p=await db.Productos.SingleAsync(p=>p.Id==id); p.ImagenUrl="https://ejemplo.test/nueva.png"; await db.SaveChangesAsync();
        }
        ficha=await admin.GetFromJsonAsync<JsonElement>($"/api/productos/{id}");
        Assert.Equal("https://ejemplo.test/nueva.png",ficha.GetProperty("imagenes")[0].GetProperty("url").GetString());
        Assert.Equal(2,ficha.GetProperty("imagenes").GetArrayLength());
        Assert.Equal(HttpStatusCode.NoContent,(await admin.DeleteAsync($"/api/productos/{id}")).StatusCode);
        Assert.Equal(2,storage.Borradas.Count);
        await using(var db=fixture.CrearContext())
        {
            Assert.False(await db.Set<FotoProducto>().AnyAsync(f=>f.ProductoId==id));
            Assert.Contains(await db.EventosAuditoria.ToListAsync(),e=>e.Entidad=="FotoProducto" && e.Accion=="Deleted" && e.ActorId==OperacionesFactory.AdminId);
        }
    }
}
