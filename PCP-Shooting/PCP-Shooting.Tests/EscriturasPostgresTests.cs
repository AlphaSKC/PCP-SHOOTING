using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Domain.Services;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;
using PCP_Shooting.Persistence.Services;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("PCP_RUN_POSTGRES_TESTS") != "1")
            Skip = "Activa PCP_RUN_POSTGRES_TESTS=1 para probar en un esquema temporal de PostgreSQL.";
    }
}

public sealed class EscriturasPostgresTests(PostgresEscriturasFixture fixture)
    : IClassFixture<PostgresEscriturasFixture>
{
    [PostgresFact]
    public async Task FlujoCompletoConReintentosDescuentaInventarioUnaSolaVez()
    {
        await using var db = fixture.CrearContext();
        var service = Servicio(db);
        var (usuario, producto) = await Preparar(db, service, 5);
        var solicitud = Pedido(producto, 2);
        var pedido = await service.CrearPedidoAsync(usuario, solicitud, default);
        Assert.Equal(pedido.Id, (await service.CrearPedidoAsync(usuario, solicitud, default)).Id);
        await Conflicto(() => service.RecogerPedidoAsync(pedido.Id, default));
        var registro = new RegistrarPago { IdOperacion = Guid.NewGuid(), ReferenciaExterna = Guid.NewGuid().ToString() };
        var pago = await service.RegistrarPagoAsync(pedido.Id, registro, default);
        Assert.Equal(pago.Id, (await service.RegistrarPagoAsync(pedido.Id, registro, default)).Id);
        await service.ConfirmarPagoAsync(pago.Id, default);
        await service.ConfirmarPagoAsync(pago.Id, default);
        await service.RecogerPedidoAsync(pedido.Id, default);
        await service.RecogerPedidoAsync(pedido.Id, default);
        Assert.Equal(3, await Stock(db, producto));
        Assert.Single(await db.MovimientosInventario.Where(m => m.DetallePedidoId != null && m.ProductoId == producto).ToListAsync());
        var guardado = await db.Pedidos.AsNoTracking().SingleAsync(p => p.Id == pedido.Id);
        Assert.Equal(EstadoPedido.Recogido, guardado.Estado);
        Assert.NotNull(guardado.RecogidoEnUtc);
        Assert.Equal(25m, (await db.Pagos.SingleAsync(p => p.Id == pago.Id)).Importe);
    }

    [PostgresFact]
    public async Task PrecioDelPedidoSeConservaAunqueCambieElCatalogo()
    {
        await using var db = fixture.CrearContext();
        var service = Servicio(db);
        var (usuario, producto) = await Preparar(db, service, 3);
        var pedido = await service.CrearPedidoAsync(usuario, Pedido(producto), default);
        var original = await db.Productos.AsNoTracking().SingleAsync(p => p.Id == producto);
        await service.GuardarProductoAsync(producto, new GuardarProducto
        {
            Sku = original.Sku, Nombre = "Editado", Precio = 90, EsVendible = true
        }, default);
        var pago = await Pago(service, pedido.Id);
        Assert.Equal(12.50m, (await db.Pagos.SingleAsync(p => p.Id == pago.Id)).Importe);
    }

    [PostgresFact]
    public async Task PagoParcialSeRechaza()
    {
        await using var db = fixture.CrearContext();
        var service = Servicio(db);
        var (usuario, producto) = await Preparar(db, service, 1);
        var pedido = await service.CrearPedidoAsync(usuario, Pedido(producto), default);
        var pago = await Pago(service, pedido.Id);
        var entidad = await db.Pagos.SingleAsync(p => p.Id == pago.Id);
        entidad.Importe = 1;
        await db.SaveChangesAsync();
        await Conflicto(() => service.ConfirmarPagoAsync(pago.Id, default));
        Assert.Equal(1, await Stock(db, producto));
    }

    [PostgresFact]
    public async Task SinStockRevierteTodoElPagoYPermiteOtraOperacion()
    {
        await using var db = fixture.CrearContext();
        var service = Servicio(db);
        var (usuario, producto) = await Preparar(db, service, 1);
        var segundo = await Producto(service);
        var solicitud = Pedido(producto);
        solicitud.Partidas.Add(new PartidaPedido { ProductoId = segundo, Cantidad = 1 });
        var pedido = await service.CrearPedidoAsync(usuario, solicitud, default);
        var pago = await Pago(service, pedido.Id);
        await Conflicto(() => service.ConfirmarPagoAsync(pago.Id, default));
        Assert.Equal(EstadoPago.Pendiente, (await db.Pagos.AsNoTracking().SingleAsync(p => p.Id == pago.Id)).Estado);
        Assert.Equal(EstadoPedido.PendientePago, (await db.Pedidos.AsNoTracking().SingleAsync(p => p.Id == pedido.Id)).Estado);
        await service.RegistrarMovimientoAsync(producto, Entrada(1), default);
        Assert.Equal(2, await Stock(db, producto));
        Assert.Empty(await db.MovimientosInventario.Where(m => m.ProductoId == producto && m.DetallePedidoId != null).ToListAsync());
    }

    [PostgresFact]
    public async Task DosPagosConcurrentesNoVendenLaMismaExistencia()
    {
        await using var db = fixture.CrearContext();
        var service = Servicio(db);
        var (usuario, producto) = await Preparar(db, service, 1);
        var pedido1 = await service.CrearPedidoAsync(usuario, Pedido(producto), default);
        var pedido2 = await service.CrearPedidoAsync(usuario, Pedido(producto), default);
        var pago1 = await Pago(service, pedido1.Id);
        var pago2 = await Pago(service, pedido2.Id);
        async Task<bool> Confirmar(Guid id)
        {
            await using var otroDb = fixture.CrearContext();
            try
            {
                await Servicio(otroDb).ConfirmarPagoAsync(id, default);
                return true;
            }
            catch (OperacionException exception) when (exception.Status == 409) { return false; }
        }
        var resultados = await Task.WhenAll(Confirmar(pago1.Id), Confirmar(pago2.Id));
        Assert.Single(resultados, r => r);
        Assert.Equal(0, await Stock(db, producto));
    }

    [PostgresFact]
    public async Task MovimientosIdempotentesYSalidasSinStockSeRechazan()
    {
        await using var db = fixture.CrearContext();
        var service = Servicio(db);
        var (_, producto) = await Preparar(db, service, 1);
        var entrada = Entrada(2);
        await service.RegistrarMovimientoAsync(producto, entrada, default);
        await service.RegistrarMovimientoAsync(producto, entrada, default);
        Assert.Equal(3, await Stock(db, producto));
        await Conflicto(() => service.RegistrarMovimientoAsync(producto, new RegistrarMovimiento
        {
            IdOperacion = Guid.NewGuid(), Tipo = TipoMovimientoInventario.Salida, VariacionCantidad = -4, Motivo = "Prueba"
        }, default));
        Assert.Equal(3, await Stock(db, producto));
    }

    [PostgresFact]
    public async Task RenovacionManualValidaPagoPropiedadVigenciaYReintentos()
    {
        await using var db = fixture.CrearContext();
        var service = Servicio(db);
        var (usuario, producto) = await Preparar(db, service, 3);
        var pedido = await service.CrearPedidoAsync(usuario, Pedido(producto), default);
        var pago = await Pago(service, pedido.Id);
        var inicio = new DateOnly(2026, 1, 1);
        var solicitud = new RenovarMembresia
        {
            IdOperacion = Guid.NewGuid(), UsuarioId = usuario, PagoId = pago.Id,
            InicioVigencia = inicio, FinVigenciaExclusivo = inicio.AddYears(1)
        };
        await Conflicto(() => service.RenovarMembresiaAsync(solicitud, default));
        await service.ConfirmarPagoAsync(pago.Id, default);
        await service.RenovarMembresiaAsync(solicitud, default);
        await service.RenovarMembresiaAsync(solicitud, default);
        Assert.Single(await db.Membresias.Where(m => m.UsuarioId == usuario).ToListAsync());
        await Conflicto(() => service.RenovarMembresiaAsync(new RenovarMembresia
        {
            IdOperacion = Guid.NewGuid(), UsuarioId = usuario, PagoId = pago.Id,
            InicioVigencia = inicio.AddYears(1), FinVigenciaExclusivo = inicio.AddYears(2)
        }, default));
        var pedido2 = await service.CrearPedidoAsync(usuario, Pedido(producto), default);
        var pago2 = await Pago(service, pedido2.Id);
        await service.ConfirmarPagoAsync(pago2.Id, default);
        await Conflicto(() => service.RenovarMembresiaAsync(new RenovarMembresia
        {
            IdOperacion = Guid.NewGuid(), UsuarioId = usuario, PagoId = pago2.Id,
            InicioVigencia = inicio.AddMonths(6), FinVigenciaExclusivo = inicio.AddYears(2)
        }, default));
        await service.RenovarMembresiaAsync(new RenovarMembresia
        {
            IdOperacion = Guid.NewGuid(), UsuarioId = usuario, PagoId = pago2.Id,
            InicioVigencia = inicio.AddYears(1), FinVigenciaExclusivo = inicio.AddYears(2)
        }, default);
        Assert.Equal(2, await db.Membresias.CountAsync(m => m.UsuarioId == usuario));
    }

    [PostgresFact]
    public async Task ReferenciaReutilizadaEIdentificadorConContenidoDistintoSeRechazan()
    {
        await using var db = fixture.CrearContext();
        var service = Servicio(db);
        var (usuario, producto) = await Preparar(db, service, 4);
        var solicitud = Pedido(producto);
        var pedido = await service.CrearPedidoAsync(usuario, solicitud, default);
        var distinto = new CrearPedido
        {
            IdOperacion = solicitud.IdOperacion,
            Partidas = new() { new PartidaPedido { ProductoId = producto, Cantidad = 2 } }
        };
        await Conflicto(() => service.CrearPedidoAsync(usuario, distinto, default));
        var referencia = Guid.NewGuid().ToString();
        await service.RegistrarPagoAsync(pedido.Id, new RegistrarPago { IdOperacion = Guid.NewGuid(), ReferenciaExterna = referencia }, default);
        await Conflicto(() => service.RegistrarPagoAsync(pedido.Id, new RegistrarPago { IdOperacion = Guid.NewGuid(), ReferenciaExterna = referencia }, default));
    }

    [PostgresFact]
    public async Task ArticuloInformativoNoPuedeAgregarseAUnPedido()
    {
        await using var db = fixture.CrearContext();
        var service = Servicio(db);
        var (usuario, _) = await Preparar(db, service, 1);
        var informativo = await service.GuardarProductoAsync(null, new GuardarProducto
        {
            Sku = Guid.NewGuid().ToString(), Nombre = "Ficha informativa"
        }, default);
        await Conflicto(() => service.CrearPedidoAsync(usuario, Pedido(informativo.Id), default));
    }


    [PostgresFact]
    public async Task CancelacionYConfirmacionConcurrentesConservanEstadoEInventario()
    {
        await using var db = fixture.CrearContext();
        var service = Servicio(db);
        var (usuario, producto) = await Preparar(db, service, 1);
        var pedido = await service.CrearPedidoAsync(usuario, Pedido(producto), default);
        var pago = await Pago(service, pedido.Id);
        var inicio = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<bool> Ejecutar(bool confirmar)
        {
            await inicio.Task;
            await using var otroDb = fixture.CrearContext();
            try
            {
                if (confirmar)
                    await Servicio(otroDb).ConfirmarPagoAsync(pago.Id, default);
                else
                    await Servicio(otroDb).CancelarPedidoAsync(usuario, pedido.Id, default);
                return true;
            }
            catch (OperacionException exception) when (exception.Status == 409)
            {
                return false;
            }
        }

        var confirmacion = Ejecutar(true);
        var cancelacion = Ejecutar(false);
        inicio.SetResult();
        var resultados = await Task.WhenAll(confirmacion, cancelacion);
        Assert.Single(resultados, resultado => resultado);

        var pedidoGuardado = await db.Pedidos.AsNoTracking().SingleAsync(p => p.Id == pedido.Id);
        var pagoGuardado = await db.Pagos.AsNoTracking().SingleAsync(p => p.Id == pago.Id);
        if (resultados[0])
        {
            Assert.Equal(EstadoPedido.Pagado, pedidoGuardado.Estado);
            Assert.Equal(EstadoPago.Confirmado, pagoGuardado.Estado);
            Assert.Equal(0, await Stock(db, producto));
        }
        else
        {
            Assert.Equal(EstadoPedido.Cancelado, pedidoGuardado.Estado);
            Assert.Equal(EstadoPago.Cancelado, pagoGuardado.Estado);
            Assert.Equal(1, await Stock(db, producto));
        }
        Assert.Single(await db.EventosAuditoria.Where(e =>
            e.EntidadId == pedido.Id && e.Accion == "Modified").ToListAsync());
    }

    [PostgresFact]
    public async Task ErrorDeSerializacionEnvueltoSeReintenta()
    {
        await using var db = fixture.CrearContext();
        var transacciones = new TransaccionesPersistence(db);
        var intentos = 0;
        var resultado = await transacciones.EjecutarAsync(() =>
        {
            intentos++;
            if (intentos == 1)
                throw new InvalidOperationException("Error transitorio envuelto",
                    new DbUpdateException("Error al guardar",
                        new Npgsql.PostgresException("Conflicto de prueba", "ERROR", "ERROR", "40001")));
            return Task.FromResult(42);
        }, default);
        Assert.Equal(42, resultado);
        Assert.Equal(2, intentos);
    }

    private static EscriturasService Servicio(PcpShootingDbContext db) => new(db, new TransaccionesPersistence(db));
    private static CrearPedido Pedido(Guid producto, int cantidad = 1) => new()
    {
        IdOperacion = Guid.NewGuid(), Partidas = new() { new PartidaPedido { ProductoId = producto, Cantidad = cantidad } }
    };
    private static RegistrarMovimiento Entrada(int cantidad) => new()
    {
        IdOperacion = Guid.NewGuid(), Tipo = TipoMovimientoInventario.Entrada, VariacionCantidad = cantidad, Motivo = "Prueba"
    };
    private static async Task<Guid> Producto(EscriturasService service) =>
        (await service.GuardarProductoAsync(null, new GuardarProducto
        {
            Sku = Guid.NewGuid().ToString(), Nombre = "Articulo de prueba", EsVendible = true, Precio = 12.50m
        }, default)).Id;
    private static async Task<(Guid Usuario, Guid Producto)> Preparar(PcpShootingDbContext db, EscriturasService service, int stock)
    {
        var usuario = Guid.NewGuid();
        db.PerfilesUsuario.Add(new PerfilUsuario { Id = usuario, Nombre = "Prueba" });
        await db.SaveChangesAsync();
        var producto = await Producto(service);
        await service.RegistrarMovimientoAsync(producto, Entrada(stock), default);
        return (usuario, producto);
    }
    private static Task<EscrituraResultado> Pago(EscriturasService service, Guid pedido) =>
        service.RegistrarPagoAsync(pedido, new RegistrarPago
        {
            IdOperacion = Guid.NewGuid(), ReferenciaExterna = Guid.NewGuid().ToString()
        }, default);
    private static Task<long> Stock(PcpShootingDbContext db, Guid producto) =>
        db.MovimientosInventario.Where(m => m.ProductoId == producto).SumAsync(m => (long)m.VariacionCantidad);
    private static async Task Conflicto(Func<Task<EscrituraResultado>> operacion) =>
        Assert.Equal(409, (await Assert.ThrowsAsync<OperacionException>(operacion)).Status);
}

public sealed class PostgresEscriturasFixture : IAsyncLifetime
{
    private readonly string schema = "pcp_test_" + Guid.NewGuid().ToString("N");
    private string? connectionString;

    public PcpShootingDbContext CrearContext(IHttpContextAccessor? solicitud = null) => new(
        new DbContextOptionsBuilder<PcpShootingDbContext>().UseNpgsql(connectionString!).Options, solicitud);

    public async Task InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("PCP_RUN_POSTGRES_TESTS") != "1") return;
        var config = new ConfigurationBuilder().AddUserSecrets("PCP-Shooting-Postgres").Build();
        var builder = new NpgsqlConnectionStringBuilder(config.GetConnectionString("Postgres"));
        builder.SearchPath = schema;
        connectionString = builder.ConnectionString;
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", connection);
        await command.ExecuteNonQueryAsync();
        await using var db = CrearContext();
        await db.Database.ExecuteSqlRawAsync(db.Database.GenerateCreateScript());
    }

    public async Task DisposeAsync()
    {
        if (connectionString is null) return;
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE", connection);
        await command.ExecuteNonQueryAsync();
        NpgsqlConnection.ClearPool(connection);
    }
}