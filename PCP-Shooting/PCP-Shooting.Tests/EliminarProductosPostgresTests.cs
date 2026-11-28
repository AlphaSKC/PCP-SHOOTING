using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Migrations;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class EliminarProductosPostgresTests(PostgresEscriturasFixture fixture) : IClassFixture<PostgresEscriturasFixture>
{
    [PostgresFact]
    public async Task ProvisionRealDeStorageYTablasEsPrivadaYRestringida()
    {
        await using var db=fixture.CrearContext();
        Assert.True(await db.Database.SqlQueryRaw<bool>("""
            SELECT EXISTS(SELECT 1 FROM pg_policies WHERE schemaname='storage' AND tablename='objects'
                AND policyname='pcp_productos_eliminar_imagen' AND cmd='DELETE')
                AND EXISTS(SELECT 1 FROM pg_policies WHERE schemaname='storage' AND tablename='objects'
                AND policyname='pcp_productos_consultar_eliminacion' AND cmd='SELECT')
                AND NOT has_function_privilege('anon','public.pcp_puede_eliminar_imagen_producto(text)','EXECUTE')
                AND has_function_privilege('authenticated','public.pcp_puede_eliminar_imagen_producto(text)','EXECUTE') AS "Value"
            """).SingleAsync());
        Assert.False(await db.Database.SqlQueryRaw<bool>("SELECT public.pcp_puede_eliminar_imagen_producto('productos/no-reservada.png') AS \"Value\"").SingleAsync());
        Assert.Equal(2,await db.Database.SqlQueryRaw<int>("""
            SELECT count(*)::int AS "Value" FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname='public' AND c.relname IN ('EliminacionesProducto','ArchivosProductoEliminado') AND c.relrowsecurity
                AND NOT has_table_privilege('anon',c.oid,'SELECT') AND NOT has_table_privilege('authenticated',c.oid,'SELECT')
            """).SingleAsync());
        Assert.Equal(12,await db.Database.SqlQueryRaw<int>("""
            SELECT count(*)::int AS "Value" FROM pg_trigger t JOIN pg_class c ON c.oid=t.tgrelid JOIN pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname='public' AND t.tgname='pcp_proteger_imagen_producto' AND NOT t.tgisinternal
            """).SingleAsync());
    }

    [PostgresFact]
    public async Task MigracionBorradoAuditoriaProteccionDeReferenciasYReintentoReales()
    {
        await using (var db=fixture.CrearContext())
        {
            var esquema=await db.Database.SqlQueryRaw<string>("SELECT current_schema() AS \"Value\"").SingleAsync();
            Assert.Matches("^pcp_test_[a-f0-9]{32}$",esquema);
            await db.Database.ExecuteSqlRawAsync("DROP TABLE \"ArchivosProductoEliminado\", \"EliminacionesProducto\"");
            var operaciones=new EliminacionSeguraProductos().UpOperations
                .Where(o=>o is not SqlOperation s || !s.Sql.Contains("CREATE FUNCTION public.pcp_puede_eliminar_imagen_producto")).ToArray();
            foreach(var cmd in db.GetService<IMigrationsSqlGenerator>().Generate(operaciones,db.Model))
                await db.Database.ExecuteSqlRawAsync(cmd.CommandText);
        }
        var storage=new ContenidoImagenesApiTests.StorageSimulado(); using var origen=new OperacionesFactory(fixture);
        using var app=origen.WithWebHostBuilder(b=>b.ConfigureServices(s=>
        { s.RemoveAll<IEliminacionImagenStorage>(); s.AddSingleton<IEliminacionImagenStorage>(storage); }));
        using var admin=app.CreateClient(); admin.DefaultRequestHeaders.Add("X-Usuario-Prueba",OperacionesFactory.AdminId.ToString());
        admin.DefaultRequestHeaders.Authorization=new("Bearer","prueba");
        var id=Guid.NewGuid(); var otro=Guid.NewGuid(); var ruta=$"productos/{id:N}/{Guid.NewGuid():N}.png";
        await using(var db=fixture.CrearContext())
        {
            db.Productos.AddRange(new Producto { Id=id,Sku=id.ToString(),Nombre="Error",ImagenUrl="https://imagenes.ejemplo.test/"+ruta },
                new Producto { Id=otro,Sku=otro.ToString(),Nombre="Otro" });
            db.ImagenesProducto.Add(new ImagenProducto { Id=Guid.NewGuid(),ProductoId=id,CreadorId=OperacionesFactory.AdminId,
                Ruta=ruta,Sha256=new string('a',64),TipoContenido="image/png",Tamano=12,Disponible=true,CreadaEnUtc=DateTime.UtcNow });
            await db.SaveChangesAsync();
        }
        storage.Fallar=true;
        Assert.Equal(HttpStatusCode.BadGateway,(await admin.DeleteAsync($"/api/productos/{id}")).StatusCode);
        await using(var db=fixture.CrearContext())
        {
            Assert.False(await db.Productos.AnyAsync(p=>p.Id==id)); Assert.False(await db.ImagenesProducto.AnyAsync(i=>i.ProductoId==id));
            Assert.Contains(await db.EventosAuditoria.ToListAsync(),e=>e.EntidadId==id && e.Entidad=="Producto" && e.Accion=="Deleted" && e.ActorId==OperacionesFactory.AdminId);
            var p=await db.Productos.SingleAsync(p=>p.Id==otro); p.NotasMarkdown=$"![foto](https://ejemplo.test/{ruta.Replace("/","%2F")})";
            var error=await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());
            Assert.Equal("CK_ImagenProducto_Eliminada",Assert.IsType<Npgsql.PostgresException>(error.InnerException).ConstraintName);
        }
        Assert.Equal(HttpStatusCode.NoContent,(await admin.DeleteAsync($"/api/productos/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,(await admin.DeleteAsync($"/api/productos/{id}")).StatusCode);
        Assert.Single(storage.Borradas);
        await using(var db=fixture.CrearContext())
        {
            Assert.NotNull((await db.EliminacionesProducto.SingleAsync(e=>e.Id==id)).CompletadaEnUtc);
            // Incluso un movimiento inicial impide el borrado, sin tocar sus registros.
            db.MovimientosInventario.Add(new MovimientoInventario { Id=Guid.NewGuid(),ProductoId=otro,Tipo=TipoMovimientoInventario.Entrada,VariacionCantidad=1,RegistradoEnUtc=DateTime.UtcNow });
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Conflict,(await admin.DeleteAsync($"/api/productos/{otro}")).StatusCode);
        await using(var db=fixture.CrearContext())
        {
            var esquema=await db.Database.SqlQueryRaw<string>("SELECT current_schema() AS \"Value\"").SingleAsync();
            Assert.Matches("^pcp_test_[a-f0-9]{32}$",esquema);
            var sql=new EliminacionSeguraProductos().UpOperations.OfType<SqlOperation>().Single(s=>s.Sql.Contains("CREATE FUNCTION public.pcp_puede_eliminar_imagen_producto")).Sql;
            var funcion=sql[..sql.IndexOf("CREATE POLICY",StringComparison.Ordinal)]
                .Replace("public.pcp_puede_eliminar_imagen_producto",$"\"{esquema}\".pcp_puede_eliminar_imagen_producto")
                .Replace("public.\"",$"\"{esquema}\".\"");
            await db.Database.ExecuteSqlRawAsync(funcion);
            var recepcion=new PerfilUsuario { Id=Guid.NewGuid(),Nombre="Recepcion",Rol=PCP_Shooting.Domain.Interfaces.RolUsuario.Recepcion };
            var baja=new EliminacionProducto { Id=Guid.NewGuid(),ActorId=OperacionesFactory.AdminId,Categoria=CategoriaProducto.Alimento,CreadaEnUtc=DateTime.UtcNow };
            var archivo=new ArchivoProductoEliminado { Id=Guid.NewGuid(),EliminacionId=baja.Id,Ruta=$"productos/{baja.Id:N}/{Guid.NewGuid():N}.png" };
            db.AddRange(recepcion,baja,archivo); await db.SaveChangesAsync();
            await using var tx=await db.Database.BeginTransactionAsync();
            var consulta=$"SELECT \"{esquema}\".pcp_puede_eliminar_imagen_producto({{0}}) AS \"Value\"";
            async Task<bool> Permite(Guid actor)
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"SELECT set_config('request.jwt.claim.sub',{actor.ToString()},true)");
                return await db.Database.SqlQueryRaw<bool>(consulta,archivo.Ruta).SingleAsync();
            }
            Assert.True(await Permite(OperacionesFactory.AdminId));
            Assert.True(await Permite(recepcion.Id));
            baja.Categoria=CategoriaProducto.Playera; await db.SaveChangesAsync();
            Assert.False(await Permite(recepcion.Id)); Assert.True(await Permite(OperacionesFactory.AdminId));
            archivo.EliminadoEnUtc=DateTime.UtcNow; await db.SaveChangesAsync();
            Assert.False(await Permite(OperacionesFactory.AdminId));
            await tx.RollbackAsync();
        }
    }
}
