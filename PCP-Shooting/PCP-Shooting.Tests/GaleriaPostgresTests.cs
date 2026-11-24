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

public sealed class GaleriaPostgresTests(PostgresEscriturasFixture fixture) : IClassFixture<PostgresEscriturasFixture>
{
    [PostgresFact]
    public async Task ProvisionAplicadaTieneBucketPublicoYFuncionRestringida()
    {
        await using var db = fixture.CrearContext();
        Assert.True(await db.Database.SqlQueryRaw<bool>("""
            SELECT EXISTS(SELECT 1 FROM storage.buckets WHERE id='pcp-galeria' AND public
                AND file_size_limit=5242880 AND allowed_mime_types @> ARRAY['image/png','image/jpeg','image/webp']::text[]
                AND NOT ('image/svg+xml'=ANY(allowed_mime_types))) AS "Value"
            """).SingleAsync());
        Assert.False(await db.Database.SqlQueryRaw<bool>("SELECT has_function_privilege('anon','public.pcp_puede_subir_galeria(text)','EXECUTE') AS \"Value\"").SingleAsync());
        Assert.True(await db.Database.SqlQueryRaw<bool>("SELECT has_function_privilege('authenticated','public.pcp_puede_subir_galeria(text)','EXECUTE') AS \"Value\"").SingleAsync());
        Assert.False(await db.Database.SqlQueryRaw<bool>("SELECT public.pcp_puede_subir_galeria('ruta-no-reservada') AS \"Value\"").SingleAsync());
    }
    [PostgresFact]
    public async Task PoliticaDeStorageSoloAceptaReservaDelAdministradorYVersionActual()
    {
        await using var db = fixture.CrearContext();
        var esquema = await db.Database.SqlQueryRaw<string>("SELECT current_schema() AS \"Value\"").SingleAsync();
        Assert.Matches("^pcp_test_[a-f0-9]{32}$", esquema);
        await using var tx = await db.Database.BeginTransactionAsync();
        var sql = new GaleriaInicial().UpOperations.OfType<SqlOperation>().Single(o => o.Sql.Contains("storage.buckets")).Sql
            .Replace("public.", $"\"{esquema}\".").Replace("pcp_galeria_insertar", esquema + "_galeria");
        await db.Database.ExecuteSqlRawAsync(sql);
        var actor = new PerfilUsuario { Id = Guid.NewGuid(), Nombre = "Storage galería", Rol = RolUsuario.Administrador };
        var album = new AlbumGaleria { Id = Guid.NewGuid(), Nombre = "Club", Version = Guid.NewGuid() };
        var foto = new FotoGaleria { Id = Guid.NewGuid(), Album = album, Titulo = "Vista", TextoAlternativo = "Vista", Version = Guid.NewGuid() };
        var carga = new CargaFotoGaleria { Id = Guid.NewGuid(), Foto = foto, ActorId = actor.Id, Ruta = "galeria/prueba/reserva.png",
            Sha256 = new string('a', 64), TipoContenido = "image/png", Tamano = 10, VersionDestino = foto.Version };
        db.Add(actor); db.Add(carga); await db.SaveChangesAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT set_config('request.jwt.claim.sub', {actor.Id.ToString()}, true)");
        async Task<bool> Permite() => await db.Database.SqlQueryRaw<bool>("SELECT pcp_puede_subir_galeria('galeria/prueba/reserva.png') AS \"Value\"").SingleAsync();
        Assert.True(await Permite());
        foto.Version = Guid.NewGuid(); await db.SaveChangesAsync(); Assert.False(await Permite());
        foto.Version = carga.VersionDestino; carga.Disponible = true; await db.SaveChangesAsync(); Assert.False(await Permite());
        carga.Disponible = false; actor.Activo = false; await db.SaveChangesAsync(); Assert.False(await Permite());
        var funcion = esquema + ".pcp_puede_subir_galeria(text)";
        Assert.False(await db.Database.SqlQuery<bool>($"SELECT has_function_privilege('anon', {funcion}, 'EXECUTE') AS \"Value\"").SingleAsync());
        await tx.RollbackAsync(); // También revierte bucket/política temporales, sin modificar la provisión pública.
    }
    [PostgresFact]
    public async Task MigracionCargaPublicacionPortadaYFiltrosPersistenEnPostgreSql()
    {
        await using var db = fixture.CrearContext();
        Assert.StartsWith("pcp_test_", await db.Database.SqlQueryRaw<string>("SELECT current_schema() AS \"Value\"").SingleAsync());
        await db.Database.ExecuteSqlRawAsync("DROP TABLE \"CargasFotosGaleria\", \"FotosGaleria\", \"AlbumsGaleria\"");
        var ops = new GaleriaInicial().UpOperations.Where(o => o is not SqlOperation sql || !sql.Sql.Contains("storage.buckets")).ToArray();
        foreach (var cmd in db.GetService<IMigrationsSqlGenerator>().Generate(ops, db.Model)) await db.Database.ExecuteSqlRawAsync(cmd.CommandText);
        Assert.Equal(3, await db.Database.SqlQueryRaw<int>("""
            SELECT count(*)::int AS "Value" FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname=current_schema() AND c.relname IN ('AlbumsGaleria','FotosGaleria','CargasFotosGaleria')
                AND c.relrowsecurity AND NOT has_table_privilege('anon',c.oid,'SELECT')
                AND NOT has_table_privilege('authenticated',c.oid,'SELECT')
            """).SingleAsync());
        var admin = new PerfilUsuario { Id = Guid.NewGuid(), Nombre = "Prueba galería", Rol = RolUsuario.Administrador };
        db.PerfilesUsuario.Add(admin); await db.SaveChangesAsync();
        var servicio = new GaleriaService(db, TimeProvider.System);
        var album = await servicio.GuardarAlbumAsync(null, new() { Nombre = "Club", Publicado = true }, default);
        var fotos = new List<FotoGaleriaGestion>();
        var storage = new ContenidoImagenesApiTests.StorageSimulado();
        var imagenes = new GaleriaImagenesService(db, new TransaccionesPersistence(db), storage, TimeProvider.System);
        for (var i = 0; i < 2; i++)
        {
            var foto = await servicio.GuardarFotoAsync(null, album.Id, new() { Titulo = "Foto " + i, TextoAlternativo = "Vista " + i, Orden = i }, default);
            var op = Guid.NewGuid();
            async Task<ImagenContenidoResultado> Subir() => await imagenes.SubirAsync(admin.Id, foto.Id, op, new MemoryStream(ImagenesTests.Png), "foto.png", "image/png", "simulado", default);
            var carga = await Subir(); Assert.Equal(carga, await Subir());
            fotos.Add(await servicio.GuardarFotoAsync(foto.Id, null, new() { Titulo = foto.Titulo, TextoAlternativo = foto.TextoAlternativo,
                Orden = i, Publicada = true, Version = carga.Version }, default));
        }
        var publico = Assert.Single((await servicio.AlbumsPublicosAsync(new(), default)).Elementos);
        Assert.Equal(2, publico.CantidadFotos); Assert.Equal(fotos[0].ImagenUrl, publico.PortadaUrl);
        var pagina = await servicio.FotosPublicasAsync(new() { AlbumId = album.Id, Pagina = 2, TamanoPagina = 1 }, default);
        Assert.Equal(2, pagina.Total); Assert.Equal(fotos[1].Id, Assert.Single(pagina.Elementos).Id);
        await servicio.GuardarFotoAsync(fotos[0].Id, null, new() { Titulo = fotos[0].Titulo, TextoAlternativo = fotos[0].TextoAlternativo, Orden = 0, Version = fotos[0].Version }, default);
        Assert.Equal(fotos[1].ImagenUrl, Assert.Single((await servicio.AlbumsPublicosAsync(new(), default)).Elementos).PortadaUrl);
        await servicio.GuardarAlbumAsync(album.Id, new() { Nombre = album.Nombre, Publicado = false, Version = album.Version }, default);
        Assert.Empty((await servicio.FotosPublicasAsync(new(), default)).Elementos);
        Assert.Empty((await servicio.AlbumsPublicosAsync(new(), default)).Elementos);
        Assert.Equal(2, storage.Intentos);
    }
}
