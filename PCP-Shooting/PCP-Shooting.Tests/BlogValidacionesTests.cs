using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Domain.Services;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class BlogValidacionesTests
{
    [Theory]
    [InlineData("/api/blog?pagina=0")]
    [InlineData("/api/blog?categoria=99")]
    [InlineData("/api/blog?tamanoPagina=101")]
    [InlineData("/api/administracion/blog?estado=99")]
    public async Task FiltrosInvalidosDevuelven400(string ruta)
    {
        using var factory = new PerfilesFactory();
        using var admin = factory.CrearCliente("admin");
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync(ruta)).StatusCode);
    }

    [Theory]
    [InlineData("Programado", -1)]
    [InlineData("Publicado", 1)]
    public async Task EstadoYFechaDebenSerCompatibles(string estado, int dias)
    {
        using var factory = new PerfilesFactory();
        using var admin = factory.CrearCliente("admin");
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/administracion/blog", new
        {
            slug = "fecha-invalida", titulo = "Titulo", resumen = "Resumen", contenidoMarkdown = "Contenido",
            estado, publicarEnUtc = DateTimeOffset.UtcNow.AddDays(dias)
        })).StatusCode);
    }

    [Fact]
    public async Task ProgramacionSeVuelvePublicaExactamenteEnLaFechaSinModificarElRegistro()
    {
        using var factory = new PerfilesFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>();
        var fecha = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var reloj = new RelojBlog(fecha.AddSeconds(-1));
        var servicio = new BlogService(db, reloj);
        var creado = await servicio.GuardarAsync(null, PerfilesFactory.AdminId, new GuardarArticuloBlog
        {
            Slug = "fecha-exacta", Titulo = "Titulo", Resumen = "Resumen", ContenidoMarkdown = "Contenido",
            Estado = EstadoArticuloBlog.Programado, PublicarEnUtc = fecha
        }, default);
        Assert.Null(await servicio.ConsultarPublicoAsync(creado.Slug, default));
        reloj.Ahora = fecha;
        Assert.NotNull(await servicio.ConsultarPublicoAsync(creado.Slug, default));
        Assert.Equal(EstadoArticuloBlog.Publicado, (await servicio.ConsultarGestionAsync(creado.Id, default))!.Estado);
        Assert.Equal(EstadoArticuloBlog.Programado, (await db.ArticulosBlog.FindAsync(creado.Id))!.Estado);
        Assert.Equal(creado.Version, (await servicio.ConsultarGestionAsync(creado.Id, default))!.Version);
    }

    private sealed class RelojBlog(DateTimeOffset ahora) : TimeProvider
    {
        public DateTimeOffset Ahora { get; set; } = ahora;
        public override DateTimeOffset GetUtcNow() => Ahora;
    }
}
