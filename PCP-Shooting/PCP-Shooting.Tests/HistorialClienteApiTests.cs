using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class HistorialClienteApiTests : IClassFixture<PerfilesFactory>
{
    private readonly PerfilesFactory factory;
    private static readonly Guid[] Pedidos = { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

    public HistorialClienteApiTests(PerfilesFactory factory)
    {
        this.factory = factory;
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>();
        if (db.Pedidos.Any()) return;
        var producto = new Producto
        {
            Id = Guid.NewGuid(), Sku = "HISTORIAL-PRUEBA", Nombre = "Producto anterior",
            Activo = false, Precio = 999
        };
        db.Productos.Add(producto);
        for (var indice = 0; indice < 4; indice++)
        {
            var usuario = indice < 3 ? PerfilesFactory.ClienteId : PerfilesFactory.InactivoId;
            var precio = (indice + 1) * 10m;
            var pedido = new Pedido
            {
                Id = indice < 3 ? Pedidos[indice] : Guid.NewGuid(),
                UsuarioId = usuario, CreadoEnUtc = new DateTime(2026, 1, indice + 1, 12, 0, 0, DateTimeKind.Utc),
                Estado = indice == 1 ? EstadoPedido.Recogido : EstadoPedido.Pagado
            };
            pedido.Detalles.Add(new DetallePedido
            {
                Id = Guid.NewGuid(), ProductoId = producto.Id, Cantidad = 2, PrecioUnitario = precio
            });
            var pago = new Pago
            {
                Id = Guid.NewGuid(), Importe = precio * 2, Estado = EstadoPago.Confirmado,
                ReferenciaExterna = "HIST-" + indice, ConfirmadoEnUtc = pedido.CreadoEnUtc.AddHours(1)
            };
            pedido.Pagos.Add(pago);
            db.Pedidos.Add(pedido);
            db.Membresias.Add(new MembresiaCartaq
            {
                Id = Guid.NewGuid(), UsuarioId = usuario, Pago = pago,
                InicioVigencia = new DateOnly(2024 + indice, 1, 1),
                FinVigenciaExclusivo = new DateOnly(2025 + indice, 1, 1)
            });
        }
        db.SaveChanges();
    }

    private static string Ruta(Guid id) => "/api/administracion/usuarios/" + id + "/historial";

    [Fact]
    public async Task HistorialReuneSoloLosDatosDelClienteIndicado()
    {
        using var admin = factory.CrearCliente("admin");
        var historial = await admin.GetFromJsonAsync<HistorialCliente>(
            Ruta(PerfilesFactory.ClienteId) + "?usuarioId=" + PerfilesFactory.InactivoId);
        Assert.Equal(PerfilesFactory.ClienteId, historial!.Perfil.Id);
        Assert.Equal(3, historial.Pedidos.Total);
        Assert.Equal(3, historial.Pagos.Total);
        Assert.Equal(3, historial.Membresias.Total);
        Assert.All(historial.Pedidos.Elementos, pedido => Assert.Equal(PerfilesFactory.ClienteId, pedido.UsuarioId));
        Assert.All(historial.Pagos.Elementos, pago => Assert.Equal(PerfilesFactory.ClienteId, pago.UsuarioId));
        Assert.Equal(60m, historial.Pedidos.Elementos[0].Total);
        Assert.Equal(Pedidos[2], historial.Pedidos.Elementos[0].Id);
        Assert.Equal(new DateOnly(2026, 1, 1), historial.Membresias.Elementos[0].InicioVigencia);
    }

    [Fact]
    public async Task LasTresColeccionesSePaginanIndependientemente()
    {
        using var admin = factory.CrearCliente("admin");
        var historial = await admin.GetFromJsonAsync<HistorialCliente>(
            Ruta(PerfilesFactory.ClienteId) + "?tamanoPagina=1&paginaPedidos=2&paginaPagos=3&paginaMembresias=1");
        Assert.Equal(2, historial!.Pedidos.Pagina);
        Assert.Equal(3, historial.Pagos.Pagina);
        Assert.Equal(1, historial.Membresias.Pagina);
        Assert.Equal(40m, Assert.Single(historial.Pedidos.Elementos).Total);
        Assert.Equal(20m, Assert.Single(historial.Pagos.Elementos).Importe);
        Assert.Equal(new DateOnly(2026, 1, 1), Assert.Single(historial.Membresias.Elementos).InicioVigencia);
        Assert.Equal(3, historial.Pedidos.Total);
        Assert.Equal(3, historial.Pagos.Total);
        Assert.Equal(3, historial.Membresias.Total);
    }

    [Fact]
    public async Task PaginasFueraDeRangoConservanPerfilYTotales()
    {
        using var admin = factory.CrearCliente("admin");
        var historial = await admin.GetFromJsonAsync<HistorialCliente>(
            Ruta(PerfilesFactory.ClienteId) +
            "?tamanoPagina=100&paginaPedidos=2147483647&paginaPagos=2147483647&paginaMembresias=2147483647");
        Assert.NotNull(historial!.Perfil);
        Assert.Empty(historial.Pedidos.Elementos);
        Assert.Empty(historial.Pagos.Elementos);
        Assert.Empty(historial.Membresias.Elementos);
        Assert.Equal(3, historial.Pedidos.Total);
        Assert.Equal(3, historial.Pagos.Total);
        Assert.Equal(3, historial.Membresias.Total);
    }

    [Fact]
    public async Task PerfilSinActividadDevuelveColeccionesVacias()
    {
        using var admin = factory.CrearCliente("admin");
        var historial = await admin.GetFromJsonAsync<HistorialCliente>(Ruta(PerfilesFactory.AdminId));
        Assert.Equal(PerfilesFactory.AdminId, historial!.Perfil.Id);
        Assert.Equal(0, historial.Pedidos.Total);
        Assert.Equal(0, historial.Pagos.Total);
        Assert.Equal(0, historial.Membresias.Total);
    }

    [Fact]
    public async Task AdministracionPuedeConsultarUnPerfilDesactivado()
    {
        using var admin = factory.CrearCliente("admin");
        var historial = await admin.GetFromJsonAsync<HistorialCliente>(Ruta(PerfilesFactory.InactivoId));
        Assert.False(historial!.Perfil.Activo);
        Assert.Single(historial.Pedidos.Elementos);
        Assert.Single(historial.Pagos.Elementos);
        Assert.Single(historial.Membresias.Elementos);
    }

    [Fact]
    public async Task PerfilInexistenteDevuelve404()
    {
        using var admin = factory.CrearCliente("admin");
        var response = await admin.GetAsync(Ruta(Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("cliente", HttpStatusCode.Forbidden)]
    [InlineData("inactivo", HttpStatusCode.Forbidden)]
    public async Task HistorialRequiereAdministrador(string? identidad, HttpStatusCode esperado)
    {
        using var client = factory.CrearCliente(identidad);
        Assert.Equal(esperado, (await client.GetAsync(Ruta(PerfilesFactory.ClienteId))).StatusCode);
    }

    [Fact]
    public async Task RecepcionNoPuedeConsultarElHistorialPrivado()
    {
        using var app = new JwtBackendFactory();
        using var recepcion = app.Cliente(app.Token(JwtBackendFactory.RecepcionId));
        Assert.Equal(HttpStatusCode.Forbidden,
            (await recepcion.GetAsync(Ruta(JwtBackendFactory.ClienteId))).StatusCode);
    }

    [Theory]
    [InlineData("paginaPedidos=0")]
    [InlineData("paginaPagos=0")]
    [InlineData("paginaMembresias=0")]
    [InlineData("tamanoPagina=101")]
    [InlineData("paginaPagos=invalida")]
    public async Task PaginacionInvalidaSeRechaza(string filtro)
    {
        using var admin = factory.CrearCliente("admin");
        var response = await admin.GetAsync(Ruta(PerfilesFactory.ClienteId) + "?" + filtro);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
