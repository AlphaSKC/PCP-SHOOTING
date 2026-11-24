using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class PaquetesApiTests
{
    private const string Gestion = "/api/administracion/paquetes";
    private static readonly Guid Consumible = Guid.NewGuid();
    private static readonly Guid Playera = Guid.NewGuid();

    private static HttpClient Admin(PerfilesFactory factory)
    {
        var cliente = factory.CrearCliente("admin");
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>();
        db.Productos.AddRange(
            new Producto { Id = Consumible, Sku = "CONSUMIBLE-INTERNO", Nombre = "Material de actividad", Categoria = (CategoriaProducto)4 },
            new Producto { Id = Playera, Sku = "PLAYERA", Nombre = "Playera", Categoria = CategoriaProducto.Playera });
        db.SaveChanges();
        return cliente;
    }

    private static object Datos(string codigo = "PK-PRUEBA", bool activo = false, decimal? precio = 0m, Guid? version = null,
        Guid? productoId = null, int cantidad = 2, decimal? precioSocio = null) => new
    {
        codigo, nombre = "Actividad de prueba", descripcion = "Descripción", incluyeMarkdown = "- Material incluido", requisitosMarkdown = "- Responsiva",
        precioPorPersona = precio, precioSocioCartaq = precioSocio, activo, version,
        consumibles = new[] { new { productoId = productoId ?? Consumible, cantidadPorPersona = cantidad } }
    };

    private static async Task<JsonElement> Crear(HttpClient admin, object datos)
    {
        var respuesta = await admin.PostAsJsonAsync(Gestion, datos);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.NotNull(respuesta.Headers.Location);
        return await respuesta.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task BorradorConPrecioCeroNoSePublicaYActivadoTieneDetallePublicoSinInventarioInterno()
    {
        using var factory = new PerfilesFactory();
        using var admin = Admin(factory);
        using var publico = factory.CrearCliente(null);
        var creado = await Crear(admin, Datos());
        var id = creado.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await publico.GetAsync("/api/paquetes/" + id)).StatusCode);
        Assert.Equal(0, (await publico.GetFromJsonAsync<JsonElement>("/api/paquetes")).GetProperty("total").GetInt32());
        var publicado = await admin.PutAsJsonAsync(Gestion + "/" + id,
            Datos(activo: true, precio: 150.50m, precioSocio: 120.25m, version: creado.GetProperty("version").GetGuid()));
        Assert.Equal(HttpStatusCode.OK, publicado.StatusCode);
        var ficha = await publico.GetFromJsonAsync<JsonElement>("/api/paquetes/" + id);
        Assert.Equal(150.50m, ficha.GetProperty("precioPorPersona").GetDecimal());
        Assert.Equal(120.25m, ficha.GetProperty("precioSocioCartaq").GetDecimal());
        Assert.False(ficha.TryGetProperty("consumibles", out _));
        Assert.False(ficha.TryGetProperty("version", out _));
        var lista = await publico.GetFromJsonAsync<JsonElement>("/api/paquetes?busqueda=ACTIVIDAD&tamanoPagina=1");
        Assert.Equal(1, lista.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task CambiarConsumiblesNoModificaStockYActualizaEnLugarDeDuplicar()
    {
        using var factory = new PerfilesFactory();
        using var admin = Admin(factory);
        var creado = await Crear(admin, Datos());
        var id = creado.GetProperty("id").GetGuid();
        var editado = await admin.PutAsJsonAsync(Gestion + "/" + id, Datos(version: creado.GetProperty("version").GetGuid(), cantidad: 5));
        Assert.Equal(HttpStatusCode.OK, editado.StatusCode);
        var detalle = await editado.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(5, Assert.Single(detalle.GetProperty("consumibles").EnumerateArray()).GetProperty("cantidadPorPersona").GetInt32());
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>();
        Assert.Empty(db.MovimientosInventario);
        Assert.Equal(2, db.EventosAuditoria.Count(e => e.EntidadId == id));
    }

    [Fact]
    public async Task CodigoYVersionEvitanDuplicadosYSobrescrituras()
    {
        using var factory = new PerfilesFactory();
        using var admin = Admin(factory);
        var creado = await Crear(admin, Datos());
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(Gestion, Datos(codigo: "pk-prueba"))).StatusCode);
        var ruta = Gestion + "/" + creado.GetProperty("id").GetGuid();
        var datos = Datos(version: creado.GetProperty("version").GetGuid());
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync(ruta, datos)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync(ruta, datos)).StatusCode);
    }

    [Theory]
    [InlineData(true, 0, 2)]
    [InlineData(false, -1, 2)]
    [InlineData(false, 10.123, 2)]
    [InlineData(false, 10, 0)]
    public async Task RechazaPreciosYCantidadesInvalidos(bool activo, double precio, int cantidad)
    {
        using var factory = new PerfilesFactory();
        using var admin = Admin(factory);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(Gestion, Datos(activo: activo, precio: (decimal)precio, cantidad: cantidad))).StatusCode);
    }

    [Fact]
    public async Task NoAceptaArticulosQueNoSeanConsumiblesNiReferenciasDuplicadas()
    {
        using var factory = new PerfilesFactory();
        using var admin = Admin(factory);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(Gestion, Datos(productoId: Playera))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsJsonAsync(Gestion, Datos(productoId: Guid.NewGuid()))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(Gestion, new
        {
            codigo = "DUP", nombre = "Duplicado", incluyeMarkdown = "Incluye material", consumibles = new[]
            {
                new { productoId = Consumible, cantidadPorPersona = 1 }, new { productoId = Consumible, cantidadPorPersona = 2 }
            }
        })).StatusCode);
    }

    [Fact]
    public async Task ConsumiblesNoAparecenEnCatalogoPublicoYDesactivarlosOcultaPaquetesDependientes()
    {
        using var factory = new PerfilesFactory();
        using var admin = Admin(factory);
        using var publico = factory.CrearCliente(null);
        var creado = await Crear(admin, Datos(activo: true, precio: 100m));
        var catalogo = await publico.GetFromJsonAsync<JsonElement>("/api/productos?categoria=4");
        Assert.Equal(0, catalogo.GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await publico.GetAsync("/api/productos/CONSUMIBLE-INTERNO")).StatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>();
            (await db.Productos.FindAsync(Consumible))!.Activo = false;
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.NotFound, (await publico.GetAsync("/api/paquetes/" + creado.GetProperty("id").GetGuid())).StatusCode);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("cliente", HttpStatusCode.Forbidden)]
    [InlineData("inactivo", HttpStatusCode.Forbidden)]
    public async Task GestionRestringida(string? identidad, HttpStatusCode esperado)
    {
        using var factory = new PerfilesFactory();
        using var cliente = factory.CrearCliente(identidad);
        Assert.Equal(esperado, (await cliente.GetAsync(Gestion)).StatusCode);
        Assert.Equal(esperado, (await cliente.PostAsJsonAsync(Gestion, Datos())).StatusCode);
    }

    [Fact]
    public async Task RecepcionConsultaPaquetesPeroNoLosModificaNiGestionaConsumibles()
    {
        using var factory = new JwtBackendFactory();
        using var recepcion = factory.Cliente(factory.Token(JwtBackendFactory.RecepcionId));
        Assert.Equal(HttpStatusCode.OK, (await recepcion.GetAsync(Gestion)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await recepcion.PostAsJsonAsync(Gestion, Datos())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await recepcion.PostAsJsonAsync("/api/productos", new { sku = "NO", nombre = "Consumible", categoria = 4 })).StatusCode);
    }
}
