using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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

public sealed class TiendaCategoriasPostgresTests(PostgresEscriturasFixture fixture) : IClassFixture<PostgresEscriturasFixture>
{
    [PostgresFact]
    public async Task CompatibilidadPermiteCrearMarcasConLaApiAnteriorDuranteElDespliegue()
    {
        await using var db = fixture.CrearContext();
        Assert.StartsWith("pcp_test_", await db.Database.SqlQueryRaw<string>("SELECT current_schema() AS \"Value\"").SingleAsync());
        var ajuste = new CompatibilidadMarcasF24();
        foreach (var cmd in db.GetService<IMigrationsSqlGenerator>().Generate(ajuste.DownOperations, db.Model))
            await db.Database.ExecuteSqlRawAsync(cmd.CommandText);
        var id = Guid.NewGuid();
        Task<int> InsertarComoApiAnterior() => db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"MarcasAliadas\" (\"Id\",\"Nombre\",\"Activo\",\"Orden\",\"Version\",\"CreadoEnUtc\",\"ActualizadoEnUtc\") VALUES ({id},'Marca API previa',true,0,{Guid.NewGuid()},{DateTime.UtcNow},{DateTime.UtcNow})");
        var problema = await Assert.ThrowsAsync<Npgsql.PostgresException>(InsertarComoApiAnterior);
        Assert.Equal("23502", problema.SqlState);
        foreach (var cmd in db.GetService<IMigrationsSqlGenerator>().Generate(ajuste.UpOperations, db.Model))
            await db.Database.ExecuteSqlRawAsync(cmd.CommandText);
        Assert.Equal(1, await InsertarComoApiAnterior());
        var marca = await db.MarcasAliadas.AsNoTracking().SingleAsync(m => m.Id == id);
        Assert.True(marca.VisibleEnCarrusel); Assert.Matches("^marca-[a-f0-9]{32}$", marca.Slug);
    }

    [PostgresFact]
    public async Task ProvisionAplicadaTieneBucketPublicoYPermisosDeCargaRestringidos()
    {
        await using var db = fixture.CrearContext();
        Assert.True(await db.Database.SqlQueryRaw<bool>("""
            SELECT public AND file_size_limit=5242880
                AND allowed_mime_types @> ARRAY['image/jpeg','image/png','image/webp']::text[] AS "Value"
            FROM storage.buckets WHERE id='pcp-tienda'
            """).SingleAsync());
        Assert.True(await db.Database.SqlQueryRaw<bool>("""
            SELECT EXISTS(SELECT 1 FROM pg_policies WHERE schemaname='storage' AND tablename='objects'
                AND policyname='pcp_tienda_insertar_imagen' AND cmd='INSERT')
                AND NOT has_function_privilege('anon','public.pcp_puede_subir_categoria_tienda(text)','EXECUTE')
                AND has_function_privilege('authenticated','public.pcp_puede_subir_categoria_tienda(text)','EXECUTE') AS "Value"
            """).SingleAsync());
        Assert.False(await db.Database.SqlQueryRaw<bool>("SELECT public.pcp_puede_subir_categoria_tienda('tienda/no-reservada.png') AS \"Value\"").SingleAsync());
    }

    [PostgresFact]
    public async Task FuncionStorageSoloPermiteCargaReservadaPorAdministradorActivoYVersionVigente()
    {
        await using var db = fixture.CrearContext();
        var esquema = await db.Database.SqlQueryRaw<string>("SELECT current_schema() AS \"Value\"").SingleAsync();
        Assert.Matches("^pcp_test_[a-f0-9]{32}$", esquema);
        await using var tx = await db.Database.BeginTransactionAsync();
        var provision = new CategoriasTiendaYMarcas().UpOperations.OfType<SqlOperation>().Single(s => s.Sql.Contains("CREATE FUNCTION public.pcp_puede_subir_categoria_tienda")).Sql;
        var inicio = provision.IndexOf("CREATE FUNCTION", StringComparison.Ordinal);
        var fin = provision.IndexOf("CREATE POLICY", StringComparison.Ordinal);
        var funcion = provision[inicio..fin].Replace("public.pcp_puede_subir_categoria_tienda", $"\"{esquema}\".pcp_puede_subir_categoria_tienda")
            .Replace("public.\"", $"\"{esquema}\".\"");
        await db.Database.ExecuteSqlRawAsync(funcion);
        var actor = new PerfilUsuario { Id = Guid.NewGuid(), Nombre = "Gestor Storage", Rol = PCP_Shooting.Domain.Interfaces.RolUsuario.Administrador };
        var categoria = new CategoriaTienda { Id = Guid.NewGuid(), Nombre = "Prueba", Slug = "prueba-storage", Version = Guid.NewGuid(), ActualizadoEnUtc = DateTime.UtcNow };
        var carga = new CargaImagenContenido { Id = Guid.NewGuid(), CategoriaTiendaId = categoria.Id, CreadorId = actor.Id,
            Ruta = $"tienda/{categoria.Id:N}/{Guid.NewGuid():N}.png", Sha256 = new string('a', 64), TipoContenido = "image/png", Tamano = 77,
            VersionDestino = categoria.Version, CreadaEnUtc = DateTime.UtcNow };
        db.AddRange(actor, categoria, carga); await db.SaveChangesAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT set_config('request.jwt.claim.sub',{actor.Id.ToString()},true)");
        // El identificador del esquema está limitado por la expresión regular anterior; la ruta sigue parametrizada.
        var consultaFuncion = $"SELECT \"{esquema}\".pcp_puede_subir_categoria_tienda({{0}}) AS \"Value\"";
        Task<bool> Permite(string ruta) => db.Database.SqlQueryRaw<bool>(consultaFuncion, ruta).SingleAsync();
        Assert.True(await Permite(carga.Ruta)); Assert.False(await Permite("tienda/no-reservada.png"));
        actor.Activo = false; await db.SaveChangesAsync(); Assert.False(await Permite(carga.Ruta));
        actor.Activo = true; categoria.Version = Guid.NewGuid(); await db.SaveChangesAsync(); Assert.False(await Permite(carga.Ruta));
        carga.VersionDestino = categoria.Version; carga.Disponible = true; carga.SubidaEnUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(); Assert.False(await Permite(carga.Ruta));
        await tx.RollbackAsync();
    }

    [PostgresFact]
    public async Task MigracionConservaMarcasYProductosYFlujoCompletoTraduceSql()
    {
        var productoId = Guid.NewGuid(); var marcaAnterior = Guid.NewGuid(); var marcaDuplicada = Guid.NewGuid();
        await using (var db = fixture.CrearContext())
        {
            Assert.StartsWith("pcp_test_", await db.Database.SqlQueryRaw<string>("SELECT current_schema() AS \"Value\"").SingleAsync());
            var migracion = new CategoriasTiendaYMarcas();
            // Solo el esquema temporal. Las operaciones globales de Storage se verifican después de aplicar la migración.
            foreach (var comando in db.GetService<IMigrationsSqlGenerator>().Generate(migracion.DownOperations.Where(o => o is not SqlOperation).ToArray(), db.Model))
                await db.Database.ExecuteSqlRawAsync(comando.CommandText);
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"MarcasAliadas\" (\"Id\",\"Nombre\",\"Activo\",\"Orden\",\"Version\",\"CreadoEnUtc\",\"ActualizadoEnUtc\") VALUES ({marcaAnterior},'Ácme & Club',false,0,{Guid.NewGuid()},{DateTime.UtcNow},{DateTime.UtcNow}),({marcaDuplicada},'Ácme & Club',true,1,{Guid.NewGuid()},{DateTime.UtcNow},{DateTime.UtcNow})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"Productos\" (\"Id\",\"Sku\",\"Nombre\",\"Categoria\",\"Activo\",\"EsVendible\",\"Precio\",\"UsaTallas\") VALUES ({productoId},'F24-PG','Playera PG',2,true,true,200,false)");
            var operaciones = migracion.UpOperations.Where(o => o is not SqlOperation s || !s.Sql.Contains("storage.buckets")).ToArray();
            foreach (var comando in db.GetService<IMigrationsSqlGenerator>().Generate(operaciones, db.Model))
                await db.Database.ExecuteSqlRawAsync(comando.CommandText);
            var marcas = await db.MarcasAliadas.AsNoTracking().ToListAsync();
            Assert.Equal(marcas.Count, marcas.Select(m => m.Slug).Distinct().Count());
            Assert.All(marcas, m => { Assert.Matches("^[a-z0-9]+(-[a-z0-9]+)*$", m.Slug); Assert.Equal(m.Activo, m.VisibleEnCarrusel); });
            Assert.Equal("Ácme & Club", marcas.Single(m => m.Id == marcaAnterior).Nombre);
            var anterior = await db.Productos.AsNoTracking().SingleAsync(p => p.Id == productoId);
            Assert.Null(anterior.MarcaId); Assert.Null(anterior.CategoriaTiendaId); Assert.Equal(CategoriaProducto.Playera, anterior.Categoria);
            Assert.True(await db.Database.SqlQueryRaw<bool>("""
                SELECT c.relrowsecurity AND NOT has_table_privilege('anon',c.oid,'SELECT') AND NOT has_table_privilege('authenticated',c.oid,'SELECT') AS "Value"
                FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
                WHERE n.nspname=current_schema() AND c.relname='CategoriasTienda'
                """).SingleAsync());
        }
        var storage = new ContenidoImagenesApiTests.StorageSimulado();
        using var origen = new OperacionesFactory(fixture);
        using var app = origen.WithWebHostBuilder(b => b.ConfigureServices(s => { s.RemoveAll<IImagenStorage>(); s.AddSingleton<IImagenStorage>(storage); }));
        using var admin = app.CreateClient(); admin.DefaultRequestHeaders.Add("X-Usuario-Prueba", OperacionesFactory.AdminId.ToString()); admin.DefaultRequestHeaders.Authorization = new("Bearer", "prueba");
        var creado = await admin.PostAsJsonAsync("/api/administracion/tienda/categorias", TiendaCategoriasApiTests.Datos());
        Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
        var categoria = await creado.Content.ReadFromJsonAsync<JsonElement>(); var id = categoria.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/productos/{productoId}", new { sku = "F24-PG", nombre = "Playera PG", categoria = 2, esVendible = true, precio = 200, categoriaTiendaId = id, marcaId = marcaDuplicada })).StatusCode);
        var marcaSlug = (await admin.GetFromJsonAsync<JsonElement>("/api/administracion/marcas")).EnumerateArray().Single(m => m.GetProperty("id").GetGuid() == marcaDuplicada).GetProperty("slug").GetString();
        var productos = await admin.GetFromJsonAsync<JsonElement>($"/api/productos?categoriaTienda=ropa&marca={marcaSlug}");
        var producto = Assert.Single(productos.GetProperty("elementos").EnumerateArray());
        Assert.Equal(id, producto.GetProperty("categoriaTienda").GetProperty("id").GetGuid());
        Assert.Equal(marcaDuplicada, (await admin.GetFromJsonAsync<JsonElement>("/api/productos/F24-PG")).GetProperty("marca").GetProperty("id").GetGuid());
        Assert.Equal(1, Assert.Single((await admin.GetFromJsonAsync<JsonElement>("/api/tienda/categorias")).EnumerateArray()).GetProperty("cantidadProductos").GetInt32());
        Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"/api/administracion/tienda/categorias/{id}")).StatusCode);
        using (var form = new MultipartFormDataContent())
        {
            form.Add(new StringContent(Guid.NewGuid().ToString()), "idOperacion");
            var archivo = new ByteArrayContent(ImagenesTests.Png); archivo.Headers.ContentType = new("image/png"); form.Add(archivo, "archivo", "categoria.png");
            Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/administracion/tienda/categorias/{id}/imagen", form)).StatusCode);
        }
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/productos/{productoId}", new { sku = "F24-PG", nombre = "Playera PG", categoria = 2, esVendible = true, precio = 200 })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/administracion/tienda/categorias/{id}")).StatusCode);
        await using var verificar = fixture.CrearContext();
        Assert.Empty(await verificar.CargasImagenContenido.Where(c => c.CategoriaTiendaId == id).ToListAsync());
        Assert.Contains(await verificar.EventosAuditoria.ToListAsync(), e => e.EntidadId == id && e.Accion == "Deleted" && e.ActorId == OperacionesFactory.AdminId);
    }
}
