using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class InventarioGeneralPostgresTests(PostgresEscriturasFixture fixture)
    : IClassFixture<PostgresEscriturasFixture>
{
    [PostgresFact]
    public async Task ConsultaSaldosYFiltrosEnPostgreSqlRespetandoRecepcion()
    {
        using var factory = new OperacionesFactory(fixture);
        using var admin = factory.Cliente(OperacionesFactory.AdminId);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/usuarios/yo")).StatusCode);
        var prefijo = Guid.NewGuid().ToString("N");
        var alimentos = new Producto
        {
            Id = Guid.NewGuid(), Sku = prefijo + "-A", Nombre = "A " + prefijo,
            Categoria = CategoriaProducto.Alimento, StockMinimo = 7, EsVendible = true, Precio = 10
        };
        var playera = new Producto
        {
            Id = Guid.NewGuid(), Sku = prefijo + "-B", Nombre = "B " + prefijo,
            Categoria = CategoriaProducto.Playera, StockMinimo = 1, Activo = false
        };
        var amplio = new Producto { Id = Guid.NewGuid(), Sku = prefijo + "-C", Nombre = "C " + prefijo };
        await using (var db = fixture.CrearContext())
        {
            db.Productos.AddRange(alimentos, playera, amplio);
            foreach (var cantidad in new[] { 10, -3 })
                db.MovimientosInventario.Add(new MovimientoInventario
                {
                    Id = Guid.NewGuid(), ProductoId = alimentos.Id, VariacionCantidad = cantidad,
                    Tipo = cantidad > 0 ? TipoMovimientoInventario.Entrada : TipoMovimientoInventario.Salida,
                    RegistradoEnUtc = DateTime.UtcNow
                });
            foreach (var cantidad in new[] { int.MaxValue, int.MaxValue })
                db.MovimientosInventario.Add(new MovimientoInventario
                {
                    Id = Guid.NewGuid(), ProductoId = amplio.Id, VariacionCantidad = cantidad,
                    Tipo = TipoMovimientoInventario.Entrada, RegistradoEnUtc = DateTime.UtcNow
                });
            var perfil = await db.PerfilesUsuario.SingleAsync(p => p.Id == OperacionesFactory.OtroId);
            perfil.Rol = RolUsuario.Recepcion;
            await db.SaveChangesAsync();
        }
        var lista = await admin.GetFromJsonAsync<PaginaConsulta<ExistenciaProducto>>("/api/inventario?busqueda=" + prefijo);
        Assert.Equal(3, lista!.Total);
        Assert.Equal(7, lista.Elementos.Single(p => p.ProductoId == alimentos.Id).Existencia);
        Assert.Equal((long)int.MaxValue * 2, lista.Elementos.Single(p => p.ProductoId == amplio.Id).Existencia);
        var alerta = await admin.GetFromJsonAsync<PaginaConsulta<ExistenciaProducto>>(
            "/api/inventario?busqueda=" + prefijo + "&bajoMinimo=true&tamanoPagina=1");
        Assert.Equal(2, alerta!.Total);
        Assert.Single(alerta.Elementos);
        var sinExistencias = await admin.GetFromJsonAsync<PaginaConsulta<ExistenciaProducto>>(
            "/api/inventario?busqueda=" + prefijo + "&conExistencias=false&activo=false");
        Assert.Equal(playera.Id, Assert.Single(sinExistencias!.Elementos).ProductoId);
        using var recepcion = factory.Cliente(OperacionesFactory.OtroId);
        var propia = await recepcion.GetFromJsonAsync<PaginaConsulta<ExistenciaProducto>>(
            "/api/inventario?busqueda=" + prefijo);
        Assert.Equal(1, propia!.Total);
        Assert.Equal(alimentos.Id, Assert.Single(propia.Elementos).ProductoId);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await recepcion.GetAsync("/api/inventario?categoria=2&busqueda=" + prefijo)).StatusCode);
    }
}
