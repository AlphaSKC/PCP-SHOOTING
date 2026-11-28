using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public class GaleriaProductoTests
{
    [Fact]
    public async Task AgregarOrdenarYQuitarConservaPrincipalEIdempotencia()
    {
        using var app=new ContenidoImagenesApiTests.App(); using var admin=app.Cliente("admin");
        var id=Guid.NewGuid();
        using(var scope=app.Services.CreateScope())
        {
            var db=scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>();
            db.Productos.Add(new Producto { Id=id,Sku=id.ToString(),Nombre="Galeria",Categoria=CategoriaProducto.Alimento }); await db.SaveChangesAsync();
        }
        var a=Guid.NewGuid(); var b=Guid.NewGuid();
        Assert.Equal(HttpStatusCode.OK,(await Subir(admin,id,a)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await Subir(admin,id,b,true)).StatusCode);
        var ficha=await admin.GetFromJsonAsync<JsonElement>($"/api/productos/{id}");
        Assert.Equal(2,ficha.GetProperty("imagenes").GetArrayLength());
        Assert.Equal(a,ficha.GetProperty("imagenes")[0].GetProperty("id").GetGuid());
        Assert.Equal(HttpStatusCode.Conflict,(await admin.DeleteAsync($"/api/productos/{id}/imagenes/{a}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await admin.PutAsJsonAsync($"/api/productos/{id}/imagenes/orden",new { ids=new[]{b,a} })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,(await admin.DeleteAsync($"/api/productos/{id}/imagenes/{a}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await Subir(admin,id,a)).StatusCode);
        ficha=await admin.GetFromJsonAsync<JsonElement>($"/api/productos/{id}");
        Assert.Single(ficha.GetProperty("imagenes").EnumerateArray());
        Assert.Equal(b,ficha.GetProperty("imagenes")[0].GetProperty("id").GetGuid());
        Assert.Equal(HttpStatusCode.Conflict,(await admin.DeleteAsync($"/api/productos/{id}/imagenes/{b}")).StatusCode);
        foreach(var ruta in new[]{"/api/productos","/api/administracion/productos"})
        {
            var listado=await admin.GetFromJsonAsync<JsonElement>(ruta);
            var producto=listado.GetProperty("elementos").EnumerateArray().Single(p=>p.GetProperty("id").GetGuid()==id);
            Assert.Equal(b,producto.GetProperty("imagenes")[0].GetProperty("id").GetGuid());
            Assert.Equal(producto.GetProperty("imagenUrl").GetString(),producto.GetProperty("imagenes")[0].GetProperty("url").GetString());
        }
        Assert.Equal(HttpStatusCode.NoContent,(await admin.DeleteAsync($"/api/productos/{id}")).StatusCode);
    }

    [Fact]
    public async Task LimiteIncluyePendientesReemplazarConservaOchoYReintentosNoDuplican()
    {
        using var app=new ContenidoImagenesApiTests.App(); using var admin=app.Cliente("admin");
        var id=Guid.NewGuid(); await Crear(app,id,CategoriaProducto.Playera);
        for(var i=0;i<7;i++) Assert.Equal(HttpStatusCode.OK,(await Subir(admin,id,Guid.NewGuid(),true)).StatusCode);
        app.Storage.Fallar=true; var pendiente=Guid.NewGuid();
        Assert.Equal(HttpStatusCode.BadGateway,(await Subir(admin,id,pendiente,true)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await Subir(admin,id,Guid.NewGuid(),true)).StatusCode);
        Assert.Equal(8,app.Storage.Intentos);
        Assert.Equal(HttpStatusCode.Conflict,(await Subir(admin,id,pendiente,false)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await Subir(admin,id,pendiente,true)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await Subir(admin,id,pendiente,true)).StatusCode);
        var reemplazo=Guid.NewGuid(); Assert.Equal(HttpStatusCode.OK,(await Subir(admin,id,reemplazo)).StatusCode);
        var ficha=await admin.GetFromJsonAsync<JsonElement>($"/api/productos/{id}");
        Assert.Equal(8,ficha.GetProperty("imagenes").GetArrayLength());
        Assert.Equal(reemplazo,ficha.GetProperty("imagenes")[0].GetProperty("id").GetGuid());
    }

    [Theory]
    [InlineData("admin",CategoriaProducto.Playera,HttpStatusCode.OK)]
    [InlineData("recepcion",CategoriaProducto.Alimento,HttpStatusCode.OK)]
    [InlineData("recepcion",CategoriaProducto.Playera,HttpStatusCode.Forbidden)]
    [InlineData("cliente",CategoriaProducto.Alimento,HttpStatusCode.Forbidden)]
    [InlineData("inactivo",CategoriaProducto.Alimento,HttpStatusCode.Forbidden)]
    [InlineData(null,CategoriaProducto.Alimento,HttpStatusCode.Unauthorized)]
    public async Task PermisosDeOrdenYRetirada(string? actor,CategoriaProducto categoria,HttpStatusCode esperado)
    {
        using var app=new ContenidoImagenesApiTests.App(); using var admin=app.Cliente("admin");
        var id=Guid.NewGuid(); await Crear(app,id,categoria); var a=Guid.NewGuid(); var b=Guid.NewGuid();
        await Subir(admin,id,a); await Subir(admin,id,b,true);
        using var cliente=app.Cliente(actor);
        Assert.Equal(esperado,(await cliente.PutAsJsonAsync($"/api/productos/{id}/imagenes/orden",new { ids=new[]{a,b} })).StatusCode);
        Assert.Equal(esperado==HttpStatusCode.OK ? HttpStatusCode.NoContent:esperado,(await cliente.DeleteAsync($"/api/productos/{id}/imagenes/{b}")).StatusCode);
    }

    [Fact]
    public async Task RechazaOrdenIncompletoDuplicadoOAjenoSinCambiarPrincipal()
    {
        using var app=new ContenidoImagenesApiTests.App(); using var admin=app.Cliente("admin");
        var id=Guid.NewGuid(); await Crear(app,id,CategoriaProducto.Alimento); var a=Guid.NewGuid(); var b=Guid.NewGuid();
        await Subir(admin,id,a); await Subir(admin,id,b,true);
        foreach(var ids in new[]{Array.Empty<Guid>(),new[]{a,a},new[]{Guid.Empty}})
            Assert.Equal(HttpStatusCode.BadRequest,(await admin.PutAsJsonAsync($"/api/productos/{id}/imagenes/orden",new {ids})).StatusCode);
        foreach(var ids in new[]{new[]{a},new[]{a,Guid.NewGuid()}})
            Assert.Equal(HttpStatusCode.Conflict,(await admin.PutAsJsonAsync($"/api/productos/{id}/imagenes/orden",new {ids})).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await admin.DeleteAsync($"/api/productos/{id}/imagenes/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await admin.PutAsJsonAsync($"/api/productos/{id}",new {sku=id.ToString(),nombre="Galeria",categoria=1,esVendible=false,imagenUrl=(string?)null})).StatusCode);
    }

    [Fact]
    public async Task FotoSecundariaCompartidaBloqueaBorradoCompleto()
    {
        using var app=new ContenidoImagenesApiTests.App(); using var admin=app.Cliente("admin");
        var id=Guid.NewGuid(); var otro=Guid.NewGuid(); await Crear(app,id,CategoriaProducto.Playera); await Crear(app,otro,CategoriaProducto.Playera);
        var imagen=Guid.NewGuid(); await Subir(admin,id,imagen);
        using(var scope=app.Services.CreateScope())
        {
            var db=scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>();
            db.Set<FotoProducto>().Add(new FotoProducto { Id=Guid.NewGuid(),ProductoId=otro,Url=$"https://imagenes.ejemplo.test/productos/{id:N}/{imagen:N}.png",Orden=1 });
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Conflict,(await admin.DeleteAsync($"/api/productos/{id}")).StatusCode);
        Assert.Empty(app.Storage.Borradas);
    }

    private static async Task Crear(ContenidoImagenesApiTests.App app,Guid id,CategoriaProducto categoria)
    {
        using var scope=app.Services.CreateScope(); var db=scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>();
        db.Productos.Add(new Producto { Id=id,Sku=id.ToString(),Nombre="Galeria",Categoria=categoria }); await db.SaveChangesAsync();
    }

    internal static async Task<HttpResponseMessage> Subir(HttpClient cliente,Guid producto,Guid operacion,bool agregar=false)
    {
        using var form=new MultipartFormDataContent(); form.Add(new StringContent(operacion.ToString()),"idOperacion");
        form.Add(new StringContent(agregar.ToString()),"agregar");
        var archivo=new ByteArrayContent(ImagenesTests.Png); archivo.Headers.ContentType=new("image/png"); form.Add(archivo,"archivo","foto.png");
        return await cliente.PostAsync($"/api/productos/{producto}/imagen",form);
    }
}
