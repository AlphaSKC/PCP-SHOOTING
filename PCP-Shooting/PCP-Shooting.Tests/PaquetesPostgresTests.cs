using PCP_Shooting.Persistence.Services;
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Domain.Services;
using PCP_Shooting.Persistence.Migrations;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class PaquetesPostgresTests(PostgresEscriturasFixture fixture) : IClassFixture<PostgresEscriturasFixture>
{
    private const string Gestion = "/api/administracion/paquetes";
    private static GuardarPaquete Datos(string codigo, Guid producto, Guid? version = null, int cantidad = 2, decimal precio = 150.50m) => new()
    {
        Codigo = codigo, Nombre = "Actividad de prueba PostgreSQL", IncluyeMarkdown = "- Material incluido", PrecioPorPersona = precio,
        PrecioSocioCartaq = 120.25m, Activo = true, Version = version,
        Consumibles = new() { new ConsumiblePaqueteDatos { ProductoId = producto, CantidadPorPersona = cantidad } }
    };

    private static async Task<Guid> Consumible(HttpClient admin)
    {
        var respuesta = await admin.PostAsJsonAsync("/api/productos", new GuardarProducto
        {
            Sku = "CON-" + Guid.NewGuid().ToString("N"), Nombre = "Material interno", Categoria = CategoriaProducto.Consumible, EsVendible = false
        });
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return (await respuesta.Content.ReadFromJsonAsync<EscrituraResultado>())!.Id;
    }

    [PostgresFact]
    public async Task ConsumiblesInternosSeGestionanEnInventarioYPaquetesSinVentaNiConsumoPrematuro()
    {
        using var factory = new OperacionesFactory(fixture);
        using var admin = factory.Cliente(OperacionesFactory.AdminId);
        var producto = await Consumible(admin);
        var codigo = "PK-" + Guid.NewGuid().ToString("N");
        var respuesta = await admin.PostAsJsonAsync(Gestion, Datos(codigo, producto));
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var paquete = (await respuesta.Content.ReadFromJsonAsync<PaqueteGestion>())!;
        Assert.Equal(150.50m, paquete.PrecioPorPersona);
        Assert.Equal(120.25m, paquete.PrecioSocioCartaq);
        Assert.Equal(2, Assert.Single(paquete.Consumibles).CantidadPorPersona);
        using var publico = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.OK, (await publico.GetAsync("/api/paquetes/" + paquete.Id)).StatusCode);
        var lista = await publico.GetFromJsonAsync<PaginaConsulta<PaquetePublico>>("/api/paquetes?busqueda=" + codigo);
        Assert.Equal(paquete.Id, Assert.Single(lista!.Elementos).Id);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync("/api/productos/" + producto, new GuardarProducto
        {
            Sku = "CAMBIO-" + Guid.NewGuid().ToString("N"), Nombre = "Intento", Categoria = CategoriaProducto.SinClasificar
        })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/productos", new GuardarProducto
        {
            Sku = "VENTA-" + Guid.NewGuid().ToString("N"), Nombre = "Intento de venta", Categoria = CategoriaProducto.Consumible, EsVendible = true, Precio = 1m
        })).StatusCode);
        await using var db = fixture.CrearContext();
        Assert.False(await db.MovimientosInventario.AnyAsync(m => m.ProductoId == producto));
        Assert.Contains(await db.EventosAuditoria.Where(e => e.EntidadId == paquete.Id).ToListAsync(), e => e.ActorId == OperacionesFactory.AdminId);
    }

    [PostgresFact]
    public async Task SustituirConsumiblesEsAtomicoYNoDejaReferenciasAnteriores()
    {
        using var factory = new OperacionesFactory(fixture);
        using var admin = factory.Cliente(OperacionesFactory.AdminId);
        var uno = await Consumible(admin);
        var dos = await Consumible(admin);
        var codigo = "EDIT-" + Guid.NewGuid().ToString("N");
        var respuesta = await admin.PostAsJsonAsync(Gestion, Datos(codigo, uno));
        var original = (await respuesta.Content.ReadFromJsonAsync<PaqueteGestion>())!;
        var editado = await admin.PutAsJsonAsync(Gestion + "/" + original.Id, Datos(codigo, dos, original.Version, 5));
        Assert.True(editado.StatusCode == HttpStatusCode.OK, await editado.Content.ReadAsStringAsync());
        var actual = (await editado.Content.ReadFromJsonAsync<PaqueteGestion>())!;
        Assert.Equal(dos, Assert.Single(actual.Consumibles).ProductoId);
        await using var db = fixture.CrearContext();
        Assert.Equal(1, await db.ConsumiblesPaquete.CountAsync(c => c.PaqueteId == original.Id));
        Assert.False(await db.ConsumiblesPaquete.AnyAsync(c => c.PaqueteId == original.Id && c.ProductoId == uno));
    }

    [PostgresFact]
    public async Task VersionProtegePrecioYComposicionFrenteAEdicionesConcurrentes()
    {
        using var factory = new OperacionesFactory(fixture);
        using var admin = factory.Cliente(OperacionesFactory.AdminId);
        var producto = await Consumible(admin);
        var codigo = "VERSION-" + Guid.NewGuid().ToString("N");
        var respuesta = await admin.PostAsJsonAsync(Gestion, Datos(codigo, producto));
        var original = (await respuesta.Content.ReadFromJsonAsync<PaqueteGestion>())!;
        await using var uno = fixture.CrearContext();
        await using var dos = fixture.CrearContext();
        await uno.Paquetes.Include(p => p.Consumibles).LoadAsync();
        await dos.Paquetes.Include(p => p.Consumibles).LoadAsync();
        await new PaquetesService(uno, TimeProvider.System, new TransaccionesPersistence(uno)).GuardarAsync(original.Id, Datos(codigo, producto, original.Version, 3, 140m), default);
        var error = await Assert.ThrowsAsync<OperacionException>(() => new PaquetesService(dos, TimeProvider.System, new TransaccionesPersistence(dos))
            .GuardarAsync(original.Id, Datos(codigo, producto, original.Version, 7, 160m), default));
        Assert.Equal(409, error.Status);
        await using var comprobacion = fixture.CrearContext();
        var guardado = await comprobacion.Paquetes.Include(p => p.Consumibles).SingleAsync(p => p.Id == original.Id);
        Assert.Equal(140m, guardado.PrecioPorPersona);
        Assert.Equal(3, Assert.Single(guardado.Consumibles).CantidadPorPersona);
    }

    [PostgresFact]
    public async Task MigracionProtegeTablasYNoPermiteConsumiblesVendibles()
    {
        await using var db = fixture.CrearContext();
        var esquema = await db.Database.SqlQueryRaw<string>("SELECT current_schema() AS \"Value\"").SingleAsync();
        Assert.StartsWith("pcp_test_", esquema);
        await db.Database.ExecuteSqlRawAsync("DROP TABLE \"ConsumiblesPaquete\", \"Paquetes\"");
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"Productos\" DROP CONSTRAINT \"CK_Producto_Consumible\"");
        var migracion = new PaquetesYConsumibles();
        foreach (var comando in db.GetService<IMigrationsSqlGenerator>().Generate(migracion.UpOperations, db.Model))
            await db.Database.ExecuteSqlRawAsync(comando.CommandText);
        var protegidas = await db.Database.SqlQueryRaw<int>("""
            SELECT count(*)::int AS "Value" FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE n.nspname = current_schema() AND c.relname IN ('Paquetes', 'ConsumiblesPaquete')
                AND c.relrowsecurity AND NOT has_table_privilege('anon', c.oid, 'SELECT')
                AND NOT has_table_privilege('authenticated', c.oid, 'SELECT')
            """).SingleAsync();
        Assert.Equal(2, protegidas);
        db.Productos.Add(new Producto { Id = Guid.NewGuid(), Sku = "INVALIDO", Nombre = "Inválido", Categoria = CategoriaProducto.Consumible, EsVendible = true, Precio = 1m });
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal("CK_Producto_Consumible", Assert.IsType<PostgresException>(error.InnerException).ConstraintName);
    }
}
