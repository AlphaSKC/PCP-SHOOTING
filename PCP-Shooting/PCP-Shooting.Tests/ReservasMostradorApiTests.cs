using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class ReservasMostradorApiTests
{
    [Fact]
    public async Task RecepcionPuedeCrearReservaParaClienteYConsultarDetalleAdministrativo()
    {
        using var app = new DisponibilidadTestApp();
        using var recepcion = app.ClientePara("recepcion");
        var id = Guid.NewGuid();
        var linea = app.Leer(db => db.LineasTiro.Single(l => l.Numero == 1).Id);
        var datos = new
        {
            idOperacion = id, clienteId = PerfilesFactory.ClienteId, paqueteId = app.PaqueteId,
            fecha = "2026-11-20", hora = "11:00:00",
            participantes = new[] { new { lineaId = linea, nombre = "Participante", nacimiento = "2000-01-01" } }
        };
        var respuesta = await recepcion.PostAsJsonAsync("/api/reservas/mostrador", datos);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var reserva = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(id, reserva.GetProperty("id").GetGuid());
        Assert.Equal("PendientePago", reserva.GetProperty("estado").GetString());
        var guardada = app.Leer(db => db.ReservasWeb.Single(r => r.Id == id));
        Assert.Equal(PerfilesFactory.ClienteId, guardada.UsuarioId);
        Assert.Equal(PerfilesFactory.RecepcionId, guardada.CreadoPorId);
        Assert.Null(guardada.ApartadoId);
        Assert.True(guardada.EsMostrador);

        var reintento = await recepcion.PostAsJsonAsync("/api/reservas/mostrador", datos);
        Assert.Equal(HttpStatusCode.Created, reintento.StatusCode);
        var reservaReintentada = await reintento.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(id, reservaReintentada.GetProperty("id").GetGuid());

        var mismosDatosOtraIdentidad = new
        {
            datos.idOperacion, datos.clienteId, datos.paqueteId, datos.fecha, datos.hora,
            participantes = new[] { new { lineaId = linea, nombre = "Otro nombre", nacimiento = "2000-01-01" } }
        };
        Assert.Equal(HttpStatusCode.Conflict,
            (await recepcion.PostAsJsonAsync("/api/reservas/mostrador", mismosDatosOtraIdentidad)).StatusCode);

        var lista = await recepcion.GetFromJsonAsync<JsonElement>("/api/reservas/administracion?fecha=2026-11-20");
        Assert.Equal(1, lista.GetProperty("total").GetInt32());
        var detalle = await recepcion.GetFromJsonAsync<JsonElement>("/api/reservas/administracion/" + id);
        Assert.Equal("Cliente de prueba", detalle.GetProperty("nombreCliente").GetString());
        Assert.Equal(1, detalle.GetProperty("participantes")[0].GetProperty("numeroLinea").GetInt32());
        using var cliente = app.ClientePara("cliente");
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/reservas/administracion/" + id)).StatusCode);
    }

    [Fact]
    public async Task ReservaMostradorNoPermiteApartarUnaLineaYaSeleccionada()
    {
        using var app = new DisponibilidadTestApp();
        using var recepcion = app.ClientePara("recepcion");
        using var cliente = app.ClientePara("cliente");
        var linea = app.Leer(db => db.LineasTiro.Single(l => l.Numero == 1).Id);
        var apartado = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.Created, (await cliente.PostAsJsonAsync("/api/apartados", new
        {
            idOperacion = apartado, paqueteId = app.PaqueteId, fecha = "2026-11-20", hora = "11:00:00", lineas = new[] { linea }
        })).StatusCode);
        var datos = new
        {
            idOperacion = Guid.NewGuid(), clienteId = PerfilesFactory.ClienteId, paqueteId = app.PaqueteId,
            fecha = "2026-11-20", hora = "11:00:00",
            participantes = new[] { new { lineaId = linea, nombre = "Participante", nacimiento = "2000-01-01" } }
        };
        Assert.Equal(HttpStatusCode.Conflict, (await recepcion.PostAsJsonAsync("/api/reservas/mostrador", datos)).StatusCode);
    }

    [Fact]
    public async Task ClienteNoPuedeCrearReservaDeMostradorYAdministradorPuedeListar()
    {
        using var app = new DisponibilidadTestApp();
        using var cliente = app.ClientePara("cliente");
        var linea = app.Leer(db => db.LineasTiro.Single(l => l.Numero == 1).Id);
        var response = await cliente.PostAsJsonAsync("/api/reservas/mostrador", new
        {
            idOperacion = Guid.NewGuid(), clienteId = PerfilesFactory.ClienteId, paqueteId = app.PaqueteId,
            fecha = "2026-11-20", hora = "11:00:00",
            participantes = new[] { new { lineaId = linea, nombre = "Participante", nacimiento = "2000-01-01" } }
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var admin = app.ClientePara("admin");
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/reservas/administracion")).StatusCode);
    }
}
