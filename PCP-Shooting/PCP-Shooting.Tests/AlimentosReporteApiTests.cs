using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class AlimentosReporteApiTests
{
    [Fact]
    public async Task SumaSoloPartidasDeAlimentosEnPedidosPagadosSinDuplicarTotalesMixtos()
    {
        using var app = new PerfilesFactory();
        using var admin = app.CrearCliente("admin");
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PcpShootingDbContext>();
            var alimento = new Producto { Id = Guid.NewGuid(), Sku = "COMIDA", Nombre = "=Bebida, especial", Categoria = CategoriaProducto.Alimento, Precio = 999m };
            var playera = new Producto { Id = Guid.NewGuid(), Sku = "ROPA", Nombre = "Playera", Categoria = CategoriaProducto.Playera, Precio = 999m };
            db.Productos.AddRange(alimento, playera);
            for (var i = 0; i < 4; i++)
            {
                var pedido = new Pedido { Id = Guid.NewGuid(), UsuarioId = i == 3 ? PerfilesFactory.InactivoId : PerfilesFactory.ClienteId,
                    CreadoEnUtc = new DateTime(2026, 11, 24, 12, 0, 0, DateTimeKind.Utc).AddHours(i),
                    Estado = i == 2 ? EstadoPedido.PendientePago : i == 1 ? EstadoPedido.Recogido : EstadoPedido.Pagado };
                pedido.Detalles.Add(new DetallePedido { Id = Guid.NewGuid(), ProductoId = alimento.Id, Cantidad = i + 1, PrecioUnitario = 10.25m });
                pedido.Detalles.Add(new DetallePedido { Id = Guid.NewGuid(), ProductoId = playera.Id, Cantidad = 1, PrecioUnitario = 100m });
                db.Pedidos.Add(pedido);
            }
            db.SaveChanges();
        }
        var ruta = $"/api/reportes/alimentos?usuarioId={PerfilesFactory.ClienteId}&desdeUtc=2026-11-24T12:00:00Z&hastaUtc=2026-11-24T14:00:00Z";
        var reporte = await admin.GetFromJsonAsync<JsonElement>(ruta + "&tamanoPagina=1");
        Assert.Equal(2, reporte.GetProperty("datos").GetProperty("total").GetInt32());
        Assert.Equal(30.75m, reporte.GetProperty("importeConfirmado").GetDecimal());
        var fila = Assert.Single(reporte.GetProperty("datos").GetProperty("elementos").EnumerateArray());
        Assert.Equal(10.25m, fila.GetProperty("precioUnitario").GetDecimal());
        Assert.Equal(20.50m, fila.GetProperty("total").GetDecimal());
        var csv = await admin.GetAsync(ruta + "&formato=csv&tamanoPagina=1&pagina=999");
        Assert.Equal(HttpStatusCode.OK, csv.StatusCode);
        var texto = await csv.Content.ReadAsStringAsync();
        Assert.Equal(3, texto.TrimEnd().Split("\r\n").Length);
        Assert.Contains("'=Bebida, especial", texto);
        Assert.DoesNotContain("Playera", texto);
        var ninguno = await admin.GetFromJsonAsync<JsonElement>(ruta + "&productoId=" + Guid.NewGuid());
        Assert.Equal(0, ninguno.GetProperty("datos").GetProperty("total").GetInt32());
        Assert.Equal(0m, ninguno.GetProperty("importeConfirmado").GetDecimal());
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("cliente", HttpStatusCode.Forbidden)]
    [InlineData("recepcion", HttpStatusCode.Forbidden)]
    public async Task RequiereAdministrador(string? identidad, HttpStatusCode esperado)
    {
        using var app = new PerfilesFactory();
        using var cliente = app.CrearCliente(identidad);
        Assert.Equal(esperado, (await cliente.GetAsync("/api/reportes/alimentos")).StatusCode);
    }

    [Theory]
    [InlineData("productoId=invalido")]
    [InlineData("pagina=0")]
    [InlineData("desdeUtc=2026-11-25T00:00:00Z&hastaUtc=2026-11-24T00:00:00Z")]
    public async Task ValidaFiltros(string filtro)
    {
        using var app = new PerfilesFactory();
        using var admin = app.CrearCliente("admin");
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/api/reportes/alimentos?" + filtro)).StatusCode);
    }
}
