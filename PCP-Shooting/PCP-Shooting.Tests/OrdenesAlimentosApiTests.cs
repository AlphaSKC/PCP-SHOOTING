using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class OrdenesAlimentosApiTests
{
    [Fact]
    public async Task PreparaSinPagoCobraSinDescontarYEntregaUnaSolaVez()
    {
        using var app=new ContenidoImagenesApiTests.App(true); using var cliente=app.Cliente("cliente"); using var recepcion=app.Cliente("recepcion");
        var producto=await Producto(app); var id=Guid.NewGuid(); var datos=new {idOperacion=id,partidas=new[]{new{productoId=producto,cantidad=1}}};
        var respuesta=await cliente.PostAsJsonAsync("/api/ordenes-alimentos",datos); Assert.Equal(HttpStatusCode.OK,respuesta.StatusCode);
        var orden=await respuesta.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal(25m,orden.GetProperty("total").GetDecimal());
        Assert.Equal(HttpStatusCode.OK,(await cliente.PostAsJsonAsync("/api/ordenes-alimentos",datos)).StatusCode);
        foreach(var estado in new[]{"EnPreparacion","Listo"})
        {
            respuesta=await recepcion.PostAsJsonAsync($"/api/administracion/ordenes-alimentos/{id}/estado",new {idOperacion=Guid.NewGuid(),version=orden.GetProperty("version").GetGuid(),estado});
            Assert.Equal(HttpStatusCode.OK,respuesta.StatusCode); orden=await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        }
        var entrega=Guid.NewGuid();
        Assert.Equal(HttpStatusCode.Conflict,(await recepcion.PostAsJsonAsync($"/api/administracion/ordenes-alimentos/{id}/entrega",new{idOperacion=entrega,version=orden.GetProperty("version").GetGuid()})).StatusCode);
        respuesta=await recepcion.PostAsJsonAsync($"/api/administracion/ordenes-alimentos/{id}/cobro",new{idOperacion=Guid.NewGuid(),version=orden.GetProperty("version").GetGuid(),metodo="Tarjeta",referencia="TICKET-001",importe=25m});
        Assert.Equal(HttpStatusCode.OK,respuesta.StatusCode); orden=await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1L,await Stock(app,producto));
        var entregar=new {idOperacion=entrega,version=orden.GetProperty("version").GetGuid()};
        Assert.Equal(HttpStatusCode.OK,(await recepcion.PostAsJsonAsync($"/api/administracion/ordenes-alimentos/{id}/entrega",entregar)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await recepcion.PostAsJsonAsync($"/api/administracion/ordenes-alimentos/{id}/entrega",entregar)).StatusCode);
        Assert.Equal(0L,await Stock(app,producto));
    }

    [Fact]
    public async Task ProduccionPermaneceDeshabilitadaSinCambiarLaCarta()
    {
        using var app=new ContenidoImagenesApiTests.App(); using var cliente=app.Cliente("cliente"); var producto=await Producto(app);
        Assert.Equal(HttpStatusCode.ServiceUnavailable,(await cliente.PostAsJsonAsync("/api/ordenes-alimentos",Datos(Guid.NewGuid(),producto))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await cliente.GetAsync("/api/alimentos")).StatusCode);
        using var scope=app.Services.CreateScope(); Assert.Empty(await scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>().Set<OrdenAlimentos>().ToListAsync());
    }

    [Theory]
    [InlineData(null,HttpStatusCode.Unauthorized)]
    [InlineData("cliente",HttpStatusCode.Forbidden)]
    [InlineData("inactivo",HttpStatusCode.Forbidden)]
    [InlineData("recepcion",HttpStatusCode.OK)]
    [InlineData("admin",HttpStatusCode.OK)]
    public async Task BandejaOperativaSoloParaPersonalActivo(string? actor,HttpStatusCode esperado)
    {
        using var app=new ContenidoImagenesApiTests.App(true); using var cliente=app.Cliente(actor);
        Assert.Equal(esperado,(await cliente.GetAsync("/api/administracion/ordenes-alimentos")).StatusCode);
        if(esperado!=HttpStatusCode.OK)
            Assert.Equal(esperado,(await cliente.PostAsJsonAsync($"/api/administracion/ordenes-alimentos/{Guid.NewGuid()}/entrega",new{idOperacion=Guid.NewGuid(),version=Guid.NewGuid()})).StatusCode);
    }

    [Fact]
    public async Task MostradorSinReservaNiCuentaEsPrivadoYNoPermiteSuplantarClientes()
    {
        using var app=new ContenidoImagenesApiTests.App(true); using var recepcion=app.Cliente("recepcion"); using var cliente=app.Cliente("cliente");
        var producto=await Producto(app); var id=Guid.NewGuid(); var ruta="/api/administracion/ordenes-alimentos";
        var datos=new{idOperacion=id,nombreRecogida="Visita",partidas=new[]{new{productoId=producto,cantidad=1}}};
        Assert.Equal(HttpStatusCode.OK,(await recepcion.PostAsJsonAsync(ruta,datos)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await cliente.GetAsync($"/api/ordenes-alimentos/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await recepcion.GetAsync(ruta+"/"+id)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await cliente.PostAsJsonAsync("/api/ordenes-alimentos",new{idOperacion=Guid.NewGuid(),clienteId=PerfilesFactory.AdminId,partidas=datos.partidas})).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await recepcion.PostAsJsonAsync(ruta,Datos(Guid.NewGuid(),producto))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await recepcion.PostAsJsonAsync(ruta,new{idOperacion=id,nombreRecogida="Otra persona",partidas=datos.partidas})).StatusCode);
    }

    [Fact]
    public async Task RechazaOtrosProductosDuplicadosYCantidadesInvalidas()
    {
        using var app=new ContenidoImagenesApiTests.App(true); using var cliente=app.Cliente("cliente");
        var ropa=await Producto(app,CategoriaProducto.Playera); var alimento=await Producto(app);
        Assert.Equal(HttpStatusCode.Conflict,(await cliente.PostAsJsonAsync("/api/ordenes-alimentos",Datos(Guid.NewGuid(),ropa))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await cliente.PostAsJsonAsync("/api/ordenes-alimentos",new{idOperacion=Guid.NewGuid(),partidas=new[]{new{productoId=alimento,cantidad=1},new{productoId=alimento,cantidad=1}}})).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await cliente.PostAsJsonAsync("/api/ordenes-alimentos",new{idOperacion=Guid.NewGuid(),partidas=new[]{new{productoId=alimento,cantidad=0}}})).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await cliente.PostAsJsonAsync("/api/ordenes-alimentos",new{idOperacion=Guid.NewGuid(),partidas=new[]{new{productoId=alimento,cantidad=2}}})).StatusCode);
    }

    [Fact]
    public async Task PreciosHistoricosVersionesYReintentosNoPermitenSaltarEstadosNiDuplicarCobro()
    {
        using var app=new ContenidoImagenesApiTests.App(true); using var cliente=app.Cliente("cliente"); using var admin=app.Cliente("admin");
        var producto=await Producto(app); var id=Guid.NewGuid(); var creacion=Datos(id,producto);
        var orden=await Leer(await cliente.PostAsJsonAsync("/api/ordenes-alimentos",creacion));
        using(var scope=app.Services.CreateScope())
        {
            var db=scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>(); var p=await db.Productos.SingleAsync(p=>p.Id==producto);
            p.Precio=30m; p.Nombre="Nombre cambiado"; await db.SaveChangesAsync();
        }
        orden=await Leer(await cliente.PostAsJsonAsync("/api/ordenes-alimentos",creacion)); Assert.Equal(25m,orden.GetProperty("total").GetDecimal());
        Assert.Equal("Agua",orden.GetProperty("partidas")[0].GetProperty("nombre").GetString());
        Assert.Equal(HttpStatusCode.Conflict,(await admin.PutAsJsonAsync($"/api/productos/{producto}",new{sku=producto.ToString(),nombre="Agua",categoria=2,esVendible=true,precio=30m})).StatusCode);
        var ruta=$"/api/administracion/ordenes-alimentos/{id}"; var version=orden.GetProperty("version").GetGuid();
        Assert.Equal(HttpStatusCode.Conflict,(await admin.PostAsJsonAsync(ruta+"/estado",new{idOperacion=Guid.NewGuid(),version,estado="Listo"})).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await admin.PostAsJsonAsync(ruta+"/estado",new{idOperacion=Guid.NewGuid(),version=Guid.NewGuid(),estado="EnPreparacion"})).StatusCode);
        var preparar=new{idOperacion=Guid.NewGuid(),version,estado="EnPreparacion"}; orden=await Leer(await admin.PostAsJsonAsync(ruta+"/estado",preparar));
        orden=await Leer(await admin.PostAsJsonAsync(ruta+"/estado",new{idOperacion=Guid.NewGuid(),version=orden.GetProperty("version").GetGuid(),estado="Listo"}));
        Assert.Equal("Listo",(await Leer(await admin.PostAsJsonAsync(ruta+"/estado",preparar))).GetProperty("estado").GetString());
        var pago=new{idOperacion=Guid.NewGuid(),version=orden.GetProperty("version").GetGuid(),metodo="Transferencia",referencia="SPEI-1",importe=25m};
        Assert.Equal(HttpStatusCode.BadRequest,(await admin.PostAsJsonAsync(ruta+"/cobro",new{pago.idOperacion,pago.version,pago.metodo,pago.referencia,importe=24m})).StatusCode);
        await Leer(await admin.PostAsJsonAsync(ruta+"/cobro",pago)); await Leer(await admin.PostAsJsonAsync(ruta+"/cobro",pago));
        Assert.Equal(1L,await Stock(app,producto));
        Assert.Equal(HttpStatusCode.Conflict,(await admin.PostAsJsonAsync(ruta+"/cobro",new{pago.idOperacion,pago.version,pago.metodo,referencia="Otra",pago.importe})).StatusCode);
    }

    [Fact]
    public async Task FaltaDeStockEnEntregaNoDescuentaParcialmenteNiModificaElCobro()
    {
        using var app=new ContenidoImagenesApiTests.App(true); using var cliente=app.Cliente("cliente"); using var admin=app.Cliente("admin");
        var a=await Producto(app); var b=await Producto(app); var id=Guid.NewGuid();
        var orden=await Leer(await cliente.PostAsJsonAsync("/api/ordenes-alimentos",new{idOperacion=id,partidas=new[]{new{productoId=a,cantidad=1},new{productoId=b,cantidad=1}}}));
        var ruta=$"/api/administracion/ordenes-alimentos/{id}";
        foreach(var estado in new[]{"EnPreparacion","Listo"}) orden=await Leer(await admin.PostAsJsonAsync(ruta+"/estado",new{idOperacion=Guid.NewGuid(),version=orden.GetProperty("version").GetGuid(),estado}));
        orden=await Leer(await admin.PostAsJsonAsync(ruta+"/cobro",new{idOperacion=Guid.NewGuid(),version=orden.GetProperty("version").GetGuid(),metodo="Tarjeta",referencia="T-2",importe=50m}));
        using(var scope=app.Services.CreateScope())
        {
            var db=scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>();
            db.MovimientosInventario.Add(new MovimientoInventario{Id=Guid.NewGuid(),ProductoId=b,Tipo=TipoMovimientoInventario.Salida,VariacionCantidad=-1,RegistradoEnUtc=DateTime.UtcNow}); await db.SaveChangesAsync();
        }
        var entrega=new{idOperacion=Guid.NewGuid(),version=orden.GetProperty("version").GetGuid()};
        Assert.Equal(HttpStatusCode.Conflict,(await admin.PostAsJsonAsync(ruta+"/entrega",entrega)).StatusCode);
        Assert.Equal(1L,await Stock(app,a));
        orden=await admin.GetFromJsonAsync<JsonElement>(ruta); Assert.Equal("Listo",orden.GetProperty("estado").GetString()); Assert.Equal(50m,orden.GetProperty("importeConfirmado").GetDecimal());
    }

    private static object Datos(Guid id,Guid producto)=>new{idOperacion=id,partidas=new[]{new{productoId=producto,cantidad=1}}};
    private static async Task<JsonElement> Leer(HttpResponseMessage respuesta)
    {
        Assert.Equal(HttpStatusCode.OK,respuesta.StatusCode); return await respuesta.Content.ReadFromJsonAsync<JsonElement>();
    }

    internal static async Task<Guid> Producto(ContenidoImagenesApiTests.App app,CategoriaProducto categoria=CategoriaProducto.Alimento,int stock=1)
    {
        using var scope=app.Services.CreateScope(); var db=scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>(); var id=Guid.NewGuid();
        db.Productos.Add(new Producto{Id=id,Sku=id.ToString(),Nombre="Agua",Categoria=categoria,EsVendible=true,Precio=25m});
        if(stock>0) db.MovimientosInventario.Add(new MovimientoInventario{Id=Guid.NewGuid(),ProductoId=id,Tipo=TipoMovimientoInventario.Entrada,VariacionCantidad=stock,RegistradoEnUtc=DateTime.UtcNow});
        await db.SaveChangesAsync(); return id;
    }

    private static async Task<long> Stock(ContenidoImagenesApiTests.App app,Guid id)
    {
        using var scope=app.Services.CreateScope(); var db=scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>();
        return await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SumAsync(db.MovimientosInventario.Where(m=>m.ProductoId==id),m=>(long)m.VariacionCantidad);
    }
}
