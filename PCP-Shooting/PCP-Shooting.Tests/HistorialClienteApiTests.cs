using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
    public async Task ReservasDelClienteSePaginanYRespetanPagosEnVerificacion()
    {
        using var app = new DisponibilidadTestApp();
        using var admin = app.ClientePara("admin");
        var ids = new List<Guid>();
        for (var numero = 1; numero <= 5; numero++)
        {
            var id = Guid.NewGuid();
            ids.Add(id);
            var respuesta = await admin.PostAsJsonAsync("/api/reservas/mostrador", new
            {
                idOperacion = id, clienteId = PerfilesFactory.ClienteId,
                paqueteId = app.PaqueteId, fecha = "2026-11-20", hora = "11:00:00",
                participantes = new[] { new { lineaId = app.Leer(db => db.LineasTiro.Single(l => l.Numero == numero).Id),
                    nombre = "Participante", nacimiento = "2000-01-01" } }
            });
            Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        }
        app.Modificar(db =>
        {
            for (var indice = 0; indice < ids.Count; indice++)
            {
                var reserva = db.ReservasWeb.Single(r => r.Id == ids[indice]);
                if (indice == 4) reserva.UsuarioId = PerfilesFactory.AdminId;
                reserva.CreadoEnUtc = app.Reloj.Ahora.UtcDateTime.AddMinutes(-indice);
                if (indice is 1 or 2 or 3) reserva.VencePagoEnUtc = app.Reloj.Ahora.UtcDateTime;
                if (indice == 2) reserva.PagoConfirmadoEnUtc = app.Reloj.Ahora.UtcDateTime;
                if (indice == 3) reserva.Pagos.Add(new PagoReserva { Id = Guid.NewGuid(), Importe = reserva.Total,
                    CreadoEnUtc = app.Reloj.Ahora.UtcDateTime, Estado = EstadoPago.Rechazado, ConciliacionEnCurso = true });
            }
        });
        var historial = await admin.GetFromJsonAsync<JsonElement>(Ruta(PerfilesFactory.ClienteId));
        var reservas = historial.GetProperty("reservas");
        Assert.Equal(4, reservas.GetProperty("total").GetInt32());
        var elementos = reservas.GetProperty("elementos").EnumerateArray().ToArray();
        Assert.Equal(ids.Take(4), elementos.Select(r => r.GetProperty("id").GetGuid()));
        Assert.Equal(new[] { "PendientePago", "Vencida", "Pagada", "PendientePago" },
            elementos.Select(r => r.GetProperty("estado").GetString()));
        Assert.All(elementos, r => Assert.Equal(1, r.GetProperty("cantidadParticipantes").GetInt32()));
        var pagina = await admin.GetFromJsonAsync<JsonElement>(Ruta(PerfilesFactory.ClienteId) + "?tamanoPagina=1&paginaReservas=2&paginaPedidos=3");
        Assert.Equal(ids[1], Assert.Single(pagina.GetProperty("reservas").GetProperty("elementos").EnumerateArray()).GetProperty("id").GetGuid());
        Assert.Equal(3, pagina.GetProperty("pedidos").GetProperty("pagina").GetInt32());
        var vacia = await admin.GetFromJsonAsync<JsonElement>(Ruta(PerfilesFactory.ClienteId) + "?tamanoPagina=100&paginaReservas=2147483647");
        Assert.Equal(4, vacia.GetProperty("reservas").GetProperty("total").GetInt32());
        Assert.Empty(vacia.GetProperty("reservas").GetProperty("elementos").EnumerateArray());
    }

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
    [InlineData("paginaReservas=0")]
    [InlineData("paginaReservas=invalida")]
    [InlineData("tamanoPagina=101")]
    [InlineData("paginaPagos=invalida")]
    public async Task PaginacionInvalidaSeRechaza(string filtro)
    {
        using var admin = factory.CrearCliente("admin");
        var response = await admin.GetAsync(Ruta(PerfilesFactory.ClienteId) + "?" + filtro);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
