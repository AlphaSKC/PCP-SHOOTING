using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Domain.Services;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;
using PCP_Shooting.Persistence.Services;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class PaquetesConcurrenciaPostgresTests(PostgresEscriturasFixture fixture) : IClassFixture<PostgresEscriturasFixture>
{
    [PostgresFact]
    public async Task CambioDeCategoriaConcurrenteNoPermiteGuardarComposicionInvalida()
    {
        var productoId = Guid.NewGuid();
        var sku = "RACE-" + Guid.NewGuid().ToString("N");
        var codigo = "RACE-" + Guid.NewGuid().ToString("N");
        await using var baseDatos = fixture.CrearContext();
        baseDatos.Productos.Add(new Producto { Id = productoId, Sku = sku, Nombre = "Material", Categoria = CategoriaProducto.Consumible });
        await baseDatos.SaveChangesAsync();
        var interceptor = new CambiarCategoriaDuranteLectura(async () =>
        {
            await using var otro = fixture.CrearContext();
            await new EscriturasService(otro, new TransaccionesPersistence(otro)).GuardarProductoAsync(productoId,
                new GuardarProducto { Sku = sku, Nombre = "Material reclasificado", Categoria = CategoriaProducto.SinClasificar }, default);
        });
        var opciones = new DbContextOptionsBuilder<PcpShootingDbContext>().UseNpgsql(baseDatos.Database.GetConnectionString())
            .AddInterceptors(interceptor).Options;
        await using var db = new PcpShootingDbContext(opciones);
        await Assert.ThrowsAsync<ValidationException>(() => new PaquetesService(db, TimeProvider.System, new TransaccionesPersistence(db)).GuardarAsync(null,
            new GuardarPaquete
            {
                Codigo = codigo, Nombre = "Paquete", IncluyeMarkdown = "Material", PrecioPorPersona = 100m, Activo = true,
                Consumibles = new() { new ConsumiblePaqueteDatos { ProductoId = productoId, CantidadPorPersona = 1 } }
            }, default));
        Assert.True(interceptor.Ejecutado);
        await using var comprobacion = fixture.CrearContext();
        Assert.False(await comprobacion.Paquetes.AnyAsync(p => p.Codigo == codigo.ToUpperInvariant()));
        Assert.False(await comprobacion.ConsumiblesPaquete.AnyAsync(c => c.ProductoId == productoId));
        Assert.Equal(CategoriaProducto.SinClasificar, (await comprobacion.Productos.SingleAsync(p => p.Id == productoId)).Categoria);
    }

    private sealed class CambiarCategoriaDuranteLectura(Func<Task> editar) : DbCommandInterceptor
    {
        public bool Ejecutado { get; private set; }
        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
            DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (!Ejecutado && command.CommandText.Contains("FROM \"Productos\""))
            {
                Ejecutado = true;
                await editar();
            }
            return result;
        }
    }
}
