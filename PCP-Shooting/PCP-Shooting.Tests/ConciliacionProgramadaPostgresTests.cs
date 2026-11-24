using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Domain.Services;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Migrations;
using PCP_Shooting.Persistence.Models;
using PCP_Shooting.Persistence.Services;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class ConciliacionProgramadaPostgresTests(PostgresEscriturasFixture fixture):IClassFixture<PostgresEscriturasFixture>
{
    [PostgresFact]
    public async Task MigracionIncorporaCheckoutsPreviosYCicloRecuperaPedidoYReservaSinWebhook()
    {
        await using var db=fixture.CrearContext();var ahora=DateTime.UtcNow;
        var esquema=await db.Database.SqlQueryRaw<string>("SELECT current_schema() AS \"Value\"").SingleAsync();Assert.StartsWith("pcp_test_",esquema);
        var usuario=new PerfilUsuario{Id=Guid.NewGuid(),Nombre="Cliente conciliación programada"};
        var producto=new Producto{Id=Guid.NewGuid(),Sku="CONCILIACION-AUTO",Nombre="Playera",Precio=100,EsVendible=true};
        var pedido=new Pedido{Id=Guid.NewGuid(),UsuarioId=usuario.Id,CreadoEnUtc=ahora.AddMinutes(-20)};
        pedido.Detalles.Add(new DetallePedido{Id=Guid.NewGuid(),Producto=producto,Cantidad=1,PrecioUnitario=100});
        var pago=new Pago{Id=Guid.NewGuid(),Importe=100,PreferenciaMercadoPagoId="pref-previa",CheckoutUrl="https://ejemplo.test/pago"};pedido.Pagos.Add(pago);
        var manual=new Pago{Id=Guid.NewGuid(),Importe=100,ReferenciaExterna="MANUAL"};pedido.Pagos.Add(manual);
        var reserva=new ReservaWeb{Id=Guid.NewGuid(),UsuarioId=usuario.Id,CreadoPorId=usuario.Id,EsMostrador=true,
            CodigoPaquete="AUTO",NombrePaquete="Actividad",IncluyeMarkdown="Actividad",SolicitudHash=new string('A',64),ZonaHoraria="America/Cancun",
            PrecioPorPersona=100,Total=100,CreadoEnUtc=ahora.AddMinutes(-20),VencePagoEnUtc=ahora.AddMinutes(-10),
            Fecha=DateOnly.FromDateTime(ahora),Hora=new(13,0),InicioUtc=ahora.AddHours(2),FinActividadUtc=ahora.AddHours(2).AddMinutes(10),FinOcupacionUtc=ahora.AddHours(2).AddMinutes(15)};
        reserva.Pagos.Add(new PagoReserva{Id=Guid.NewGuid(),Importe=100,CreadoEnUtc=ahora.AddMinutes(-15)});
        db.PerfilesUsuario.Add(usuario);db.Pedidos.Add(pedido);db.ReservasWeb.Add(reserva);
        db.MovimientosInventario.Add(new MovimientoInventario{Id=Guid.NewGuid(),Producto=producto,Tipo=TipoMovimientoInventario.Entrada,VariacionCantidad=2,RegistradoEnUtc=ahora});
        await db.SaveChangesAsync();db.ChangeTracker.Clear();
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"Pagos\" DROP COLUMN \"EsMercadoPago\", DROP COLUMN \"UltimaConciliacionEnUtc\", DROP COLUMN \"PaginaConciliacion\"");
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"PagosReserva\" DROP COLUMN \"UltimaConciliacionEnUtc\", DROP COLUMN \"PaginaConciliacion\", DROP COLUMN \"ConciliacionEnCurso\"");
        foreach(var comando in db.GetService<IMigrationsSqlGenerator>().Generate(new ConciliacionProgramada().UpOperations,db.Model))
            await db.Database.ExecuteSqlRawAsync(comando.CommandText);
        Assert.True((await db.Pagos.AsNoTracking().SingleAsync(p=>p.Id==pago.Id)).EsMercadoPago);
        Assert.False((await db.Pagos.AsNoTracking().SingleAsync(p=>p.Id==manual.Id)).EsMercadoPago);
        using var handler=new MercadoPagoApiTests.PagoHandler{Responder=r=>
        {
            var esPedido=r.RequestUri!.Query.Contains(pedido.Id.ToString()) || r.RequestUri.AbsolutePath.EndsWith("123");
            return MercadoPagoApiTests.AppPago.Json(r.RequestUri.AbsolutePath.EndsWith("search")
                ? new {results=new[]{new {id=esPedido?123:456}},paging=new {total=1}}
                : (object)new {id=esPedido?123:456,external_reference=(esPedido?"pedido:":"reserva:")+(esPedido?pedido.Id:reserva.Id),status="approved",transaction_amount=100,currency_id="MXN"});
        }};
        var s=new ServiceCollection();s.AddLogging();s.AddSingleton<TimeProvider>(TimeProvider.System);
        s.AddSingleton<IOptions<MercadoPagoOptions>>(Options.Create(new MercadoPagoOptions{AccessToken="simulado"}));
        s.AddSingleton<IOptions<ConciliacionProgramadaOptions>>(Options.Create(new ConciliacionProgramadaOptions{Habilitada=true,Lote=2,RetrasoMinutos=0}));
        s.AddSingleton<IHttpClientFactory>(new MercadoPagoApiTests.ClientesHttp(handler));s.AddScoped(_=>fixture.CrearContext());
        s.AddScoped<ITransaccionesPersistence,TransaccionesPersistence>();s.AddScoped<IEscriturasService,EscriturasService>();
        s.AddScoped<IMercadoPagoService,MercadoPagoService>();s.AddScoped<IConciliacionProgramadaService,ConciliacionProgramadaService>();
        await using var provider=s.BuildServiceProvider();await using var scope=provider.CreateAsyncScope();
        var servicio=scope.ServiceProvider.GetRequiredService<IConciliacionProgramadaService>();
        var resultado=await servicio.EjecutarCicloAsync(default);Assert.Equal(2,resultado.Revisadas);Assert.Equal(0,resultado.Fallidas);
        Assert.Equal(0,(await servicio.EjecutarCicloAsync(default)).Revisadas);
        db.ChangeTracker.Clear();Assert.Equal(EstadoPedido.Pagado,(await db.Pedidos.SingleAsync(p=>p.Id==pedido.Id)).Estado);
        Assert.NotNull((await db.ReservasWeb.SingleAsync(r=>r.Id==reserva.Id)).PagoConfirmadoEnUtc);
        Assert.Null((await db.Pagos.SingleAsync(p=>p.Id==manual.Id)).UltimaConciliacionEnUtc);
        Assert.Equal(1,await db.MovimientosInventario.SumAsync(m=>m.VariacionCantidad));Assert.Equal(4,handler.Llamadas);
    }
}
