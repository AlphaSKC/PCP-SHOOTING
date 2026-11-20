using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Domain.Services;
using PCP_Shooting.Persistence.Migrations;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class BlogPostgresTests(PostgresEscriturasFixture fixture) : IClassFixture<PostgresEscriturasFixture>
{
    private static GuardarArticuloBlog Datos(string slug, Guid? version = null) => new()
    {
        Slug = slug, Titulo = "Novedades del club", Resumen = "Noticias y eventos", ContenidoMarkdown = "## Noticias\n- Club",
        Categoria = CategoriaBlog.Club, Estado = EstadoArticuloBlog.Programado, PublicarEnUtc = DateTimeOffset.UtcNow.AddDays(1), Version = version
    };

    [PostgresFact]
    public async Task PublicacionProgramadaFiltrosYArchivoFuncionanConPostgreSql()
    {
        using var factory = new OperacionesFactory(fixture);
        using var admin = factory.Cliente(OperacionesFactory.AdminId);
        var slug = "pg-" + Guid.NewGuid().ToString("N");
        var creado = await admin.PostAsJsonAsync("/api/administracion/blog", Datos(slug));
        Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
        var articulo = (await creado.Content.ReadFromJsonAsync<ArticuloBlogGestion>())!;
        using var publico = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.NotFound, (await publico.GetAsync("/api/blog/" + slug)).StatusCode);
        await using (var db = fixture.CrearContext())
        {
            var guardado = await db.ArticulosBlog.SingleAsync(a => a.Id == articulo.Id);
            guardado.PublicarEnUtc = DateTime.UtcNow.AddDays(-1);
            await db.SaveChangesAsync();
        }
        var pagina = await publico.GetFromJsonAsync<PaginaConsulta<ArticuloBlogResumen>>("/api/blog?categoria=Club&busqueda=NOVEDADES");
        Assert.Contains(pagina!.Elementos, a => a.Id == articulo.Id);
        var gestion = await admin.GetFromJsonAsync<PaginaConsulta<ArticuloBlogGestion>>("/api/administracion/blog?estado=Publicado");
        Assert.Contains(gestion!.Elementos, a => a.Id == articulo.Id && a.Estado == EstadoArticuloBlog.Publicado);
        var archivado = await admin.PutAsJsonAsync("/api/administracion/blog/" + articulo.Id, new GuardarArticuloBlog
        {
            Slug = slug, Titulo = articulo.Titulo, Resumen = articulo.Resumen, ContenidoMarkdown = articulo.ContenidoMarkdown,
            Categoria = articulo.Categoria, Estado = EstadoArticuloBlog.Archivado, Version = articulo.Version
        });
        Assert.Equal(HttpStatusCode.OK, archivado.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await publico.GetAsync("/api/blog/" + slug)).StatusCode);
        await using var comprobacion = fixture.CrearContext();
        Assert.Contains(await comprobacion.EventosAuditoria.Where(e => e.EntidadId == articulo.Id).ToListAsync(), e => e.ActorId == OperacionesFactory.AdminId);
    }

    [PostgresFact]
    public async Task VersionEvitaSobrescrituraAunqueAmbasSesionesYaCargaronElArticulo()
    {
        using var factory = new OperacionesFactory(fixture);
        using var admin = factory.Cliente(OperacionesFactory.AdminId);
        var slug = "version-" + Guid.NewGuid().ToString("N");
        var respuesta = await admin.PostAsJsonAsync("/api/administracion/blog", Datos(slug));
        var articulo = (await respuesta.Content.ReadFromJsonAsync<ArticuloBlogGestion>())!;
        await using var dbUno = fixture.CrearContext();
        await using var dbDos = fixture.CrearContext();
        await dbUno.ArticulosBlog.LoadAsync();
        await dbDos.ArticulosBlog.LoadAsync();
        await new BlogService(dbUno, TimeProvider.System).GuardarAsync(articulo.Id, OperacionesFactory.AdminId, Datos(slug, articulo.Version), default);
        var error = await Assert.ThrowsAsync<OperacionException>(() => new BlogService(dbDos, TimeProvider.System)
            .GuardarAsync(articulo.Id, OperacionesFactory.AdminId, Datos(slug, articulo.Version), default));
        Assert.Equal(409, error.Status);
    }

    [PostgresFact]
    public async Task MigracionCreaTablaProtegidaContraAccesoDirectoYSlugsDuplicados()
    {
        // Solo se reconstruye esta tabla dentro del esquema temporal propio del fixture.
        await using var db = fixture.CrearContext();
        await db.Database.ExecuteSqlRawAsync("DROP TABLE \"ArticulosBlog\"");
        var migracion = new BlogArticulos();
        foreach (var comando in db.GetService<IMigrationsSqlGenerator>().Generate(migracion.UpOperations, db.Model))
            await db.Database.ExecuteSqlRawAsync(comando.CommandText);
        var rls = await db.Database.SqlQueryRaw<bool>("""
            SELECT relrowsecurity AS "Value" FROM pg_class WHERE oid = '"ArticulosBlog"'::regclass
            """).SingleAsync();
        Assert.True(rls);
        var acceso = await db.Database.SqlQueryRaw<bool>("""
            SELECT has_table_privilege('anon', format('%I.%I', current_schema(), 'ArticulosBlog'), 'SELECT')
                OR has_table_privilege('authenticated', format('%I.%I', current_schema(), 'ArticulosBlog'), 'SELECT') AS "Value"
            """).SingleAsync();
        Assert.False(acceso);
        var autor = new PerfilUsuario { Id = Guid.NewGuid(), Nombre = "Autor de prueba" };
        db.PerfilesUsuario.Add(autor);
        await db.SaveChangesAsync();
        ArticuloBlog Articulo() => new() { Id = Guid.NewGuid(), Slug = "slug-unico", Titulo = "Titulo", Resumen = "Resumen",
            ContenidoMarkdown = "Contenido", AutorId = autor.Id, Version = Guid.NewGuid(), CreadoEnUtc = DateTime.UtcNow, ActualizadoEnUtc = DateTime.UtcNow };
        db.ArticulosBlog.Add(Articulo());
        await db.SaveChangesAsync();
        db.ArticulosBlog.Add(Articulo());
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
