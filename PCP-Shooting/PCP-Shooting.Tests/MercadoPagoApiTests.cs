using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PCP_Shooting.Domain.Services;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class MercadoPagoApiTests
{
    [Theory]
    [InlineData("pedidos")]
    [InlineData("reservas")]
    public async Task IntentoPendientePosteriorNoQuedaRechazadoPorUnIntentoAnterior(string tipo)
    {
        using var app=new AppPago();var id=tipo=="pedidos"?app.Pedido():app.Reserva();
        if(tipo=="pedidos")
        {
            app.Handler.Responder=_=>AppPago.Json(new{id="pref",sandbox_init_point="https://sandbox.ejemplo.test/pago"});
            using var cliente=app.Cliente("cliente");Assert.Equal(HttpStatusCode.OK,(await cliente.PostAsync($"/api/pedidos/{id}/checkout",null)).StatusCode);
        }
        app.Handler.Responder=r=>r.RequestUri!.AbsolutePath.EndsWith("search")
            ? AppPago.Json(new{results=new[]{new{id=123},new{id=456}},paging=new{total=2}})
            : AppPago.Json(new{id=r.RequestUri.AbsolutePath.EndsWith("123")?123:456,
                external_reference=(tipo=="pedidos"?"pedido:":"reserva:")+id,
                status=r.RequestUri.AbsolutePath.EndsWith("123")?"rejected":"in_process",transaction_amount=100,currency_id="MXN"});
        using var admin=app.Cliente("admin");Assert.Equal(HttpStatusCode.OK,(await admin.PostAsync($"/api/administracion/mercadopago/{tipo}/{id}/conciliar",null)).StatusCode);
        Assert.Equal(EstadoPago.Pendiente,tipo=="pedidos"?app.Leer(db=>db.Pagos.Single().Estado):app.Leer(db=>db.PagosReserva.Single().Estado));
    }
    [Theory]
    [InlineData("pedidos")]
    [InlineData("reservas")]
    public async Task ReabrirCheckoutRechazadoReactivaConciliacion(string tipo)
    {
        using var app=new AppPago();var id=tipo=="pedidos"?app.Pedido():app.Reserva(false);
        app.Handler.Responder=_=>AppPago.Json(new {id="pref",sandbox_init_point="https://sandbox.ejemplo.test/pago"});
        using var cliente=app.Cliente("cliente");var ruta=$"/api/{tipo}/{id}/checkout";
        Assert.Equal(HttpStatusCode.OK,(await cliente.PostAsync(ruta,null)).StatusCode);
        app.Leer(db=>{if(tipo=="pedidos"){var p=db.Pagos.Single();p.Estado=EstadoPago.Rechazado;p.PaginaConciliacion=0;}
            else {var p=db.PagosReserva.Single();p.Estado=EstadoPago.Rechazado;p.PaginaConciliacion=0;}return db.SaveChanges();});
        Assert.Equal(HttpStatusCode.OK,(await cliente.PostAsync(ruta,null)).StatusCode);
        Assert.Equal(EstadoPago.Pendiente,tipo=="pedidos"?app.Leer(db=>db.Pagos.Single().Estado):app.Leer(db=>db.PagosReserva.Single().Estado));
        Assert.Equal(1,tipo=="pedidos"?app.Leer(db=>db.Pagos.Single().PaginaConciliacion):app.Leer(db=>db.PagosReserva.Single().PaginaConciliacion));
    }
    [Theory]
    [InlineData("reservas")]
    [InlineData("pedidos")]
    public async Task IntentoAprobadoPosteriorReemplazaIdentificadorPendiente(string tipo)
    {
        using var app = new AppPago();
        var id = tipo == "reservas" ? app.Reserva() : app.Pedido();
        var referencia = (tipo == "reservas" ? "reserva:" : "pedido:") + id;
        using var cliente = app.Cliente("cliente");
        if (tipo == "pedidos")
        {
            app.Handler.Responder = _ => AppPago.Json(new { id = "pref", sandbox_init_point = "https://sandbox.ejemplo.test/pago" });
            Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsync($"/api/pedidos/{id}/checkout", null)).StatusCode);
        }
        app.Handler.Responder = r =>
        {
            if (r.RequestUri!.AbsolutePath.EndsWith("search"))
                return AppPago.Json(new { results = new[] { new { id = 123 }, new { id = 456 } }, paging = new { total = 2 } });
            var primero = r.RequestUri.AbsolutePath.EndsWith("123");
            return AppPago.Json(new { id = primero ? 123 : 456, external_reference = referencia, status = primero ? "in_process" : "approved",
                transaction_amount = 100, currency_id = "MXN" });
        };
        using var admin = app.Cliente("admin");
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/administracion/mercadopago/{tipo}/{id}/conciliar", null)).StatusCode);
        Assert.Equal("456", tipo == "reservas" ? app.Leer(db => db.PagosReserva.Single().ReferenciaExterna) : app.Leer(db => db.Pagos.Single().ReferenciaExterna));
        Assert.Equal(EstadoPago.Confirmado, tipo == "reservas" ? app.Leer(db => db.PagosReserva.Single().Estado) : app.Leer(db => db.Pagos.Single().Estado));
    }

    [Fact]
    public async Task PedidoCheckoutReutilizaPreferenciaYConciliacionDescuentaUnaSolaVez()
    {
        using var app = new AppPago();
        var id = app.Pedido();
        var preferencias = 0;
        app.Handler.Responder = r =>
        {
            if (r.Method == HttpMethod.Post) { preferencias++; return AppPago.Json(new { id = "pref-pedido", sandbox_init_point = "https://sandbox.ejemplo.test/pago" }); }
            return AppPago.Json(r.RequestUri!.AbsolutePath.EndsWith("search")
                ? new { results = new[] { new { id = 123 } }, paging = new { total = 1 } }
                : (object)new { id = 123, external_reference = "pedido:" + id, status = "approved", transaction_amount = 100, currency_id = "MXN" });
        };
        using var cliente = app.Cliente("cliente");
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsync($"/api/pedidos/{id}/checkout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsync($"/api/pedidos/{id}/checkout", null)).StatusCode);
        Assert.Equal(1, preferencias);
        using var admin = app.Cliente("admin");
        var ruta = $"/api/administracion/mercadopago/pedidos/{id}/conciliar";
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync(ruta, null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync(ruta, null)).StatusCode);
        Assert.Equal(EstadoPedido.Pagado, app.Leer(db => db.Pedidos.Single().Estado));
        Assert.Equal(1, app.Leer(db => db.MovimientosInventario.Count(m => m.DetallePedidoId != null)));
        Assert.Equal(9, app.Leer(db => db.MovimientosInventario.Sum(m => m.VariacionCantidad)));
    }

    [Theory]
    [InlineData("web")]
    [InlineData("mostrador")]
    public async Task CheckoutReservaSoloPropietarioOStaffYReintentoNoCreaOtro(string origen)
    {
        using var app = new AppPago();
        var id = app.Reserva(false);
        app.Handler.Responder = r => AppPago.Json(new { id = "pref", sandbox_init_point = "https://sandbox.ejemplo.test/pago" });
        using var ajeno = app.Cliente("admin");
        Assert.Equal(HttpStatusCode.NotFound, (await ajeno.PostAsync($"/api/reservas/{id}/checkout", null)).StatusCode);
        using var cliente = app.Cliente(origen == "web" ? "cliente" : "recepcion");
        var ruta = origen == "web" ? $"/api/reservas/{id}/checkout" : $"/api/reservas/administracion/{id}/checkout";
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsync(ruta, null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsync(ruta, null)).StatusCode);
        Assert.Equal(1, app.Handler.Llamadas);
        Assert.Null(app.Leer(db => db.ReservasWeb.Single().PagoConfirmadoEnUtc));
    }

    [Theory]
    [InlineData(502)]
    [InlineData(200)]
    public async Task ErrorProveedorOReferenciaAjenaNoConfirmaReserva(int codigo)
    {
        using var app = new AppPago();
        var id = app.Reserva();
        app.Handler.Responder = r => codigo != 200 ? new((HttpStatusCode)codigo)
            : AppPago.Json(r.RequestUri!.AbsolutePath.EndsWith("search")
                ? new { results = new[] { new { id = 123 } }, paging = new { total = 1 } }
                : (object)new { id = 123, external_reference = "reserva:" + Guid.NewGuid(), status = "approved", transaction_amount = 100, currency_id = "MXN" });
        using var admin = app.Cliente("admin");
        Assert.Equal((HttpStatusCode)codigo, (await admin.PostAsync($"/api/administracion/mercadopago/reservas/{id}/conciliar", null)).StatusCode);
        Assert.Null(app.Leer(db => db.ReservasWeb.Single().PagoConfirmadoEnUtc));
    }

    [Fact]
    public async Task ConciliacionPaginaResultadosYValidaRecursoAntesDeConsultar()
    {
        using var app = new AppPago();
        var id = app.Reserva();
        app.Handler.Responder = r =>
        {
            Assert.Contains("offset=50", r.RequestUri!.Query);
            return AppPago.Json(new { results = Array.Empty<object>(), paging = new { total = 50 } });
        };
        using var admin = app.Cliente("admin");
        var ruta = $"/api/administracion/mercadopago/reservas/{id}/conciliar";
        var respuesta = await admin.PostAsync(ruta + "?pagina=2", null);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.False((await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("hayMas").GetBoolean());
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync(ruta + "?pagina=0", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsync($"/api/administracion/mercadopago/reservas/{Guid.NewGuid()}/conciliar", null)).StatusCode);
        Assert.Equal(1, app.Handler.Llamadas);
    }

    [Fact]
    public async Task ConciliacionRecuperaAprobacionSinWebhookYReintentoNoCambiaFecha()
    {
        using var app = new AppPago();
        var id = app.Reserva();
        app.Handler.Responder = r => AppPago.Json(r.RequestUri!.AbsolutePath.EndsWith("search")
            ? new { results = new[] { new { id = 123 } }, paging = new { total = 1 } }
            : (object)new { id = 123, external_reference = "reserva:" + id, status = "approved", transaction_amount = 100m, currency_id = "MXN" });
        using var admin = app.Cliente("admin");
        var ruta = $"/api/administracion/mercadopago/reservas/{id}/conciliar";
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync(ruta, null)).StatusCode);
        var fecha = app.Leer(db => db.ReservasWeb.Single().PagoConfirmadoEnUtc);
        Assert.NotNull(fecha);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync(ruta, null)).StatusCode);
        Assert.Equal(fecha, app.Leer(db => db.ReservasWeb.Single().PagoConfirmadoEnUtc));
        Assert.Equal(EstadoPago.Confirmado, app.Leer(db => db.PagosReserva.Single().Estado));
        Assert.Equal(0, app.Leer(db => db.MovimientosInventario.Count()));
    }

    [Theory]
    [InlineData("cliente", HttpStatusCode.Forbidden)]
    [InlineData("recepcion", HttpStatusCode.Forbidden)]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    public async Task ConciliacionSoloAdministrador(string? actor, HttpStatusCode esperado)
    {
        using var app = new AppPago();
        using var cliente = app.Cliente(actor);
        Assert.Equal(esperado, (await cliente.PostAsync($"/api/administracion/mercadopago/reservas/{Guid.NewGuid()}/conciliar", null)).StatusCode);
        Assert.Equal(0, app.Handler.Llamadas);
    }

    [Theory]
    [InlineData("approved", 99, "MXN", false)]
    [InlineData("approved", 100, "USD", false)]
    [InlineData("rejected", 100, "MXN", false)]
    [InlineData("approved", 100, "MXN", true)]
    public async Task WebhookValidaImporteMonedaYReferencia(string estado, decimal importe, string moneda, bool referenciaAjena)
    {
        using var app = new AppPago();
        var id = app.Reserva();
        app.Handler.Responder = _ => AppPago.Json(new { id = 123, external_reference = "reserva:" + (referenciaAjena ? Guid.NewGuid() : id),
            status = estado, transaction_amount = importe, currency_id = moneda });
        using var cliente = app.Cliente(null);
        await AppPago.Notificar(cliente);
        Assert.Null(app.Leer(db => db.ReservasWeb.Single().PagoConfirmadoEnUtc));
        Assert.Equal(estado == "rejected" && !referenciaAjena ? EstadoPago.Rechazado : EstadoPago.Pendiente,
            app.Leer(db => db.PagosReserva.Single().Estado));
    }

    [Fact]
    public async Task FirmaInvalidaNoConsultaProveedorYDuplicadaNoRevierteAprobacionTardia()
    {
        using var app = new AppPago();
        var id = app.Reserva();
        using var cliente = app.Cliente(null);
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.PostAsync("/api/webhooks/mercadopago?data.id=123", null)).StatusCode);
        Assert.Equal(0, app.Handler.Llamadas);
        app.Handler.Responder = _ => AppPago.Json(new { id = 123, external_reference = "reserva:" + id, status = "approved", transaction_amount = 100, currency_id = "MXN" });
        Assert.Equal(HttpStatusCode.OK, (await AppPago.Notificar(cliente)).StatusCode);
        var fecha = app.Leer(db => db.ReservasWeb.Single().PagoConfirmadoEnUtc);
        Assert.NotNull(fecha);
        Assert.Equal(HttpStatusCode.OK, (await AppPago.Notificar(cliente)).StatusCode);
        Assert.Equal(fecha, app.Leer(db => db.ReservasWeb.Single().PagoConfirmadoEnUtc));
        app.Handler.Responder = _ => AppPago.Json(new { id = 123, external_reference = "reserva:" + id, status = "rejected", transaction_amount = 100, currency_id = "MXN" });
        Assert.Equal(HttpStatusCode.OK, (await AppPago.Notificar(cliente)).StatusCode);
        Assert.Equal(EstadoPago.Confirmado, app.Leer(db => db.PagosReserva.Single().Estado));
    }

    internal sealed class AppPago : IDisposable
    {
        private readonly PerfilesFactory origen = new();
        private readonly WebApplicationFactory<Program> app;
        public PagoHandler Handler { get; } = new();
        public AppPago()
        {
            app = origen.WithWebHostBuilder(b => b.ConfigureServices(s =>
            {
                s.RemoveAll<IHttpClientFactory>();
                s.AddSingleton<IHttpClientFactory>(new ClientesHttp(Handler));
                s.RemoveAll<ITransaccionesPersistence>();
                s.AddScoped<ITransaccionesPersistence, TransaccionLocal>();
                s.Configure<MercadoPagoOptions>(o => { o.AccessToken = "prueba"; o.WebhookSecret = "secreto-prueba";
                    o.NotificationUrl = "https://ejemplo.test/webhook"; o.SuccessUrl = o.FailureUrl = o.PendingUrl = "https://ejemplo.test/retorno"; });
            }));
        }
        public HttpClient Cliente(string? actor)
        {
            var c = app.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
            if (actor is not null) c.DefaultRequestHeaders.Add("X-Identidad-Prueba", actor);
            return c;
        }
        public T Leer<T>(Func<PcpShootingDbContext, T> leer) { using var s = app.Services.CreateScope(); return leer(s.ServiceProvider.GetRequiredService<PcpShootingDbContext>()); }
        public Guid Reserva(bool conIntento = true)
        {
            using var s = app.Services.CreateScope();
            var db = s.ServiceProvider.GetRequiredService<PcpShootingDbContext>();
            var id = Guid.NewGuid();
            var r = new ReservaWeb { Id = id, UsuarioId = PerfilesFactory.ClienteId, CreadoPorId = PerfilesFactory.ClienteId,
                CodigoPaquete = "PRUEBA", NombrePaquete = "Actividad", IncluyeMarkdown = "Actividad", SolicitudHash = "HASH", ZonaHoraria = "America/Cancun",
                PrecioPorPersona = 100, Total = 100, VencePagoEnUtc = DateTime.UtcNow.AddMinutes(conIntento ? -2 : 5) };
            r.Participantes.Add(new ParticipanteReserva { Id = Guid.NewGuid(), LineaId = db.LineasTiro.First().Id, Nombre = "Adulto", Nacimiento = new(2000,1,1) });
            if (conIntento) r.Pagos.Add(new PagoReserva { Id = Guid.NewGuid(), Importe = 100, PreferenciaMercadoPagoId = "pref", CheckoutUrl = "https://ejemplo.test/pago",
                CreadoEnUtc = DateTime.UtcNow.AddMinutes(-5) });
            db.ReservasWeb.Add(r); db.SaveChanges(); return id;
        }
        public Guid Pedido()
        {
            using var s = app.Services.CreateScope();
            var db = s.ServiceProvider.GetRequiredService<PcpShootingDbContext>();
            var p = new Producto { Id = Guid.NewGuid(), Sku = "PLAYERA", Nombre = "Playera", Precio = 100, EsVendible = true };
            var pedido = new Pedido { Id = Guid.NewGuid(), UsuarioId = PerfilesFactory.ClienteId, CreadoEnUtc = DateTime.UtcNow };
            pedido.Detalles.Add(new DetallePedido { Id = Guid.NewGuid(), Producto = p, Cantidad = 1, PrecioUnitario = 100 });
            db.Pedidos.Add(pedido);
            db.MovimientosInventario.Add(new MovimientoInventario { Id = Guid.NewGuid(), Producto = p, Tipo = TipoMovimientoInventario.Entrada,
                VariacionCantidad = 10, Motivo = "Existencia inicial", RegistradoEnUtc = DateTime.UtcNow });
            db.SaveChanges(); return pedido.Id;
        }
        public static HttpResponseMessage Json(object o) => new(HttpStatusCode.OK) { Content = JsonContent.Create(o) };
        public static Task<HttpResponseMessage> Notificar(HttpClient c)
        {
            var hmac = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes("secreto-prueba"), Encoding.UTF8.GetBytes("id:123;request-id:peticion;ts:12345;")));
            var r = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/mercadopago?data.id=123");
            r.Headers.Add("x-request-id", "peticion"); r.Headers.Add("x-signature", "ts=12345,v1=" + hmac);
            return c.SendAsync(r);
        }
        public void Dispose() { app.Dispose(); origen.Dispose(); }
    }
    internal sealed class PagoHandler : HttpMessageHandler
    {
        public int Llamadas { get; private set; }
        public Func<HttpRequestMessage, HttpResponseMessage> Responder { get; set; } = _ => new(HttpStatusCode.ServiceUnavailable);
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) { Llamadas++; return Task.FromResult(Responder(r)); }
    }
    internal sealed class ClientesHttp(PagoHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, false) { BaseAddress = new Uri("https://api.mercadopago.com/") };
    }
    private sealed class TransaccionLocal(PcpShootingDbContext db) : ITransaccionesPersistence
    {
        public async Task<T> EjecutarAsync<T>(Func<Task<T>> operacion, CancellationToken ct) { var r = await operacion(); await db.SaveChangesAsync(ct); return r; }
    }
}
