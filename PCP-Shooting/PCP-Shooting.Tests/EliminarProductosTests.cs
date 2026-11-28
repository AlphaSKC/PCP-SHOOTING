using System.Net;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Tests;

public class EliminarProductosTests
{
    [Theory]
    [InlineData("admin", CategoriaProducto.Playera, HttpStatusCode.NoContent)]
    [InlineData("recepcion", CategoriaProducto.Alimento, HttpStatusCode.NoContent)]
    [InlineData("recepcion", CategoriaProducto.Playera, HttpStatusCode.Forbidden)]
    [InlineData("cliente", CategoriaProducto.Alimento, HttpStatusCode.Forbidden)]
    [InlineData("inactivo", CategoriaProducto.Alimento, HttpStatusCode.Forbidden)]
    [InlineData(null, CategoriaProducto.Alimento, HttpStatusCode.Unauthorized)]
    public async Task EliminaSoloSinHistorialYConPermiso(string? actor, CategoriaProducto categoria, HttpStatusCode esperado)
    {
        using var app = new ContenidoImagenesApiTests.App();
        using var cliente = app.Cliente(actor);
        var id = Guid.NewGuid();
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>();
            db.Productos.Add(new Producto { Id = id, Sku = id.ToString(), Nombre = "Producto de prueba", Categoria = categoria });
            await db.SaveChangesAsync();
        }
        Assert.Equal(esperado, (await cliente.DeleteAsync($"/api/productos/{id}")).StatusCode);
    }

    [Fact]
    public async Task GuardaAuditoriaYReanudaStorageSinVolverACrearElProducto()
    {
        using var app = new ContenidoImagenesApiTests.App(); using var admin = app.Cliente("admin");
        var id = Guid.NewGuid(); var ruta = $"productos/{id:N}/{Guid.NewGuid():N}.png";
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>();
            db.Productos.Add(new Producto { Id=id,Sku=id.ToString(),Nombre="Equivocado",ImagenUrl="https://imagenes.ejemplo.test/"+ruta });
            db.ImagenesProducto.Add(new ImagenProducto { Id=Guid.NewGuid(),ProductoId=id,CreadorId=PerfilesFactory.AdminId,
                Ruta=ruta,Sha256=new string('a',64),TipoContenido="image/png",Tamano=12,Disponible=true });
            await db.SaveChangesAsync();
        }
        app.Storage.Fallar=true;
        Assert.Equal(HttpStatusCode.BadGateway,(await admin.DeleteAsync($"/api/productos/{id}")).StatusCode);
        using (var scope = app.Services.CreateScope())
        {
            var db=scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>();
            Assert.False(await db.Productos.AnyAsync(p=>p.Id==id));
            Assert.Null((await db.EliminacionesProducto.SingleAsync(e=>e.Id==id)).CompletadaEnUtc);
            Assert.Contains(await db.EventosAuditoria.ToListAsync(),e=>e.EntidadId==id && e.Entidad=="Producto" && e.Accion=="Deleted");
        }
        using var recepcion=app.Cliente("recepcion");
        Assert.Equal(HttpStatusCode.Forbidden,(await recepcion.DeleteAsync($"/api/productos/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,(await admin.DeleteAsync($"/api/productos/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,(await admin.DeleteAsync($"/api/productos/{id}")).StatusCode);
        Assert.Equal(new[]{ruta},app.Storage.Borradas);
        Assert.Equal(HttpStatusCode.NotFound,(await admin.DeleteAsync($"/api/productos/{Guid.NewGuid()}")).StatusCode);
    }

    [Theory]
    [InlineData("movimientos")]
    [InlineData("pedido")]
    [InlineData("paquete")]
    [InlineData("consumo")]
    [InlineData("carga")]
    [InlineData("compartida")]
    [InlineData("paqueteImagen")]
    public async Task BloqueaHistorialInclusoInventarioNetoCeroYFotosAntiguas(string caso)
    {
        using var app = new ContenidoImagenesApiTests.App(); using var admin=app.Cliente("admin");
        var id=Guid.NewGuid();
        using (var scope=app.Services.CreateScope())
        {
            var db=scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>();
            db.Productos.Add(new Producto { Id=id,Sku=id.ToString(),Nombre="Prueba" });
            if(caso=="movimientos") db.MovimientosInventario.AddRange(
                new MovimientoInventario { Id=Guid.NewGuid(),ProductoId=id,VariacionCantidad=1 },
                new MovimientoInventario { Id=Guid.NewGuid(),ProductoId=id,VariacionCantidad=-1,Tipo=TipoMovimientoInventario.Salida });
            if(caso=="pedido") db.DetallesPedido.Add(new DetallePedido { Id=Guid.NewGuid(),ProductoId=id,Cantidad=1 });
            if(caso=="paquete") db.ConsumiblesPaquete.Add(new ConsumiblePaquete { Id=Guid.NewGuid(),ProductoId=id });
            if(caso=="consumo") db.ConsumosReserva.Add(new ConsumoReserva { Id=Guid.NewGuid(),ProductoId=id,Sku="PRUEBA",Nombre="Prueba" });
            if(caso is "carga" or "compartida" or "paqueteImagen")
            {
                var ruta=$"productos/{id:N}/{Guid.NewGuid():N}.png";
                db.ImagenesProducto.Add(new ImagenProducto { Id=Guid.NewGuid(),ProductoId=id,Ruta=ruta,Sha256=new string('a',64),TipoContenido="image/png",Tamano=12,Disponible=caso!="carga" });
                if(caso=="compartida") db.Productos.Add(new Producto { Id=Guid.NewGuid(),Sku="OTRO",Nombre="Otro",NotasMarkdown=$"![foto](https://imagenes.ejemplo.test/{ruta.Replace("/","%2F")})" });
                if(caso=="paqueteImagen") db.Paquetes.Add(new Paquete { Id=Guid.NewGuid(),Codigo="PRUEBA",Nombre="Paquete",IncluyeMarkdown=$"![foto](https://imagenes.ejemplo.test/{ruta})" });
            }
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Conflict,(await admin.DeleteAsync($"/api/productos/{id}")).StatusCode);
        Assert.Empty(app.Storage.Borradas);
        using var verificar=app.Services.CreateScope();
        Assert.True(await verificar.ServiceProvider.GetRequiredService<PcpShootingDbContext>().Productos.AnyAsync(p=>p.Id==id));
    }
}
