using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Domain.Services;
using PCP_Shooting.Persistence.Migrations;
using PCP_Shooting.Persistence.Models;
using PCP_Shooting.Persistence.Services;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class ContenidoImagenesPostgresTests(PostgresEscriturasFixture fixture) : IClassFixture<PostgresEscriturasFixture>
{
    [PostgresFact]
    public async Task MigracionYAsociacionDeImagenesFuncionanConTransaccionesReales()
    {
        await using var db = fixture.CrearContext();
        var esquema = await db.Database.SqlQueryRaw<string>("SELECT current_schema() AS \"Value\"").SingleAsync();
        Assert.StartsWith("pcp_test_", esquema);
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"Productos\" DROP CONSTRAINT \"FK_Productos_MarcasAliadas_MarcaId\"");
        await db.Database.ExecuteSqlRawAsync("DROP TABLE \"CargasImagenContenido\", \"MarcasAliadas\"");
        var migracion = new MarcasYPortadasBlog();
        // La provisión global de buckets/función se verifica después contra el proyecto real, sin sustituirla desde el esquema temporal.
        var operaciones = migracion.UpOperations.Where(o => o is not SqlOperation sql || !sql.Sql.Contains("storage.buckets")).ToArray();
        foreach (var comando in db.GetService<IMigrationsSqlGenerator>().Generate(operaciones, db.Model))
            await db.Database.ExecuteSqlRawAsync(comando.CommandText);
        // La migración histórica crea esas tablas con su forma original. Actualizarlas a F24 antes de usar el modelo actual.
        var actualizaciones = new CategoriasTiendaYMarcas().UpOperations.Where(o => o switch {
            SqlOperation sql => sql.Sql.Contains("UPDATE \"MarcasAliadas\""),
            AddColumnOperation c => c.Table is "MarcasAliadas" or "CargasImagenContenido",
            UpdateDataOperation u => u.Table == "MarcasAliadas",
            CreateIndexOperation i => i.Table is "MarcasAliadas" or "CargasImagenContenido",
            AddCheckConstraintOperation c => c.Table is "MarcasAliadas" or "CargasImagenContenido",
            DropCheckConstraintOperation c => c.Table == "CargasImagenContenido",
            AddForeignKeyOperation f => f.Table == "CargasImagenContenido" || f.Name == "FK_Productos_MarcasAliadas_MarcaId",
            _ => false
        }).ToArray();
        foreach (var comando in db.GetService<IMigrationsSqlGenerator>().Generate(actualizaciones, db.Model))
            await db.Database.ExecuteSqlRawAsync(comando.CommandText);
        Assert.Equal(6,await db.MarcasAliadas.CountAsync());
        Assert.Equal(2,await db.Database.SqlQueryRaw<int>("""
            SELECT count(*)::int AS "Value" FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname=current_schema() AND c.relname IN ('MarcasAliadas','CargasImagenContenido') AND c.relrowsecurity
            AND NOT has_table_privilege('anon',c.oid,'SELECT') AND NOT has_table_privilege('authenticated',c.oid,'SELECT')
            """).SingleAsync());
        var actor = new PerfilUsuario { Id=Guid.NewGuid(),Nombre="Administrador imágenes",Rol=RolUsuario.Administrador };
        var blog = new ArticuloBlog { Id=Guid.NewGuid(),Slug="portada-postgres",Titulo="Prueba",Resumen="Resumen",ContenidoMarkdown="Texto",
            AutorId=actor.Id,Version=Guid.NewGuid(),CreadoEnUtc=DateTime.UtcNow,ActualizadoEnUtc=DateTime.UtcNow };
        db.PerfilesUsuario.Add(actor);db.ArticulosBlog.Add(blog);await db.SaveChangesAsync();
        var marca = await db.MarcasAliadas.OrderBy(m=>m.Orden).FirstAsync();
        var storage = new ContenidoImagenesApiTests.StorageSimulado();
        var servicio = new ImagenesContenidoService(db,new TransaccionesPersistence(db),storage,TimeProvider.System);
        var operacion=Guid.NewGuid();
        async Task<ImagenContenidoResultado> Subir(Guid destino,bool esMarca,Guid id) => await servicio.SubirAsync(actor.Id,destino,esMarca,id,
            new MemoryStream(ImagenesTests.Png),"pixel.png","image/png","token-simulado",default);
        var logo=await Subir(marca.Id,true,operacion);
        Assert.Equal(logo,await Subir(marca.Id,true,operacion));
        Assert.Equal(1,storage.Intentos);
        await new MarcasService(db,TimeProvider.System).QuitarLogoAsync(marca.Id,default);
        Assert.Equal(409,(await Assert.ThrowsAsync<OperacionException>(()=>Subir(marca.Id,true,operacion))).Status);
        var portada=await Subir(blog.Id,false,Guid.NewGuid());
        db.ChangeTracker.Clear();
        var leido=await db.ArticulosBlog.SingleAsync(a=>a.Id==blog.Id);
        Assert.Equal(portada.Url,leido.ImagenUrl);Assert.Equal(EstadoArticuloBlog.Borrador,leido.Estado);
        Assert.Null((await db.MarcasAliadas.SingleAsync(m=>m.Id==marca.Id)).LogoUrl);
        Assert.Equal(2,await db.CargasImagenContenido.CountAsync(c=>c.Disponible));
    }

    [PostgresFact]
    public async Task ProvisionPublicaTieneBucketsYPermisosRestringidos()
    {
        await using var db=fixture.CrearContext();
        Assert.Equal(2,await db.Database.SqlQueryRaw<int>("""
            SELECT count(*)::int AS "Value" FROM storage.buckets
            WHERE public AND ((id='pcp-marcas' AND file_size_limit=2097152) OR (id='pcp-blog' AND file_size_limit=5242880))
                AND allowed_mime_types @> ARRAY['image/png','image/jpeg','image/webp']::text[]
                AND NOT ('image/svg+xml'=ANY(allowed_mime_types))
            """).SingleAsync());
        Assert.False(await db.Database.SqlQueryRaw<bool>("SELECT has_function_privilege('anon','public.pcp_puede_subir_imagen_contenido(text,text)','EXECUTE') AS \"Value\"").SingleAsync());
        Assert.True(await db.Database.SqlQueryRaw<bool>("SELECT has_function_privilege('authenticated','public.pcp_puede_subir_imagen_contenido(text,text)','EXECUTE') AS \"Value\"").SingleAsync());
        Assert.False(await db.Database.SqlQueryRaw<bool>("SELECT public.pcp_puede_subir_imagen_contenido('pcp-marcas','ruta-no-reservada') AS \"Value\"").SingleAsync());
    }
}
