using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Migrations;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class OrdenesAlimentosPostgresTests(PostgresEscriturasFixture fixture) : IClassFixture<PostgresEscriturasFixture>
{
    [PostgresFact]
    public async Task ProvisionRealTieneTablasPrivadasYMovimientoDeEntregaUnico()
    {
        await using var db=fixture.CrearContext();
        Assert.Equal(3,await db.Database.SqlQueryRaw<int>("""
            SELECT count(*)::int AS "Value" FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname='public' AND c.relname IN ('OrdenesAlimentos','DetallesOrdenAlimentos','OperacionesOrdenAlimentos')
                AND c.relrowsecurity AND NOT has_table_privilege('anon',c.oid,'SELECT') AND NOT has_table_privilege('authenticated',c.oid,'SELECT')
            """).SingleAsync());
        Assert.True(await db.Database.SqlQueryRaw<bool>("""
            SELECT EXISTS(SELECT 1 FROM pg_indexes WHERE schemaname='public' AND tablename='MovimientosInventario'
                AND indexname='IX_MovimientosInventario_DetalleOrdenAlimentosId' AND indexdef LIKE '%UNIQUE%') AS "Value"
            """).SingleAsync());
    }

    [PostgresFact]
    public async Task MigracionFlujoRealAuditoriaEntregaIdempotenteYConsultasPrivadas()
    {
        await using(var db=fixture.CrearContext())
        {
            Assert.Matches("^pcp_test_[a-f0-9]{32}$",await db.Database.SqlQueryRaw<string>("SELECT current_schema() AS \"Value\"").SingleAsync());
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"MovimientosInventario\" DROP COLUMN \"DetalleOrdenAlimentosId\"; DROP TABLE \"OperacionesOrdenAlimentos\",\"DetallesOrdenAlimentos\",\"OrdenesAlimentos\"");
            foreach(var cmd in db.GetService<IMigrationsSqlGenerator>().Generate(new BaseOrdenesAlimentos().UpOperations,db.Model))
                await db.Database.ExecuteSqlRawAsync(cmd.CommandText);
            Assert.Equal(3,await db.Database.SqlQueryRaw<int>("""
                SELECT count(*)::int AS "Value" FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
                WHERE n.nspname=current_schema() AND c.relname IN ('OrdenesAlimentos','DetallesOrdenAlimentos','OperacionesOrdenAlimentos')
                    AND c.relrowsecurity AND NOT has_table_privilege('anon',c.oid,'SELECT') AND NOT has_table_privilege('authenticated',c.oid,'SELECT')
                """).SingleAsync());
        }
        using var origen=new OperacionesFactory(fixture);
        using var app=origen.WithWebHostBuilder(b=>b.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?> { ["Alimentos:OrdenesHabilitadas"]="true" })));
        using var admin=app.CreateClient(); admin.DefaultRequestHeaders.Add("X-Usuario-Prueba",OperacionesFactory.AdminId.ToString());
        using var cliente=app.CreateClient(); cliente.DefaultRequestHeaders.Add("X-Usuario-Prueba",OperacionesFactory.ClienteId.ToString());
        using var otro=app.CreateClient(); otro.DefaultRequestHeaders.Add("X-Usuario-Prueba",OperacionesFactory.OtroId.ToString());
        var producto=Guid.NewGuid();
        await using(var db=fixture.CrearContext())
        {
            db.Productos.Add(new Producto{Id=producto,Sku=producto.ToString(),Nombre="Agua",Categoria=CategoriaProducto.Alimento,EsVendible=true,Precio=25m});
            db.MovimientosInventario.Add(new MovimientoInventario{Id=Guid.NewGuid(),ProductoId=producto,Tipo=TipoMovimientoInventario.Entrada,VariacionCantidad=2,RegistradoEnUtc=DateTime.UtcNow});
            await db.SaveChangesAsync();
        }
        var id=Guid.NewGuid();
        var orden=await Leer(await cliente.PostAsJsonAsync("/api/ordenes-alimentos",new{idOperacion=id,partidas=new[]{new{productoId=producto,cantidad=2}}}));
        Assert.Equal(HttpStatusCode.NotFound,(await otro.GetAsync($"/api/ordenes-alimentos/{id}")).StatusCode);
        var ruta=$"/api/administracion/ordenes-alimentos/{id}";
        foreach(var estado in new[]{"EnPreparacion","Listo"}) orden=await Leer(await admin.PostAsJsonAsync(ruta+"/estado",new{idOperacion=Guid.NewGuid(),version=orden.GetProperty("version").GetGuid(),estado}));
        var cobro=new{idOperacion=Guid.NewGuid(),version=orden.GetProperty("version").GetGuid(),metodo="Transferencia",referencia="SPEI-TEST",importe=50m};
        orden=await Leer(await admin.PostAsJsonAsync(ruta+"/cobro",cobro));
        await Leer(await admin.PostAsJsonAsync(ruta+"/cobro",cobro));
        await using(var db=fixture.CrearContext()) Assert.Equal(2,await db.MovimientosInventario.Where(m=>m.ProductoId==producto).SumAsync(m=>m.VariacionCantidad));
        var entregar=new{idOperacion=Guid.NewGuid(),version=orden.GetProperty("version").GetGuid()};
        orden=await Leer(await admin.PostAsJsonAsync(ruta+"/entrega",entregar));
        Assert.Equal("Entregado",orden.GetProperty("estado").GetString());
        await Leer(await admin.PostAsJsonAsync(ruta+"/entrega",entregar));
        var listado=await cliente.GetFromJsonAsync<JsonElement>("/api/ordenes-alimentos?estado=Entregado&pagada=true&tamanoPagina=1");
        Assert.Equal(1,listado.GetProperty("total").GetInt32());
        Assert.Equal(0,(await otro.GetFromJsonAsync<JsonElement>("/api/ordenes-alimentos")).GetProperty("total").GetInt32());
        await using(var db=fixture.CrearContext())
        {
            Assert.Single(await db.MovimientosInventario.Where(m=>m.DetalleOrdenAlimentosId!=null).ToListAsync());
            Assert.Equal(0,await db.MovimientosInventario.Where(m=>m.ProductoId==producto).SumAsync(m=>m.VariacionCantidad));
            Assert.Equal(4,await db.Set<OperacionOrdenAlimentos>().CountAsync(o=>o.OrdenId==id));
            Assert.Contains(await db.EventosAuditoria.ToListAsync(),e=>e.Entidad=="MovimientoInventario" && e.ActorId==OperacionesFactory.AdminId);
        }
    }

    private static async Task<JsonElement> Leer(HttpResponseMessage respuesta)
    {
        Assert.Equal(HttpStatusCode.OK,respuesta.StatusCode); return await respuesta.Content.ReadFromJsonAsync<JsonElement>();
    }
}
