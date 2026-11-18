using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class OperacionesHttpPostgresTests(PostgresEscriturasFixture fixture)
    : IClassFixture<PostgresEscriturasFixture>
{
    [PostgresFact]
    public async Task FlujoHttpCompletoConAuditoriaYConsultasAdministrativas()
    {
        using var factory = new OperacionesFactory(fixture);
        using var admin = factory.Cliente(OperacionesFactory.AdminId);
        using var cliente = factory.Cliente(OperacionesFactory.ClienteId);
        var producto = await Escribir(admin, "/api/productos", new GuardarProducto
        {
            Sku = Guid.NewGuid().ToString(), Nombre = "Articulo HTTP", Precio = 12.50m, EsVendible = true
        });
        await Escribir(admin, $"/api/inventario/{producto.Id}/movimientos", new RegistrarMovimiento
        {
            IdOperacion = Guid.NewGuid(), Tipo = TipoMovimientoInventario.Entrada,
            VariacionCantidad = 4, Motivo = "Inventario inicial de prueba"
        });
        var pedido = await Escribir(cliente, "/api/pedidos", new CrearPedido
        {
            IdOperacion = Guid.NewGuid(),
            Partidas = new() { new PartidaPedido { ProductoId = producto.Id, Cantidad = 2 } }
        });
        var pedidos = await admin.GetFromJsonAsync<PaginaConsulta<PedidoAdministrativo>>(
            "/api/administracion/pedidos?busqueda=articulo&estado=0&usuarioId=" + OperacionesFactory.ClienteId);
        Assert.Contains(pedidos!.Elementos, p => p.Id == pedido.Id && p.Total == 25m);
        var detalle = await admin.GetFromJsonAsync<PedidoDetalleAdministrativo>("/api/administracion/pedidos/" + pedido.Id);
        Assert.Equal(2, Assert.Single(detalle!.Detalles).Cantidad);
        var referencia = Guid.NewGuid().ToString();
        var pago = await Escribir(admin, $"/api/pedidos/{pedido.Id}/pagos", new RegistrarPago
        {
            IdOperacion = Guid.NewGuid(), ReferenciaExterna = referencia
        });
        await Escribir(admin, $"/api/pagos/{pago.Id}/confirmacion", new { });
        await Escribir(admin, $"/api/pagos/{pago.Id}/confirmacion", new { });
        var pagos = await admin.GetFromJsonAsync<PaginaConsulta<PagoAdministrativo>>(
            "/api/administracion/pagos?estado=1&referencia=" + referencia);
        Assert.Equal(pago.Id, Assert.Single(pagos!.Elementos).Id);
        var renovacion = new RenovarMembresia
        {
            IdOperacion = Guid.NewGuid(), UsuarioId = OperacionesFactory.ClienteId, PagoId = pago.Id,
            InicioVigencia = new DateOnly(2026, 1, 1), FinVigenciaExclusivo = new DateOnly(2027, 1, 1)
        };
        await Escribir(admin, "/api/membresias/renovaciones", renovacion);
        await Escribir(admin, "/api/membresias/renovaciones", renovacion);
        var historial = await cliente.GetFromJsonAsync<PaginaConsulta<MembresiaHistorial>>("/api/membresias/historial");
        Assert.Equal(renovacion.IdOperacion, Assert.Single(historial!.Elementos).Id);
        await Escribir(admin, $"/api/pedidos/{pedido.Id}/recogida", new { });
        var perfil = await admin.PatchAsJsonAsync("/api/perfiles/" + OperacionesFactory.OtroId + "/acceso",
            new CambiarAcceso(RolUsuario.Administrador, true));
        Assert.Equal(HttpStatusCode.OK, perfil.StatusCode);
        var auditoria = await admin.GetFromJsonAsync<PaginaConsulta<AuditoriaConsulta>>(
            $"/api/administracion/auditoria?entidadId={pago.Id}&actorId={OperacionesFactory.AdminId}");
        var confirmacion = Assert.Single(auditoria!.Elementos, e => e.Accion == "Modified");
        Assert.Contains("Estado", confirmacion.Campos);
        Assert.NotNull(confirmacion.SolicitudId);
        var acceso = await admin.GetFromJsonAsync<PaginaConsulta<AuditoriaConsulta>>(
            "/api/administracion/auditoria?entidadId=" + OperacionesFactory.OtroId);
        Assert.Contains(acceso!.Elementos, e => e.ActorId == OperacionesFactory.AdminId && e.Campos.Contains("Rol"));
    }

    [PostgresFact]
    public async Task CancelacionRespetaPropiedadEstadoYReintentos()
    {
        using var factory = new OperacionesFactory(fixture);
        using var admin = factory.Cliente(OperacionesFactory.AdminId);
        using var propietario = factory.Cliente(OperacionesFactory.ClienteId);
        using var otro = factory.Cliente(OperacionesFactory.OtroId);
        var producto = await Escribir(admin, "/api/productos", new GuardarProducto
        {
            Sku = Guid.NewGuid().ToString(), Nombre = "Cancelacion HTTP", Precio = 10, EsVendible = true
        });
        var pedido = await Escribir(propietario, "/api/pedidos", new CrearPedido
        {
            IdOperacion = Guid.NewGuid(), Partidas = new() { new PartidaPedido { ProductoId = producto.Id, Cantidad = 1 } }
        });
        var pago = await Escribir(admin, $"/api/pedidos/{pedido.Id}/pagos", new RegistrarPago
        {
            IdOperacion = Guid.NewGuid(), ReferenciaExterna = Guid.NewGuid().ToString()
        });
        var ajeno = await otro.PostAsJsonAsync($"/api/pedidos/{pedido.Id}/cancelacion", new { });
        Assert.Equal(HttpStatusCode.NotFound, ajeno.StatusCode);
        var primera = await Escribir(propietario, $"/api/pedidos/{pedido.Id}/cancelacion", new { });
        var segunda = await Escribir(propietario, $"/api/pedidos/{pedido.Id}/cancelacion", new { });
        Assert.Equal("Cancelado", primera.Estado);
        Assert.Equal(primera, segunda);
        Assert.Equal(HttpStatusCode.Conflict,
            (await admin.PostAsJsonAsync($"/api/pagos/{pago.Id}/confirmacion", new { })).StatusCode);
        var detalle = await admin.GetFromJsonAsync<PedidoDetalleAdministrativo>("/api/administracion/pedidos/" + pedido.Id);
        Assert.Equal(EstadoPago.Cancelado, Assert.Single(detalle!.Pagos).Estado);
        var eventos = await admin.GetFromJsonAsync<PaginaConsulta<AuditoriaConsulta>>(
            "/api/administracion/auditoria?entidadId=" + pedido.Id);
        Assert.Single(eventos!.Elementos, e => e.Accion == "Modified" && e.ActorId == OperacionesFactory.ClienteId);
    }

    [PostgresFact]
    public async Task ConsultasAdministrativasYHistorialNoExponenDatosDeOtroCliente()
    {
        using var factory = new OperacionesFactory(fixture);
        using var cliente = factory.Cliente(OperacionesFactory.ClienteId);
        using var otro = factory.Cliente(OperacionesFactory.OtroId);
        foreach (var ruta in new[] { "pedidos", "pagos", "auditoria", "usuarios/" + OperacionesFactory.OtroId + "/membresias" })
            Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/administracion/" + ruta)).StatusCode);
        var historial = await otro.GetFromJsonAsync<PaginaConsulta<MembresiaHistorial>>(
            "/api/membresias/historial?usuarioId=" + OperacionesFactory.ClienteId);
        Assert.Empty(historial!.Elementos);
    }


    [PostgresFact]
    public async Task DesactivacionConservaPedidosYMinimosControlanLasAlertas()
    {
        using var factory = new OperacionesFactory(fixture);
        using var admin = factory.Cliente(OperacionesFactory.AdminId);
        using var cliente = factory.Cliente(OperacionesFactory.ClienteId);
        using var publico = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        var sku = Guid.NewGuid().ToString();
        var producto = await Escribir(admin, "/api/productos", new GuardarProducto
        {
            Sku = sku, Nombre = "Articulo para desactivar", Precio = 15, EsVendible = true
        });
        Assert.Equal(HttpStatusCode.OK, (await publico.GetAsync("/api/productos/" + sku)).StatusCode);
        await Escribir(admin, $"/api/inventario/{producto.Id}/movimientos", new RegistrarMovimiento
        {
            IdOperacion = Guid.NewGuid(), Tipo = TipoMovimientoInventario.Entrada,
            VariacionCantidad = 5, Motivo = "Prueba"
        });
        Assert.Equal(HttpStatusCode.OK,
            (await admin.PutAsJsonAsync($"/api/inventario/{producto.Id}/minimo", new { stockMinimo = 6 })).StatusCode);
        var alertas = await admin.GetFromJsonAsync<PaginaConsulta<AlertaInventario>>("/api/inventario/alertas");
        Assert.Contains(alertas!.Elementos, p => p.ProductoId == producto.Id && p.Existencia == 5);
        var pendiente = await Escribir(cliente, "/api/pedidos", new CrearPedido
        {
            IdOperacion = Guid.NewGuid(),
            Partidas = new() { new PartidaPedido { ProductoId = producto.Id, Cantidad = 1 } }
        });
        Assert.Equal(HttpStatusCode.OK,
            (await admin.PatchAsJsonAsync($"/api/productos/{producto.Id}/estado", new { activo = false })).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await admin.PatchAsJsonAsync($"/api/productos/{producto.Id}/estado", new { activo = false })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await publico.GetAsync("/api/productos/" + sku)).StatusCode);
        var catalogo = await publico.GetFromJsonAsync<PaginaConsulta<ProductoCatalogo>>("/api/productos?busqueda=" + sku + "&activo=false");
        Assert.Empty(catalogo!.Elementos);
        var gestion = await admin.GetFromJsonAsync<PaginaConsulta<ProductoGestion>>(
            "/api/administracion/productos?activo=false&busqueda=" + sku);
        Assert.Equal(producto.Id, Assert.Single(gestion!.Elementos).Id);
        Assert.False(gestion.Elementos[0].Activo);
        var rechazado = await cliente.PostAsJsonAsync("/api/pedidos", new CrearPedido
        {
            IdOperacion = Guid.NewGuid(),
            Partidas = new() { new PartidaPedido { ProductoId = producto.Id, Cantidad = 1 } }
        });
        Assert.Equal(HttpStatusCode.Conflict, rechazado.StatusCode);
        var pago = await Escribir(admin, $"/api/pedidos/{pendiente.Id}/pagos", new RegistrarPago
        {
            IdOperacion = Guid.NewGuid(), ReferenciaExterna = Guid.NewGuid().ToString()
        });
        await Escribir(admin, $"/api/pagos/{pago.Id}/confirmacion", new { });
        var conservado = await cliente.GetFromJsonAsync<PedidoConsulta>("/api/pedidos/" + pendiente.Id);
        Assert.Equal("Pagado", conservado!.Resumen.Estado);
        var sinInactivo = await admin.GetFromJsonAsync<PaginaConsulta<AlertaInventario>>("/api/inventario/alertas");
        Assert.DoesNotContain(sinInactivo!.Elementos, p => p.ProductoId == producto.Id);
        Assert.Equal(HttpStatusCode.OK,
            (await admin.PatchAsJsonAsync($"/api/productos/{producto.Id}/estado", new { activo = true })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await publico.GetAsync("/api/productos/" + sku)).StatusCode);
        var reactivado = await admin.GetFromJsonAsync<PaginaConsulta<AlertaInventario>>("/api/inventario/alertas");
        Assert.Contains(reactivado!.Elementos, p => p.ProductoId == producto.Id && p.Existencia == 4);
        Assert.Equal(HttpStatusCode.OK,
            (await admin.PutAsJsonAsync($"/api/inventario/{producto.Id}/minimo", new { stockMinimo = (int?)null })).StatusCode);
        var deshabilitada = await admin.GetFromJsonAsync<PaginaConsulta<AlertaInventario>>("/api/inventario/alertas");
        Assert.DoesNotContain(deshabilitada!.Elementos, p => p.ProductoId == producto.Id);
        var auditoria = await admin.GetFromJsonAsync<PaginaConsulta<AuditoriaConsulta>>(
            "/api/administracion/auditoria?entidadId=" + producto.Id);
        Assert.Equal(2, auditoria!.Elementos.Count(e => e.Accion == "Modified" && e.Campos.Contains("Activo")));
    }

    [PostgresFact]
    public async Task AlimentosYPlayerasConTallasTienenExistenciasIndependientes()
    {
        using var factory = new OperacionesFactory(fixture);
        using var admin = factory.Cliente(OperacionesFactory.AdminId);
        using var cliente = factory.Cliente(OperacionesFactory.ClienteId);
        var skuAlimento = Guid.NewGuid().ToString();
        var notas = "- Presentación individual\n- Características de ejemplo";
        var alimento = await Escribir(admin, "/api/productos", new GuardarProducto
        {
            Sku = skuAlimento, Nombre = "Alimento de prueba", Categoria = CategoriaProducto.Alimento,
            ImagenUrl = "https://example.com/alimento.jpg", NotasMarkdown = notas
        });
        var ficha = await cliente.GetFromJsonAsync<FichaProducto>("/api/productos/" + skuAlimento);
        Assert.Equal(notas, ficha!.NotasMarkdown);
        Assert.Equal(CategoriaProducto.Alimento, ficha.Categoria);
        var completar = await admin.PutAsJsonAsync("/api/productos/" + alimento.Id, new GuardarProducto
        {
            Sku = skuAlimento, Nombre = "Alimento de prueba", Categoria = CategoriaProducto.Alimento,
            ImagenUrl = "https://example.com/alimento.jpg", NotasMarkdown = notas,
            EsVendible = true, Precio = 35
        });
        Assert.Equal(HttpStatusCode.OK, completar.StatusCode);

        async Task<Guid> CrearPlayera(string talla)
        {
            var playera = await Escribir(admin, "/api/productos", new GuardarProducto
            {
                Sku = Guid.NewGuid().ToString(), Nombre = "Playera " + talla,
                Categoria = CategoriaProducto.Playera, EsVendible = true, Precio = 180,
                ImagenUrl = "https://example.com/playera.jpg", NotasMarkdown = "- Color negro",
                EspecificacionesTecnicas = System.Text.Json.JsonSerializer.SerializeToElement(new { talla, color = "Negro" })
            });
            return playera.Id;
        }
        var playeraM = await CrearPlayera("M");
        var playeraL = await CrearPlayera("L");
        foreach (var id in new[] { alimento.Id, playeraM, playeraL })
            await Escribir(admin, $"/api/inventario/{id}/movimientos", new RegistrarMovimiento
            {
                IdOperacion = Guid.NewGuid(), Tipo = TipoMovimientoInventario.Entrada,
                VariacionCantidad = 3, Motivo = "Inventario de prueba"
            });
        var pedido = await Escribir(cliente, "/api/pedidos", new CrearPedido
        {
            IdOperacion = Guid.NewGuid(),
            Partidas = new()
            {
                new PartidaPedido { ProductoId = alimento.Id, Cantidad = 2 },
                new PartidaPedido { ProductoId = playeraM, Cantidad = 1 }
            }
        });
        var pago = await Escribir(admin, $"/api/pedidos/{pedido.Id}/pagos", new RegistrarPago
        {
            IdOperacion = Guid.NewGuid(), ReferenciaExterna = Guid.NewGuid().ToString()
        });
        await Escribir(admin, $"/api/pagos/{pago.Id}/confirmacion", new { });
        await Escribir(admin, $"/api/pedidos/{pedido.Id}/recogida", new { });
        Assert.Equal(1, await admin.GetFromJsonAsync<long>($"/api/inventario/{alimento.Id}/existencia"));
        Assert.Equal(2, await admin.GetFromJsonAsync<long>($"/api/inventario/{playeraM}/existencia"));
        Assert.Equal(3, await admin.GetFromJsonAsync<long>($"/api/inventario/{playeraL}/existencia"));
        var detalle = await admin.GetFromJsonAsync<PedidoDetalleAdministrativo>("/api/administracion/pedidos/" + pedido.Id);
        Assert.Equal(250m, detalle!.Pedido.Total);
        Assert.Equal(EstadoPedido.Recogido, detalle.Pedido.Estado);
        var catalogo = await cliente.GetFromJsonAsync<PaginaConsulta<ProductoCatalogo>>(
            "/api/productos?categoria=1&busqueda=" + skuAlimento);
        Assert.Equal(alimento.Id, Assert.Single(catalogo!.Elementos).Id);
    }

    [PostgresFact]
    public async Task ReplicaDecorativaSeGestionaComoPiezaDeAmbientacion()
    {
        using var factory = new OperacionesFactory(fixture);
        using var admin = factory.Cliente(OperacionesFactory.AdminId);
        using var cliente = factory.Cliente(OperacionesFactory.ClienteId);
        var sku = Guid.NewGuid().ToString();
        var datos = new GuardarProducto
        {
            Sku = sku, Nombre = "Escultura de ambientación",
            Categoria = CategoriaProducto.ReplicaDecorativa,
            ImagenUrl = "https://example.com/decoracion.jpg",
            NotasMarkdown = "- Pieza decorativa no funcional\n- Presentación de ejemplo",
            EspecificacionesTecnicas = System.Text.Json.JsonSerializer.SerializeToElement(new
            {
                material = "Resina", altoCm = 30, anchoCm = 60
            }),
            EsVendible = true, Precio = 250
        };
        var producto = await Escribir(admin, "/api/productos", datos);
        var catalogo = await cliente.GetFromJsonAsync<PaginaConsulta<ProductoCatalogo>>(
            "/api/productos?categoria=3&busqueda=" + sku);
        var ficha = Assert.Single(catalogo!.Elementos);
        Assert.Equal(CategoriaProducto.ReplicaDecorativa, ficha.Categoria);
        Assert.Equal(datos.NotasMarkdown, ficha.NotasMarkdown);
        Assert.Equal(250m, ficha.Precio);
        await Escribir(admin, $"/api/inventario/{producto.Id}/movimientos", new RegistrarMovimiento
        {
            IdOperacion = Guid.NewGuid(), Tipo = TipoMovimientoInventario.Entrada,
            VariacionCantidad = 2, Motivo = "Piezas decorativas de prueba"
        });
        var pedido = await Escribir(cliente, "/api/pedidos", new CrearPedido
        {
            IdOperacion = Guid.NewGuid(),
            Partidas = new() { new PartidaPedido { ProductoId = producto.Id, Cantidad = 1 } }
        });
        var pago = await Escribir(admin, $"/api/pedidos/{pedido.Id}/pagos", new RegistrarPago
        {
            IdOperacion = Guid.NewGuid(), ReferenciaExterna = Guid.NewGuid().ToString()
        });
        await Escribir(admin, $"/api/pagos/{pago.Id}/confirmacion", new { });
        await Escribir(admin, $"/api/pedidos/{pedido.Id}/recogida", new { });
        Assert.Equal(1, await admin.GetFromJsonAsync<long>($"/api/inventario/{producto.Id}/existencia"));
        Assert.Equal(HttpStatusCode.OK,
            (await admin.PatchAsJsonAsync($"/api/productos/{producto.Id}/estado", new { activo = false })).StatusCode);
        var oculto = await cliente.GetFromJsonAsync<PaginaConsulta<ProductoCatalogo>>(
            "/api/productos?categoria=3&busqueda=" + sku);
        Assert.Empty(oculto!.Elementos);
        var gestion = await admin.GetFromJsonAsync<PaginaConsulta<ProductoGestion>>(
            "/api/administracion/productos?categoria=3&activo=false&busqueda=" + sku);
        Assert.Equal(producto.Id, Assert.Single(gestion!.Elementos).Id);
    }

    private static async Task<EscrituraResultado> Escribir(HttpClient client, string ruta, object datos)
    {
        var response = await client.PostAsJsonAsync(ruta, datos);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<EscrituraResultado>())!;
    }
}

public sealed class OperacionesFactory(PostgresEscriturasFixture fixture) : WebApplicationFactory<Program>
{
    // Cada instancia usa usuarios distintos aunque las pruebas compartan esquema.
    public static readonly Guid AdminId = Guid.NewGuid();
    public static readonly Guid ClienteId = Guid.NewGuid();
    public static readonly Guid OtroId = Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:Postgres"] = "Host=unused" }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<PcpShootingDbContext>();
            services.AddScoped(sp => fixture.CrearContext(sp.GetRequiredService<IHttpContextAccessor>()));
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "BackPrueba";
                options.DefaultChallengeScheme = "BackPrueba";
                options.DefaultForbidScheme = "BackPrueba";
            }).AddScheme<AuthenticationSchemeOptions, IdentidadBackendHandler>("BackPrueba", _ => { });
            using var db = fixture.CrearContext();
            // Restaura los roles entre pruebas que reutilizan estos identificadores.
            foreach (var id in new[] { AdminId, ClienteId, OtroId })
            {
                var perfil = db.PerfilesUsuario.SingleOrDefault(p => p.Id == id);
                if (perfil is null)
                {
                    perfil = new PerfilUsuario { Id = id, Nombre = "Perfil HTTP" };
                    db.PerfilesUsuario.Add(perfil);
                }
                perfil.Rol = id == AdminId ? RolUsuario.Administrador : RolUsuario.Cliente;
                perfil.Activo = true;
            }
            db.SaveChanges();
        });
    }

    public HttpClient Cliente(Guid id)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("X-Usuario-Prueba", id.ToString());
        return client;
    }
}

public sealed class IdentidadBackendHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, PcpShootingDbContext db)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Guid.TryParse(Request.Headers["X-Usuario-Prueba"], out var id)) return AuthenticateResult.NoResult();
        var claims = new List<Claim> { new("sub", id.ToString()) };
        var perfil = await db.PerfilesUsuario.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id);
        if (perfil?.Activo == true) claims.Add(new Claim("pcp_role", perfil.Rol.ToString()));
        var identity = new ClaimsIdentity(claims, Scheme.Name, "sub", "pcp_role");
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }
}