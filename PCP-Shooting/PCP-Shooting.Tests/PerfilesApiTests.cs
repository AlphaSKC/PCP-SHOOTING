using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class PerfilesApiTests : IClassFixture<PerfilesFactory>
{
    private readonly PerfilesFactory factory;

    public PerfilesApiTests(PerfilesFactory factory) => this.factory = factory;

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("cliente", HttpStatusCode.Forbidden)]
    [InlineData("inactivo", HttpStatusCode.Forbidden)]
    [InlineData("sin-perfil", HttpStatusCode.Forbidden)]
    [InlineData("admin", HttpStatusCode.OK)]
    public async Task ListadoRequiereAdministradorActivo(string? identidad, HttpStatusCode esperado)
    {
        using var client = factory.CrearCliente(identidad);
        var response = await client.GetAsync("/api/perfiles");
        Assert.Equal(esperado, response.StatusCode);
    }

    [Fact]
    public async Task AdministradorPuedeBuscarYFiltrar()
    {
        using var client = factory.CrearCliente("admin");
        var resultado = await client.GetFromJsonAsync<PaginaPerfiles>(
            "/api/perfiles?busqueda=CLIENTE&rol=0&activo=true&tamanoPagina=1");
        Assert.NotNull(resultado);
        Assert.Equal(1, resultado.Total);
        Assert.Equal("Cliente de prueba", Assert.Single(resultado.Elementos).Nombre);
    }

    [Fact]
    public async Task PaginaFueraDeRangoConservaTotal()
    {
        using var client = factory.CrearCliente("admin");
        var resultado = await client.GetFromJsonAsync<PaginaPerfiles>("/api/perfiles?pagina=2147483647&tamanoPagina=100");
        Assert.NotNull(resultado);
        Assert.Equal(4, resultado.Total);
        Assert.Empty(resultado.Elementos);
    }

    [Theory]
    [InlineData("pagina=0")]
    [InlineData("tamanoPagina=101")]
    [InlineData("tamanoPagina=0")]
    [InlineData("rol=99")]
    public async Task FiltrosInvalidosDevuelvenProblema400(string filtro)
    {
        using var client = factory.CrearCliente("admin");
        var response = await client.GetAsync("/api/perfiles?" + filtro);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("cliente", HttpStatusCode.Forbidden)]
    [InlineData("admin", HttpStatusCode.OK)]
    public async Task ConsultaAjenaRequiereAdministrador(string identidad, HttpStatusCode esperado)
    {
        using var client = factory.CrearCliente(identidad);
        var response = await client.GetAsync("/api/perfiles/" + PerfilesFactory.ClienteId);
        Assert.Equal(esperado, response.StatusCode);
    }

    [Fact]
    public async Task PerfilInexistenteDevuelve404()
    {
        using var client = factory.CrearCliente("admin");
        var response = await client.GetAsync("/api/perfiles/" + Guid.NewGuid());
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ClienteConsultaSuPerfil()
    {
        using var client = factory.CrearCliente("cliente");
        var perfil = await client.GetFromJsonAsync<PerfilConsulta>("/api/perfiles/yo");
        Assert.Equal(PerfilesFactory.ClienteId, perfil?.Id);
    }

    [Fact]
    public async Task ClienteNoPuedeModificarAcceso()
    {
        using var client = factory.CrearCliente("cliente");
        var response = await client.PatchAsJsonAsync("/api/perfiles/" + PerfilesFactory.ClienteId + "/acceso",
            new CambiarAcceso(RolUsuario.Administrador, true));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdministradorNoPuedeModificarSuPropioAcceso()
    {
        using var client = factory.CrearCliente("admin");
        var response = await client.PatchAsJsonAsync("/api/perfiles/" + PerfilesFactory.AdminId + "/acceso",
            new CambiarAcceso(RolUsuario.Cliente, false));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AdministradorPuedeDesactivarOtroPerfilYSeRevocaSuAcceso()
    {
        using var admin = factory.CrearCliente("admin");
        var habilitacion = await admin.PatchAsJsonAsync("/api/perfiles/" + PerfilesFactory.InactivoId + "/acceso",
            new CambiarAcceso(RolUsuario.Cliente, true));
        Assert.Equal(HttpStatusCode.OK, habilitacion.StatusCode);
        using var habilitado = factory.CrearCliente("inactivo");
        Assert.Equal(HttpStatusCode.OK, (await habilitado.GetAsync("/api/usuarios/yo")).StatusCode);

        var response = await admin.PatchAsJsonAsync("/api/perfiles/" + PerfilesFactory.InactivoId + "/acceso",
            new CambiarAcceso(RolUsuario.Cliente, false));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var inactivo = factory.CrearCliente("inactivo");
        Assert.Equal(HttpStatusCode.Forbidden, (await inactivo.GetAsync("/api/usuarios/yo")).StatusCode);
    }

    [Theory]
    [InlineData("/api/productos")]
    [InlineData("/api/pedidos/00000000-0000-0000-0000-000000000001/pagos")]
    [InlineData("/api/pedidos/00000000-0000-0000-0000-000000000001/recogida")]
    [InlineData("/api/pagos/00000000-0000-0000-0000-000000000001/confirmacion")]
    [InlineData("/api/inventario/00000000-0000-0000-0000-000000000001/movimientos")]
    [InlineData("/api/membresias/renovaciones")]
    public async Task EscriturasAdministrativasRechazanClientes(string ruta)
    {
        using var client = factory.CrearCliente("cliente");
        var response = await client.PostAsJsonAsync(ruta, new { });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CrearPedidoRequiereIdentidadAutenticada()
    {
        using var client = factory.CrearCliente(null);
        var response = await client.PostAsJsonAsync("/api/pedidos", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PedidoConIdentificadorVacioDevuelveProblema400()
    {
        using var client = factory.CrearCliente("cliente");
        var response = await client.PostAsJsonAsync("/api/pedidos", new
        {
            partidas = new[] { new { productoId = Guid.NewGuid(), cantidad = 1 } }
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PrecioConMasDeDosDecimalesSeRechaza()
    {
        using var client = factory.CrearCliente("admin");
        var response = await client.PostAsJsonAsync("/api/productos", new
        {
            sku = "PRUEBA", nombre = "Articulo", esVendible = true, precio = 1.001m
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("pedidos")]
    [InlineData("pagos")]
    [InlineData("auditoria")]
    public async Task ConsultasAdministrativasRechazanClientes(string ruta)
    {
        using var client = factory.CrearCliente("cliente");
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.GetAsync("/api/administracion/" + ruta)).StatusCode);
    }

    [Theory]
    [InlineData("pedidos?pagina=0")]
    [InlineData("pedidos?estado=99")]
    [InlineData("pedidos?desdeUtc=2026-02-01T00:00:00Z&hastaUtc=2026-01-01T00:00:00Z")]
    [InlineData("pagos?tamanoPagina=101")]
    [InlineData("pagos?estado=99")]
    [InlineData("auditoria?pagina=0")]
    public async Task FiltrosAdministrativosInvalidosDevuelven400(string ruta)
    {
        using var client = factory.CrearCliente("admin");
        var response = await client.GetAsync("/api/administracion/" + ruta);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task NombreVacioSeRechazaAntesDePersistir()
    {
        using var client = factory.CrearCliente("cliente");
        var response = await client.PutAsJsonAsync("/api/perfiles/yo", new EditarPerfil("   ", null));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public sealed class PerfilesFactory : WebApplicationFactory<Program>
{
    public static readonly Guid AdminId = Guid.NewGuid();
    public static readonly Guid RecepcionId = Guid.NewGuid();
    public static readonly Guid ClienteId = Guid.NewGuid();
    public static readonly Guid InactivoId = Guid.NewGuid();
    private readonly DbContextOptions<PcpShootingDbContext> options =
        new DbContextOptionsBuilder<PcpShootingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:Postgres"] = "Host=unused" }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<PcpShootingDbContext>();
            services.AddScoped(_ => new PcpShootingDbContext(options));
            services.Replace(ServiceDescriptor.Singleton<ISupabaseUsuariosPersistence>(new CuentasSinCorreo()));
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Prueba";
                options.DefaultChallengeScheme = "Prueba";
                options.DefaultForbidScheme = "Prueba";
            }).AddScheme<AuthenticationSchemeOptions, IdentidadSimuladaHandler>("Prueba", _ => { });
            using var db = new PcpShootingDbContext(options);
            db.Database.EnsureCreated();
            db.PerfilesUsuario.AddRange(
                new PerfilUsuario { Id = AdminId, Nombre = "Administrador", Rol = RolUsuario.Administrador },
                new PerfilUsuario { Id = RecepcionId, Nombre = "Recepcion", Rol = RolUsuario.Recepcion },
                new PerfilUsuario { Id = ClienteId, Nombre = "Cliente de prueba", Telefono = "1234567890" },
                new PerfilUsuario { Id = InactivoId, Nombre = "Inactivo", Activo = false });
            db.SaveChanges();
        });
    }

    public HttpClient CrearCliente(string? identidad)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        if (identidad is not null) client.DefaultRequestHeaders.Add("X-Identidad-Prueba", identidad);
        return client;
    }
}

internal sealed class CuentasSinCorreo : ISupabaseUsuariosPersistence
{
    public Task<Guid?> BuscarCuentaConfirmadaAsync(string correo, CancellationToken ct) => Task.FromResult<Guid?>(null);
    public Task<string?> ConsultarCorreoAsync(Guid id, CancellationToken ct) => Task.FromResult<string?>(null);
}

public sealed class IdentidadSimuladaHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var alias = Request.Headers["X-Identidad-Prueba"].ToString();
        if (string.IsNullOrEmpty(alias)) return Task.FromResult(AuthenticateResult.NoResult());
        var id = alias switch
        {
            "admin" => PerfilesFactory.AdminId,
            "recepcion" => PerfilesFactory.RecepcionId,
            "cliente" => PerfilesFactory.ClienteId,
            "inactivo" => PerfilesFactory.InactivoId,
            _ => Guid.NewGuid()
        };
        var identity = new ClaimsIdentity(new[] { new Claim("sub", id.ToString()) }, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
