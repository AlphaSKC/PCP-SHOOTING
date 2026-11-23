using PCP_Shooting.Domain.Models;
using PCP_Shooting.Domain.Services;
using PCP_Shooting.Persistence.Models;
using PCP_Shooting.Persistence.Services;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class FrontendContractPostgresTests(PostgresEscriturasFixture fixture) : IClassFixture<PostgresEscriturasFixture>
{
    [PostgresFact]
    public async Task F07PedidoAdministrativoYReporteIncluyenPerfilSinTelefono()
    {
        await using var db = fixture.CrearContext();
        var usuario = new PerfilUsuario { Id = Guid.NewGuid(), Nombre = "Cliente sin teléfono" };
        var pedido = new Pedido { Id = Guid.NewGuid(), UsuarioId = usuario.Id, CreadoEnUtc = DateTime.UtcNow };
        var producto = new Producto { Id = Guid.NewGuid(), Sku = "F07-PRUEBA", Nombre = "Playera", EsVendible = true, Precio = 100 };
        pedido.Detalles.Add(new DetallePedido { Id = Guid.NewGuid(), Producto = producto, Cantidad = 1, PrecioUnitario = 100 });
        db.PerfilesUsuario.Add(usuario); db.Pedidos.Add(pedido); await db.SaveChangesAsync();
        var servicio = new AdministracionService(db, new PerfilesService(new PerfilesPersistence(db)));
        var filtro = new FiltroPedidos { UsuarioId = usuario.Id };
        var resumen = Assert.Single((await servicio.ListarPedidosAsync(filtro, default)).Elementos);
        Assert.Equal(usuario.Nombre, resumen.NombreCliente);
        Assert.Null(resumen.TelefonoCliente);
        Assert.Equal(resumen, (await servicio.ConsultarPedidoAsync(pedido.Id, default))!.Pedido);
        var reporte = await new ReportesService(db).PedidosAsync(filtro, false, default);
        var fila = Assert.Single(reporte.Datos.Elementos);
        Assert.Equal(resumen.Id, fila.Id);
        Assert.Equal(resumen.NombreCliente, fila.NombreCliente);
        Assert.Equal(resumen.TelefonoCliente, fila.TelefonoCliente);
        Assert.Contains(producto.Nombre, fila.Productos);
    }
}
