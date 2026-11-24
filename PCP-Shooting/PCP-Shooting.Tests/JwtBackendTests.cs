using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class JwtBackendTests : IClassFixture<JwtBackendFactory>
{
    private readonly JwtBackendFactory factory;
    public JwtBackendTests(JwtBackendFactory factory) => this.factory = factory;

    [Theory]
    [InlineData("valido", HttpStatusCode.OK)]
    [InlineData("firma", HttpStatusCode.Unauthorized)]
    [InlineData("emisor", HttpStatusCode.Unauthorized)]
    [InlineData("audiencia", HttpStatusCode.Unauthorized)]
    [InlineData("caducado", HttpStatusCode.Unauthorized)]
    [InlineData("sub", HttpStatusCode.Unauthorized)]
    [InlineData("publishable", HttpStatusCode.Unauthorized)]
    public async Task ValidaFirmaEmisorAudienciaVencimientoYSub(string caso, HttpStatusCode esperado)
    {
        using var client = factory.Cliente(factory.Token(JwtBackendFactory.ClienteId, caso));
        Assert.Equal(esperado, (await client.GetAsync("/api/usuarios/yo")).StatusCode);
    }

    [Fact]
    public async Task ClaimsDeRolDelTokenNoConcedenAdministracion()
    {
        using var client = factory.Cliente(factory.Token(JwtBackendFactory.ClienteId, "rol-falso"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/perfiles")).StatusCode);
        var perfil = await client.GetFromJsonAsync<PerfilConsulta>("/api/perfiles/yo");
        Assert.Equal(RolUsuario.Cliente, perfil!.Rol);
    }

    [Fact]
    public async Task RecepcionSeDevuelveComoTextoCompatibleConElFrontend()
    {
        using var client = factory.Cliente(factory.Token(JwtBackendFactory.RecepcionId));
        var response = await client.GetAsync("/api/perfiles/yo");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Recepcion", json.RootElement.GetProperty("rol").GetString());
        var actual = await client.GetFromJsonAsync<UsuarioActual>("/api/usuarios/yo");
        Assert.Equal(RolUsuario.Recepcion, actual!.Rol);
    }

    [Theory]
    [InlineData("/api/administracion/pedidos", HttpStatusCode.OK)]
    [InlineData("/api/administracion/productos", HttpStatusCode.OK)]
    [InlineData("/api/inventario/alertas", HttpStatusCode.OK)]
    [InlineData("/api/administracion/auditoria", HttpStatusCode.Forbidden)]
    [InlineData("/api/administracion/pagos", HttpStatusCode.Forbidden)]
    [InlineData("/api/perfiles", HttpStatusCode.Forbidden)]
    [InlineData("/api/administracion/usuarios/00000000-0000-0000-0000-000000000001/membresias", HttpStatusCode.Forbidden)]
    [InlineData("/api/administracion/productos?categoria=2", HttpStatusCode.Forbidden)]
    [InlineData("/api/inventario/alertas?categoria=2", HttpStatusCode.Forbidden)]
    public async Task RecepcionTieneSoloAccesosOperativos(string ruta, HttpStatusCode esperado)
    {
        using var client = factory.Cliente(factory.Token(JwtBackendFactory.RecepcionId));
        Assert.Equal(esperado, (await client.GetAsync(ruta)).StatusCode);
    }

    [Fact]
    public async Task RecepcionListaSoloAlimentosYAlertasDeAlimentos()
    {
        using var client = factory.Cliente(factory.Token(JwtBackendFactory.RecepcionId));
        var productos = await client.GetFromJsonAsync<PaginaConsulta<ProductoGestion>>("/api/administracion/productos");
        Assert.Equal(CategoriaProducto.Alimento, Assert.Single(productos!.Elementos).Categoria);
        var alertas = await client.GetFromJsonAsync<PaginaConsulta<AlertaInventario>>("/api/inventario/alertas");
        Assert.Equal(JwtBackendFactory.AlimentoId, Assert.Single(alertas!.Elementos).ProductoId);
    }

    [Fact]
    public async Task RecepcionNoPuedeConvertirUnaPlayeraEnAlimentoNiModificarSuStock()
    {
        using var client = factory.Cliente(factory.Token(JwtBackendFactory.RecepcionId));
        var cambio = await client.PutAsJsonAsync("/api/productos/" + JwtBackendFactory.PlayeraId,
            new GuardarProducto { Sku = "PLAYERA", Nombre = "Intento", Categoria = CategoriaProducto.Alimento });
        Assert.Equal(HttpStatusCode.Forbidden, cambio.StatusCode);
        var stock = await client.PostAsJsonAsync($"/api/inventario/{JwtBackendFactory.PlayeraId}/movimientos",
            new RegistrarMovimiento
            {
                IdOperacion = Guid.NewGuid(), Tipo = TipoMovimientoInventario.Entrada,
                VariacionCantidad = 1, Motivo = "Intento"
            });
        Assert.Equal(HttpStatusCode.Forbidden, stock.StatusCode);
    }

    [Fact]
    public async Task PreflightDelFrontendPermiteAuthorizationSinExigirToken()
    {
        using var client = factory.Cliente(factory.Token(JwtBackendFactory.ClienteId));
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/perfiles/yo");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "PUT");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("http://localhost:5173", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Fact]
    public async Task UnClienteNoPuedeCrearPedidosParaOtroUsuario()
    {
        using var client = factory.Cliente(factory.Token(JwtBackendFactory.ClienteId));
        var response = await client.PostAsJsonAsync("/api/pedidos", new CrearPedido
        {
            IdOperacion = Guid.NewGuid(), UsuarioId = JwtBackendFactory.RecepcionId,
            Partidas = new() { new PartidaPedido { ProductoId = JwtBackendFactory.AlimentoId, Cantidad = 1 } }
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
    [Fact]
    public async Task ProxyHttpLocalNoPierdeElTokenPorUnaRedireccion()
    {
        using var desarrollo = new JwtBackendFactory { EnvironmentName = "Development" };
        using var client = desarrollo.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost:5026"), AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            desarrollo.Token(JwtBackendFactory.ClienteId));
        var response = await client.GetAsync("/api/perfiles/yo");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }
    [Fact]
    public async Task PerfilInactivoNoAccedeALasOperaciones()
    {
        using var client = factory.Cliente(factory.Token(JwtBackendFactory.InactivoId));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/administracion/pedidos")).StatusCode);
    }

    [Fact]
    public async Task UsuarioSinPerfilPuedeConsultarSuEstadoParaCrearlo()
    {
        using var client = factory.Cliente(factory.Token(Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/perfiles/yo")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/usuarios/yo")).StatusCode);
    }
}

public sealed class JwtBackendFactory : WebApplicationFactory<Program>
{
    public static readonly Guid ClienteId = Guid.NewGuid();
    public static readonly Guid RecepcionId = Guid.NewGuid();
    public static readonly Guid InactivoId = Guid.NewGuid();
    public static readonly Guid AlimentoId = Guid.NewGuid();
    public static readonly Guid PlayeraId = Guid.NewGuid();
    public string EnvironmentName { get; init; } = "Testing";
    private const string Issuer = "https://itucrxtwphlecugsbauf.supabase.co/auth/v1";
    private readonly ECDsa privateKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    private readonly ECDsa publicKey;
    private readonly DbContextOptions<PcpShootingDbContext> options = new DbContextOptionsBuilder<PcpShootingDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

    public JwtBackendFactory() => publicKey = ECDsa.Create(privateKey.ExportParameters(false));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?> { ["Cors:AllowedOrigins:0"] = "http://localhost:5173" }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<PcpShootingDbContext>();
            services.AddScoped(_ => new PcpShootingDbContext(options));
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
                configuration.SigningKeys.Add(new ECDsaSecurityKey(publicKey) { KeyId = "clave-local-de-prueba" });
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
            });
            using var db = new PcpShootingDbContext(options);
            db.Database.EnsureCreated();
            db.PerfilesUsuario.AddRange(
                new PerfilUsuario { Id = ClienteId, Nombre = "Cliente" },
                new PerfilUsuario { Id = RecepcionId, Nombre = "Recepcion", Rol = RolUsuario.Recepcion },
                new PerfilUsuario { Id = InactivoId, Nombre = "Inactivo", Rol = RolUsuario.Recepcion, Activo = false });
            db.Productos.AddRange(
                new Producto { Id = AlimentoId, Sku = "ALIMENTO", Nombre = "Alimento", Categoria = CategoriaProducto.Alimento, StockMinimo = 1 },
                new Producto { Id = PlayeraId, Sku = "PLAYERA", Nombre = "Playera", Categoria = CategoriaProducto.Playera, StockMinimo = 1 });
            db.SaveChanges();
        });
    }

    public string Token(Guid id, string caso = "valido")
    {
        if (caso == "publishable") return "sb_publishable_no_es_un_token_de_usuario";
        using var otraClave = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var claims = new List<Claim> { new("sub", caso == "sub" ? "invalido" : id.ToString()) };
        if (caso == "rol-falso")
        {
            claims.Add(new Claim("pcp_role", "Administrador"));
            claims.Add(new Claim("role", "Administrador"));
            claims.Add(new Claim("user_metadata", "{\"rol\":\"Administrador\"}"));
        }
        var token = new JwtSecurityToken(
            issuer: caso == "emisor" ? "https://otro-emisor.invalid" : Issuer,
            audience: caso == "audiencia" ? "otra-audiencia" : "authenticated",
            claims: claims, notBefore: DateTime.UtcNow.AddHours(-1),
            expires: caso == "caducado" ? DateTime.UtcNow.AddMinutes(-2) : DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(new ECDsaSecurityKey(caso == "firma" ? otraClave : privateKey)
            {
                KeyId = "clave-local-de-prueba"
            }, SecurityAlgorithms.EcdsaSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public HttpClient Cliente(string token)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            privateKey.Dispose();
            publicKey.Dispose();
        }
    }
}
