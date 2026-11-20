using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Domain.Services;
using PCP_Shooting.Persistence.Migrations;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class OperacionPostgresTests(PostgresEscriturasFixture fixture) : IClassFixture<PostgresEscriturasFixture>
{
    private const string Ruta = "/api/administracion/operacion";

    [PostgresFact]
    public async Task RecepcionBloqueaYAuditoriaConservaActorSinPermitirCambiarConfiguracion()
    {
        using var factory = new OperacionesFactory(fixture);
        using var admin = factory.Cliente(OperacionesFactory.AdminId);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/usuarios/yo")).StatusCode);
        await using (var db = fixture.CrearContext())
        {
            var perfil = await db.PerfilesUsuario.SingleAsync(p => p.Id == OperacionesFactory.OtroId);
            perfil.Rol = PCP_Shooting.Domain.Interfaces.RolUsuario.Recepcion;
            await db.SaveChangesAsync();
        }
        using var recepcion = factory.Cliente(OperacionesFactory.OtroId);
        var lineas = (await recepcion.GetFromJsonAsync<LineaTiroConsulta[]>(Ruta + "/lineas"))!;
        Assert.Equal(15, lineas.Length);
        var datos = new GuardarBloqueoOperacion
        {
            LineaId = lineas[0].Id, InicioUtc = DateTimeOffset.Parse("2030-02-01T11:00:00-05:00"),
            FinUtc = DateTimeOffset.Parse("2030-02-01T11:15:00-05:00"), Motivo = "Bloqueo HTTP PostgreSQL"
        };
        var respuesta = await recepcion.PostAsJsonAsync(Ruta + "/bloqueos", datos);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var creado = (await respuesta.Content.ReadFromJsonAsync<BloqueoOperacionConsulta>())!;
        Assert.Equal(16, creado.InicioUtc.Hour);
        var pagina = await recepcion.GetFromJsonAsync<PaginaConsulta<BloqueoOperacionConsulta>>(
            Ruta + $"/bloqueos?lineaId={lineas[0].Id}&desdeUtc=2030-02-01T16:00:00Z&hastaUtc=2030-02-01T16:15:00Z");
        Assert.Contains(pagina!.Elementos, b => b.Id == creado.Id);
        var cancelar = new GuardarBloqueoOperacion { LineaId = datos.LineaId, InicioUtc = datos.InicioUtc, FinUtc = datos.FinUtc,
            Motivo = datos.Motivo, Activo = false, Version = creado.Version };
        Assert.Equal(HttpStatusCode.OK, (await recepcion.PutAsJsonAsync(Ruta + "/bloqueos/" + creado.Id, cancelar)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await recepcion.PutAsJsonAsync(Ruta + "/bloqueos/" + creado.Id, cancelar)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await recepcion.PutAsJsonAsync(Ruta + "/configuracion", new { })).StatusCode);
        await using var comprobacion = fixture.CrearContext();
        var eventos = await comprobacion.EventosAuditoria.Where(e => e.EntidadId == creado.Id).ToListAsync();
        Assert.Equal(2, eventos.Count);
        Assert.All(eventos, e => Assert.Equal(OperacionesFactory.OtroId, e.ActorId));
    }

    [PostgresFact]
    public async Task ConfiguracionConVersionEvitaSobrescrituraEnSesionesConcurrentes()
    {
        await using var uno = fixture.CrearContext();
        await using var dos = fixture.CrearContext();
        var servicioUno = new OperacionService(uno, TimeProvider.System);
        var servicioDos = new OperacionService(dos, TimeProvider.System);
        var original = await servicioUno.ConsultarConfiguracionAsync(default);
        await uno.ConfiguracionOperativa.LoadAsync();
        await dos.ConfiguracionOperativa.LoadAsync();
        await uno.HorariosOperacion.LoadAsync();
        await dos.HorariosOperacion.LoadAsync();
        GuardarConfiguracionOperativa Datos(int minutos) => new()
        {
            Version = original.Version, DuracionActividadMinutos = original.DuracionActividadMinutos, MargenMinutos = original.MargenMinutos,
            ApartadoPagoMinutos = minutos, AnticipacionMinimaMinutos = original.AnticipacionMinimaMinutos,
            AnticipacionMaximaDias = original.AnticipacionMaximaDias, LlegadaAnticipadaMinutos = original.LlegadaAnticipadaMinutos,
            ToleranciaLlegadaMinutos = original.ToleranciaLlegadaMinutos, Horarios = original.Horarios.ToList()
        };
        await servicioUno.GuardarConfiguracionAsync(Datos(19), default);
        var conflicto = await Assert.ThrowsAsync<OperacionException>(() => servicioDos.GuardarConfiguracionAsync(Datos(20), default));
        Assert.Equal(409, conflicto.Status);
        await using var comprobacion = fixture.CrearContext();
        Assert.Equal(19, (await comprobacion.ConfiguracionOperativa.SingleAsync()).ApartadoPagoMinutos);
        Assert.Equal(7, await comprobacion.HorariosOperacion.CountAsync());
    }

    [PostgresFact]
    public async Task MigracionCreaDatosBaseYProtegeLasCuatroTablas()
    {
        await using var db = fixture.CrearContext();
        var esquema = await db.Database.SqlQueryRaw<string>("SELECT current_schema() AS \"Value\"").SingleAsync();
        Assert.StartsWith("pcp_test_", esquema);
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"LineasApartado\" DROP CONSTRAINT \"FK_LineasApartado_LineasTiro_LineaId\"");
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"ParticipantesReserva\" DROP CONSTRAINT \"FK_ParticipantesReserva_LineasTiro_LineaId\"");
        await db.Database.ExecuteSqlRawAsync("DROP TABLE \"BloqueosOperacion\", \"HorariosOperacion\", \"ConfiguracionOperativa\", \"LineasTiro\"");
        var migracion = new ConfiguracionOperativaYLineas();
        foreach (var comando in db.GetService<IMigrationsSqlGenerator>().Generate(migracion.UpOperations, db.Model))
            await db.Database.ExecuteSqlRawAsync(comando.CommandText);
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"LineasApartado\" ADD CONSTRAINT \"FK_LineasApartado_LineasTiro_LineaId\" FOREIGN KEY (\"LineaId\") REFERENCES \"LineasTiro\" (\"Id\") ON DELETE RESTRICT");
        Assert.Equal(15, await db.LineasTiro.CountAsync());
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"ParticipantesReserva\" ADD CONSTRAINT \"FK_ParticipantesReserva_LineasTiro_LineaId\" FOREIGN KEY (\"LineaId\") REFERENCES \"LineasTiro\" (\"Id\") ON DELETE RESTRICT");
        Assert.Equal(7, await db.HorariosOperacion.CountAsync());
        Assert.Equal(10, (await db.ConfiguracionOperativa.SingleAsync()).ApartadoPagoMinutos);
        var protegidas = await db.Database.SqlQueryRaw<int>("""
            SELECT count(*)::int AS "Value" FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE n.nspname = current_schema() AND c.relname IN ('ConfiguracionOperativa', 'HorariosOperacion', 'LineasTiro', 'BloqueosOperacion')
                AND c.relrowsecurity AND NOT has_table_privilege('anon', c.oid, 'SELECT')
                AND NOT has_table_privilege('authenticated', c.oid, 'SELECT')
            """).SingleAsync();
        Assert.Equal(4, protegidas);
        db.LineasTiro.Add(new LineaTiro { Id = Guid.NewGuid(), Numero = 16, Nombre = "No válida", Capacidad = 1, Version = Guid.NewGuid() });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
