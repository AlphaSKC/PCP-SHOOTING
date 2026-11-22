using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Domain.Services;
using PCP_Shooting.Persistence.Migrations;
using PCP_Shooting.Persistence.Models;
using PCP_Shooting.Persistence.Services;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class ReservasWebPostgresTests(PostgresEscriturasFixture fixture) : IClassFixture<PostgresEscriturasFixture>
{
    [PostgresFact]
    public async Task MigracionProtegeTablasYSnapshotsPersistenConPrecisionSinConsumirInventario()
    {
        await using var db = fixture.CrearContext();
        var esquema = await db.Database.SqlQueryRaw<string>("SELECT current_schema() AS \"Value\"").SingleAsync();
        Assert.StartsWith("pcp_test_", esquema);
        await db.Database.ExecuteSqlRawAsync("DROP TABLE \"ConsumosReserva\", \"ParticipantesReserva\", \"ReservasWeb\"");
        foreach (var comando in db.GetService<IMigrationsSqlGenerator>().Generate(new ReservasWebIniciales().UpOperations, db.Model))
            await db.Database.ExecuteSqlRawAsync(comando.CommandText);
        Assert.Equal(3, await db.Database.SqlQueryRaw<int>("""
            SELECT count(*)::int AS "Value" FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname=current_schema() AND c.relname IN ('ReservasWeb','ParticipantesReserva','ConsumosReserva')
            AND c.relrowsecurity AND NOT has_table_privilege('anon',c.oid,'SELECT')
            AND NOT has_table_privilege('authenticated',c.oid,'SELECT')
            """).SingleAsync());
        var reloj = new RelojDisponibilidad(DateTimeOffset.Parse("2026-11-20T15:00:00Z"));
        var usuario = Guid.NewGuid();
        var producto = new Producto { Id = Guid.NewGuid(), Sku = "CONSUMO-PRUEBA", Nombre = "Consumible original", Categoria = CategoriaProducto.Consumible };
        var paquete = new Paquete { Id = Guid.NewGuid(), Codigo = "RESERVA-PRUEBA", Nombre = "Paquete original", IncluyeMarkdown = "- Características",
            PrecioPorPersona = 100.25m, PrecioSocioCartaq = 80m, Activo = true, Version = Guid.NewGuid() };
        paquete.Consumibles.Add(new ConsumiblePaquete { Id = Guid.NewGuid(), Producto = producto, CantidadPorPersona = 5 });
        db.PerfilesUsuario.Add(new PerfilUsuario { Id = usuario, Nombre = "Cliente" });
        db.Paquetes.Add(paquete);
        await db.SaveChangesAsync();
        var tx = new TransaccionesPersistence(db);
        var operacion = new OperacionService(db, reloj);
        var paquetes = new PaquetesService(db, reloj, tx);
        var apartados = new ApartadosService(db, new DisponibilidadService(db, operacion, paquetes, reloj), operacion, tx, reloj);
        var lineas = await db.LineasTiro.OrderBy(l => l.Numero).Take(2).Select(l => l.Id).ToListAsync();
        var apartado = await apartados.CrearAsync(usuario, new CrearApartado { IdOperacion = Guid.NewGuid(), PaqueteId = paquete.Id,
            Fecha = new(2026,11,20), Hora = new(13,0), Lineas = lineas }, default);
        var disponibilidadServicio = new DisponibilidadService(db, operacion, paquetes, reloj);
        var servicio = new ReservasWebService(db, tx, reloj, disponibilidadServicio, operacion);
        var solicitud = new CrearReservaWeb { IdOperacion = Guid.NewGuid(), ApartadoId = apartado.Id, Participantes = lineas.Select(l =>
            new ParticipanteReservaDatos { LineaId = l, Nombre = "Adulto", Nacimiento = new(2000,1,1) }).ToList() };
        var reserva = await servicio.CrearAsync(usuario, solicitud, default);
        Assert.Equal(200.50m, reserva.Total);
        Assert.Equal(10, Assert.Single(reserva.Consumibles).CantidadTotal);
        paquete.Nombre = "Paquete cambiado"; producto.Nombre = "Consumible cambiado";
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var consulta = (await servicio.ConsultarAsync(usuario, reserva.Id, default))!;
        Assert.Equal("Paquete original", consulta.NombrePaquete);
        Assert.Equal("Consumible original", Assert.Single(consulta.Consumibles).Nombre);
        Assert.Equal(reserva.VencePagoEnUtc, (await servicio.CrearAsync(usuario, solicitud, default)).VencePagoEnUtc);
        Assert.Empty(await db.Pagos.ToListAsync());
        Assert.Empty(await db.MovimientosInventario.ToListAsync());
        var copia = await db.ReservasWeb.SingleAsync();
        copia.Total = -1;
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
