using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class FrontendContractApiTests
{
    [Fact]
    public async Task F05VentanaPublicaUsaHoraDelServidorYConfiguracionActual()
    {
        using var app = new DisponibilidadTestApp();
        var ruta = "/api/disponibilidad/ventana";
        var ventana = await app.Cliente.GetFromJsonAsync<JsonElement>(ruta);
        Assert.Equal(app.Reloj.Ahora.UtcDateTime, ventana.GetProperty("consultadoEnUtc").GetDateTime());
        Assert.Equal(app.Reloj.Ahora.AddHours(1).UtcDateTime, ventana.GetProperty("inicioMinimoUtc").GetDateTime());
        Assert.Equal(app.Reloj.Ahora.AddDays(2).UtcDateTime, ventana.GetProperty("inicioMaximoUtc").GetDateTime());
        Assert.Equal("America/Cancun", ventana.GetProperty("zonaHoraria").GetString());
        Assert.Equal("2026-11-20", ventana.GetProperty("primeraFecha").GetString());
        Assert.Equal("2026-11-22", ventana.GetProperty("ultimaFecha").GetString());
        app.Modificar(db => { var c = db.ConfiguracionOperativa.Single(); c.AnticipacionMinimaMinutos = 120; c.AnticipacionMaximaDias = 3; });
        app.Reloj.Ahora = app.Reloj.Ahora.AddDays(1);
        ventana = await app.Cliente.GetFromJsonAsync<JsonElement>(ruta);
        Assert.Equal(app.Reloj.Ahora.AddHours(2).UtcDateTime, ventana.GetProperty("inicioMinimoUtc").GetDateTime());
        Assert.Equal(app.Reloj.Ahora.AddDays(3).UtcDateTime, ventana.GetProperty("inicioMaximoUtc").GetDateTime());
        Assert.Equal("2026-11-24", ventana.GetProperty("ultimaFecha").GetString());
        Assert.False(ventana.TryGetProperty("lineas", out _));
    }

    [Fact]
    public async Task F04ApartadoYReservaIncluyenLineasYResumenIncluyeParticipantes()
    {
        using var app = new DisponibilidadTestApp();
        using var cliente = app.ClientePara("cliente");
        var lineas = app.Leer(db => db.LineasTiro.OrderBy(l => l.Numero).Take(2).Select(l => new { l.Id, l.Numero, l.Nombre }).ToArray());
        var apartadoId = Guid.NewGuid();
        var datos = new { idOperacion = apartadoId, paqueteId = app.PaqueteId, fecha = "2026-11-20", hora = "13:00:00", lineas = lineas.Select(l => l.Id).ToArray() };
        var creado = await cliente.PostAsJsonAsync("/api/apartados", datos);
        Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
        void Verificar(JsonElement apartado)
        {
            Assert.Equal(2, apartado.GetProperty("lineas").GetArrayLength());
            var detalles = apartado.GetProperty("lineasDetalle");
            foreach (var l in lineas)
            {
                var detalle = detalles.EnumerateArray().Single(d => d.GetProperty("lineaId").GetGuid() == l.Id);
                Assert.Equal(l.Numero, detalle.GetProperty("numeroLinea").GetInt32());
                Assert.Equal(l.Nombre, detalle.GetProperty("nombreLinea").GetString());
            }
        }
        Verificar(await creado.Content.ReadFromJsonAsync<JsonElement>());
        Verificar(await cliente.GetFromJsonAsync<JsonElement>("/api/apartados/" + apartadoId));
        Verificar((await cliente.GetFromJsonAsync<JsonElement>("/api/apartados")).GetProperty("elementos")[0]);
        Verificar(await (await cliente.PostAsJsonAsync("/api/apartados", datos)).Content.ReadFromJsonAsync<JsonElement>());
        var reservaId = Guid.NewGuid();
        var solicitud = new { idOperacion = reservaId, apartadoId, participantes = lineas.Select(l => new { lineaId = l.Id, nombre = "Adulto", nacimiento = "2000-01-01" }).ToArray() };
        var reserva = await cliente.PostAsJsonAsync("/api/reservas", solicitud);
        Assert.Equal(HttpStatusCode.Created, reserva.StatusCode);
        void VerificarReserva(JsonElement r)
        {
            foreach (var l in lineas)
            {
                var p = r.GetProperty("participantes").EnumerateArray().Single(p => p.GetProperty("lineaId").GetGuid() == l.Id);
                Assert.Equal(l.Numero, p.GetProperty("numeroLinea").GetInt32());
                Assert.Equal(l.Nombre, p.GetProperty("nombreLinea").GetString());
            }
        }
        VerificarReserva(await reserva.Content.ReadFromJsonAsync<JsonElement>());
        VerificarReserva(await cliente.GetFromJsonAsync<JsonElement>("/api/reservas/" + reservaId));
        VerificarReserva(await (await cliente.PostAsJsonAsync("/api/reservas", solicitud)).Content.ReadFromJsonAsync<JsonElement>());
        var historial = await cliente.GetFromJsonAsync<JsonElement>("/api/reservas");
        Assert.Equal(2, historial.GetProperty("elementos")[0].GetProperty("cantidadParticipantes").GetInt32());
    }

    [Fact]
    public async Task F07RecepcionVeClienteEnPedidoSinAccesoAlDirectorioDePerfiles()
    {
        using var app = new DisponibilidadTestApp();
        var pedidoId = Guid.NewGuid();
        app.Modificar(db =>
        {
            var p = new Producto { Id = Guid.NewGuid(), Sku = "F07", Nombre = "Playera", EsVendible = true, Precio = 100 };
            var pedido = new Pedido { Id = pedidoId, UsuarioId = PerfilesFactory.ClienteId, CreadoEnUtc = app.Reloj.Ahora.UtcDateTime };
            pedido.Detalles.Add(new DetallePedido { Id = Guid.NewGuid(), Producto = p, Cantidad = 1, PrecioUnitario = 100 });
            db.Pedidos.Add(pedido);
        });
        using var recepcion = app.ClientePara("recepcion");
        void Verificar(JsonElement p)
        {
            Assert.Equal("Cliente de prueba", p.GetProperty("nombreCliente").GetString());
            Assert.Equal("1234567890", p.GetProperty("telefonoCliente").GetString());
        }
        Verificar((await recepcion.GetFromJsonAsync<JsonElement>("/api/administracion/pedidos")).GetProperty("elementos")[0]);
        Verificar((await recepcion.GetFromJsonAsync<JsonElement>("/api/administracion/pedidos/" + pedidoId)).GetProperty("pedido"));
        Assert.Equal(HttpStatusCode.Forbidden, (await recepcion.GetAsync("/api/perfiles")).StatusCode);
        using var cliente = app.ClientePara("cliente");
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/administracion/pedidos")).StatusCode);
    }
}
