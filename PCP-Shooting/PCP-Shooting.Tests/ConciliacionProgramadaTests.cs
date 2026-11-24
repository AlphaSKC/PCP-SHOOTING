using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Domain.Services;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;
using Xunit;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PCP_Shooting.Tests;

public sealed class ConciliacionProgramadaTests
{
    [Fact]
    public async Task ReservaEnRevisionConservaLineaYEstadoHastaCompletarBusqueda()
    {
        using var app=new DisponibilidadTestApp();using var cliente=app.ClientePara("cliente");
        var linea=app.Leer(db=>db.LineasTiro.OrderBy(l=>l.Numero).First().Id);var apartado=Guid.NewGuid();var reserva=Guid.NewGuid();
        Assert.Equal(HttpStatusCode.Created,(await cliente.PostAsJsonAsync("/api/apartados",new {idOperacion=apartado,paqueteId=app.PaqueteId,
            fecha="2026-11-20",hora="13:00:00",lineas=new[]{linea}})).StatusCode);
        Assert.Equal(HttpStatusCode.Created,(await cliente.PostAsJsonAsync("/api/reservas",new {idOperacion=reserva,apartadoId=apartado,
            participantes=new[]{new {lineaId=linea,nombre="Adulto",nacimiento="2000-01-01"}}})).StatusCode);
        app.Modificar(db=>db.PagosReserva.Add(new PagoReserva{Id=Guid.NewGuid(),ReservaId=reserva,Importe=100,Estado=EstadoPago.Rechazado,
            ConciliacionEnCurso=true,PaginaConciliacion=2,CreadoEnUtc=app.Reloj.Ahora.UtcDateTime}));
        app.Reloj.Ahora=app.Reloj.Ahora.AddMinutes(11);
        Assert.Equal("PendientePago",(await cliente.GetFromJsonAsync<JsonElement>("/api/reservas/"+reserva)).GetProperty("estado").GetString());
        var mapa=await app.Cliente.GetFromJsonAsync<JsonElement>(app.Ruta()+"&hora=13:00:00");
        Assert.False(mapa.GetProperty("horarios")[0].GetProperty("lineas").EnumerateArray().Single(l=>l.GetProperty("id").GetGuid()==linea).GetProperty("disponible").GetBoolean());
        app.Modificar(db=>db.PagosReserva.Single().ConciliacionEnCurso=false);
        Assert.Equal("Vencida",(await cliente.GetFromJsonAsync<JsonElement>("/api/reservas/"+reserva)).GetProperty("estado").GetString());
        mapa=await app.Cliente.GetFromJsonAsync<JsonElement>(app.Ruta()+"&hora=13:00:00");
        Assert.True(mapa.GetProperty("horarios")[0].GetProperty("lineas").EnumerateArray().Single(l=>l.GetProperty("id").GetGuid()==linea).GetProperty("disponible").GetBoolean());
    }
    [Theory]
    [InlineData(false, "token")]
    [InlineData(true, "")]
    public async Task SinActivacionOCredencialesNoConsultaPagos(bool habilitada,string token)
    {
        using var app=new App(habilitada,token); app.Reserva();
        Assert.Equal(0,(await app.Ciclo()).Revisadas); Assert.Empty(app.Plan.Llamadas);
    }
    [Fact]
    public async Task LoteAcotadoRotaPendientesYExcluyePagosManuales()
    {
        using var app=new App(); var primera=app.Reserva(); var segunda=app.Reserva();
        app.Modificar(db=>{ var p=new Pedido {Id=Guid.NewGuid(),UsuarioId=Guid.NewGuid(),CreadoEnUtc=app.Reloj.Ahora.UtcDateTime.AddDays(-1)};
            p.Pagos.Add(new Pago {Id=Guid.NewGuid(),Importe=100,ReferenciaExterna="MANUAL"});db.Pedidos.Add(p); });
        Assert.Equal(1,(await app.Ciclo()).Revisadas); app.Reloj.Ahora=app.Reloj.Ahora.AddMinutes(1);
        Assert.Equal(1,(await app.Ciclo()).Revisadas);
        Assert.Equal(new[]{primera,segunda}.OrderBy(id=>id),app.Plan.Llamadas.Select(x=>x.Id).OrderBy(id=>id));
        Assert.All(app.Plan.Llamadas,x=>Assert.Equal("reservas",x.Tipo));
    }
    [Fact]
    public async Task ErrorDeUnPagoNoBloqueaElSiguienteNiLoMarcaRechazado()
    {
        using var app=new App(lote:2); app.Reserva();app.Reserva();
        app.Plan.Responder=(_,_,_,_,_)=>throw new HttpRequestException("Proveedor no disponible");
        var resultado=await app.Ciclo(); Assert.Equal(2,resultado.Fallidas);Assert.Equal(2,app.Plan.Llamadas.Count);
        Assert.All(app.Leer(db=>db.PagosReserva.ToArray()),p=>{Assert.Equal(EstadoPago.Pendiente,p.Estado);Assert.NotNull(p.UltimaConciliacionEnUtc);});
    }
    [Fact]
    public async Task UnPendienteAntiguoNoEsDesplazadoPorLlegadasNuevas()
    {
        using var app=new App();var antiguo=app.Reserva();var nuevo=app.Reserva();
        app.Modificar(db=>{db.PagosReserva.Single(p=>p.ReservaId==antiguo).UltimaConciliacionEnUtc=app.Reloj.Ahora.UtcDateTime.AddMinutes(-5);
            db.PagosReserva.Single(p=>p.ReservaId==nuevo).CreadoEnUtc=app.Reloj.Ahora.UtcDateTime.AddMinutes(-1);});
        await app.Ciclo();Assert.Equal(antiguo,Assert.Single(app.Plan.Llamadas).Id);
    }
    [Fact]
    public async Task ContinuaPaginasEnOtroCicloYSeDetieneTrasConfirmar()
    {
        using var app=new App(); var id=app.Reserva();
        app.Plan.Responder=async(db,_,destino,pagina,ct)=>
        {
            if(pagina==2){var p=await db.PagosReserva.Include(p=>p.Reserva).SingleAsync(p=>p.ReservaId==destino,ct);
                p.Estado=EstadoPago.Confirmado;p.ConfirmadoEnUtc=app.Reloj.Ahora.UtcDateTime;p.Reserva.PagoConfirmadoEnUtc=p.ConfirmadoEnUtc;await db.SaveChangesAsync(ct);}
            return new(1,pagina,pagina==1);
        };
        Assert.Equal(1,(await app.Ciclo()).Paginas); Assert.Equal(2,app.Leer(db=>db.PagosReserva.Single().PaginaConciliacion));
        app.Reloj.Ahora=app.Reloj.Ahora.AddMinutes(1);
        Assert.Equal(1,(await app.Ciclo()).Paginas); Assert.Equal(0,(await app.Ciclo()).Revisadas);
        Assert.Equal(new[]{1,2},app.Plan.Llamadas.Select(x=>x.Pagina));
        Assert.NotNull(app.Leer(db=>db.ReservasWeb.Single(r=>r.Id==id).PagoConfirmadoEnUtc));
    }
    [Fact]
    public async Task CancelacionDelProcesoSePropagaSinCambiarPago()
    {
        using var app=new App();app.Reserva();using var cts=new CancellationTokenSource();
        app.Plan.Responder=(_,_,_,_,ct)=>{cts.Cancel();ct.ThrowIfCancellationRequested();return Task.FromResult(new ConciliacionMercadoPago(0,1,false));};
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>app.Ciclo(cts.Token));
        Assert.Equal(EstadoPago.Pendiente,app.Leer(db=>db.PagosReserva.Single().Estado));
    }
    [Fact]
    public async Task RechazoEnPaginaIntermediaConservaProteccionHastaTerminarBusqueda()
    {
        using var app=new App();app.Reserva();
        app.Plan.Responder=async(db,_,_,pagina,ct)=>{var pago=await db.PagosReserva.SingleAsync(ct);pago.Estado=EstadoPago.Rechazado;await db.SaveChangesAsync(ct);
            return new ConciliacionMercadoPago(1,pagina,pagina==1);};
        await app.Ciclo();Assert.True(app.Leer(db=>db.PagosReserva.Single().ConciliacionEnCurso));
        await app.Ciclo();Assert.False(app.Leer(db=>db.PagosReserva.Single().ConciliacionEnCurso));
        Assert.Equal(0,app.Leer(db=>db.PagosReserva.Single().PaginaConciliacion));
    }
    [Fact]
    public async Task RechazoPosteriorAUnaRevisionCompletaNoReactivaLaReserva()
    {
        using var app=new App();app.Reserva();
        app.Modificar(db=>{var p=db.PagosReserva.Single();p.Estado=EstadoPago.Rechazado;p.PaginaConciliacion=1;p.UltimaConciliacionEnUtc=app.Reloj.Ahora.UtcDateTime.AddMinutes(-5);});
        Assert.Equal(0,(await app.Ciclo()).Revisadas);Assert.Empty(app.Plan.Llamadas);
    }
    private sealed class App:IDisposable
    {
        private readonly ServiceProvider servicios; public Plan Plan {get;}=new();
        public RelojDisponibilidad Reloj {get;}=new(DateTimeOffset.Parse("2026-11-24T15:00:00Z"));
        public App(bool habilitada=true,string token="prueba",int lote=1)
        {
            var opciones=new DbContextOptionsBuilder<PcpShootingDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            var s=new ServiceCollection();s.AddLogging();s.AddSingleton(Plan);s.AddSingleton<TimeProvider>(Reloj);
            s.AddSingleton<IOptions<MercadoPagoOptions>>(Options.Create(new MercadoPagoOptions{AccessToken=token}));
            s.AddSingleton<IOptions<ConciliacionProgramadaOptions>>(Options.Create(new ConciliacionProgramadaOptions{Habilitada=habilitada,Lote=lote,PaginasPorOperacion=1,RetrasoMinutos=0}));
            s.AddScoped(_=>new PcpShootingDbContext(opciones));s.AddScoped<IMercadoPagoService,MercadoSimulado>();
            s.AddScoped<IConciliacionProgramadaService,ConciliacionProgramadaService>();servicios=s.BuildServiceProvider();
            Modificar(db=>db.Database.EnsureCreated());
        }
        public Guid Reserva()
        {
            var id=Guid.NewGuid();Modificar(db=>{var r=new ReservaWeb{Id=id,UsuarioId=Guid.NewGuid(),CreadoPorId=Guid.NewGuid(),CodigoPaquete="P",NombrePaquete="Actividad",
                IncluyeMarkdown="Actividad",SolicitudHash="hash",ZonaHoraria="America/Cancun",PrecioPorPersona=100,Total=100};
                r.Pagos.Add(new PagoReserva{Id=Guid.NewGuid(),Importe=100,CreadoEnUtc=Reloj.Ahora.UtcDateTime.AddMinutes(-10)});db.ReservasWeb.Add(r);});return id;
        }
        public void Modificar(Action<PcpShootingDbContext> accion){using var scope=servicios.CreateScope();var db=scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>();accion(db);db.SaveChanges();}
        public T Leer<T>(Func<PcpShootingDbContext,T> leer){using var scope=servicios.CreateScope();return leer(scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>());}
        public async Task<CicloConciliacionResultado> Ciclo(CancellationToken ct=default){await using var scope=servicios.CreateAsyncScope();return await scope.ServiceProvider.GetRequiredService<IConciliacionProgramadaService>().EjecutarCicloAsync(ct);}
        public void Dispose()=>servicios.Dispose();
    }
    private sealed class Plan
    {
        public List<(string Tipo,Guid Id,int Pagina)> Llamadas {get;}=new();
        public Func<PcpShootingDbContext,string,Guid,int,CancellationToken,Task<ConciliacionMercadoPago>> Responder {get;set;}=(_,_,_,p,_)=>Task.FromResult(new ConciliacionMercadoPago(0,p,false));
    }
    private sealed class MercadoSimulado(PcpShootingDbContext db,Plan plan):IMercadoPagoService
    {
        public Task<ConciliacionMercadoPago> ConciliarAsync(string tipo,Guid id,int pagina,CancellationToken ct){plan.Llamadas.Add((tipo,id,pagina));return plan.Responder(db,tipo,id,pagina,ct);}
        public Task<CheckoutMercadoPago> CrearCheckoutPedidoAsync(Guid u,Guid p,CancellationToken ct)=>throw new NotSupportedException();
        public Task<CheckoutMercadoPago> CrearCheckoutReservaAsync(Guid u,Guid p,CancellationToken ct)=>throw new NotSupportedException();
        public Task<CheckoutMercadoPago> CrearCheckoutReservaMostradorAsync(Guid p,CancellationToken ct)=>throw new NotSupportedException();
        public Task<bool> ProcesarNotificacionAsync(string? id,string? f,string? r,CancellationToken ct)=>throw new NotSupportedException();
    }
}
