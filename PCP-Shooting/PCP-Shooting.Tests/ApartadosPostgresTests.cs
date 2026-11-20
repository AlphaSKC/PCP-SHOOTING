using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Domain.Services;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Migrations;
using PCP_Shooting.Persistence.Models;
using PCP_Shooting.Persistence.Services;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class ApartadosPostgresTests(PostgresEscriturasFixture fixture) : IClassFixture<PostgresEscriturasFixture>
{
    private readonly RelojDisponibilidad reloj = new(DateTimeOffset.Parse("2026-11-20T15:00:00Z"));
    private ApartadosService Servicio(PcpShootingDbContext db)
    {
        var operacion = new OperacionService(db, reloj);
        var transacciones = new TransaccionesPersistence(db);
        var paquetes = new PaquetesService(db, reloj, transacciones);
        return new ApartadosService(db, new DisponibilidadService(db, operacion, paquetes, reloj), operacion, transacciones, reloj);
    }

    private async Task<(Guid Usuario, Guid Otro, Guid Paquete, Guid[] Lineas)> Preparar()
    {
        await using var db = fixture.CrearContext();
        // Cada prueba usa otro horario, de modo que puede compartir el esquema sin compartir capacidad.
        var usuario = Guid.NewGuid();
        var otro = Guid.NewGuid();
        var paquete = Guid.NewGuid();
        db.PerfilesUsuario.AddRange(new PerfilUsuario { Id = usuario, Nombre = "Cliente" }, new PerfilUsuario { Id = otro, Nombre = "Otro cliente" });
        db.Paquetes.Add(new Paquete { Id = paquete, Codigo = "HOLD-" + paquete.ToString("N").ToUpperInvariant(), Nombre = "Actividad",
            IncluyeMarkdown = "Actividad", Activo = true, PrecioPorPersona = 100m, Version = Guid.NewGuid(),
            CreadoEnUtc = reloj.Ahora.UtcDateTime, ActualizadoEnUtc = reloj.Ahora.UtcDateTime });
        await db.SaveChangesAsync();
        return (usuario, otro, paquete, await db.LineasTiro.OrderBy(l => l.Numero).Select(l => l.Id).ToArrayAsync());
    }

    private static CrearApartado Datos(Guid id, Guid paquete, TimeOnly hora, params Guid[] lineas) =>
        new() { IdOperacion = id, PaqueteId = paquete, Fecha = new(2026, 11, 20), Hora = hora, Lineas = lineas.ToList() };

    [PostgresFact]
    public Task IntentosSimultaneosNoDuplicanCapacidad() => ComprobarConcurrencia(false);

    [PostgresFact]
    public Task GrupoDeLineasSeApartaCompletoONinguno() => ComprobarConcurrencia(true);

    [PostgresFact]
    public Task LineasDiferentesPermitenDosApartadosSimultaneos() => ComprobarConcurrencia(false, true);

    private async Task ComprobarConcurrencia(bool grupo, bool distintas = false)
    {
        var datos = await Preparar();
        var hora = distintas ? new TimeOnly(14, 45) : grupo ? new TimeOnly(11, 30) : new TimeOnly(11, 0);
        var reunion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var leidos = 0;
        async Task Esperar()
        {
            if (Interlocked.Increment(ref leidos) == 2) reunion.TrySetResult();
            await reunion.Task.WaitAsync(TimeSpan.FromSeconds(20));
        }
        await using var baseDatos = fixture.CrearContext();
        PcpShootingDbContext Contexto() => new(new DbContextOptionsBuilder<PcpShootingDbContext>().UseNpgsql(baseDatos.Database.GetConnectionString())
            .AddInterceptors(new BarreraLectura(Esperar)).Options);
        await using var uno = Contexto();
        await using var dos = Contexto();
        var lineasUno = grupo ? new[] { datos.Lineas[0], datos.Lineas[1] } : new[] { datos.Lineas[0] };
        var lineasDos = distintas ? new[] { datos.Lineas[1] } : grupo ? new[] { datos.Lineas[1], datos.Lineas[2] } : new[] { datos.Lineas[0] };
        async Task<int> Crear(PcpShootingDbContext db, Guid usuario, Guid[] lineas)
        {
            try { await Servicio(db).CrearAsync(usuario, Datos(Guid.NewGuid(), datos.Paquete, hora, lineas), default); return 201; }
            catch (OperacionException ex) { return ex.Status; }
        }
        var resultados = await Task.WhenAll(Crear(uno, datos.Usuario, lineasUno), Crear(dos, datos.Otro, lineasDos));
        Assert.Equal(distintas ? new[] { 201, 201 } : new[] { 201, 409 }, resultados.OrderBy(r => r));
        await using var comprobacion = fixture.CrearContext();
        var apartados = await comprobacion.Apartados.Include(a => a.Lineas).Where(a => a.PaqueteId == datos.Paquete).ToListAsync();
        if (distintas)
        {
            Assert.Equal(2, apartados.Count);
            Assert.All(apartados, a => Assert.Single(a.Lineas));
        }
        else Assert.Equal(grupo ? 2 : 1, Assert.Single(apartados).Lineas.Count);
        Assert.False(await comprobacion.MovimientosInventario.AnyAsync());
    }

    [PostgresFact]
    public async Task MismoIdSimultaneoEsIdempotente()
    {
        var datos = await Preparar();
        var id = Guid.NewGuid();
        await using var uno = fixture.CrearContext();
        await using var dos = fixture.CrearContext();
        var solicitud = Datos(id, datos.Paquete, new TimeOnly(12, 0), datos.Lineas[4]);
        var resultados = await Task.WhenAll(Servicio(uno).CrearAsync(datos.Usuario, solicitud, default), Servicio(dos).CrearAsync(datos.Usuario, solicitud, default));
        Assert.All(resultados, a => Assert.Equal(id, a.Id));
        Assert.Equal(resultados[0].VenceEnUtc, resultados[1].VenceEnUtc);
        await using var db = fixture.CrearContext();
        Assert.Equal(1, await db.Apartados.CountAsync(a => a.Id == id));
        Assert.Equal(1, await db.LineasApartado.CountAsync(l => l.ApartadoId == id));
        Assert.Equal(1, await db.EventosAuditoria.CountAsync(e => e.EntidadId == id));
    }

    [PostgresFact]
    public async Task VencimientoExactoPermiteNuevoApartadoSinRenovarElAnterior()
    {
        var datos = await Preparar();
        await using var db = fixture.CrearContext();
        var servicio = Servicio(db);
        var solicitud = Datos(Guid.NewGuid(), datos.Paquete, new TimeOnly(13, 0), datos.Lineas[7]);
        var primero = await servicio.CrearAsync(datos.Usuario, solicitud, default);
        reloj.Ahora = new DateTimeOffset(primero.VenceEnUtc);
        var reintento = await servicio.CrearAsync(datos.Usuario, solicitud, default);
        Assert.Equal(EstadoApartado.Vencido, reintento.Estado);
        Assert.Equal(primero.VenceEnUtc, reintento.VenceEnUtc);
        var segundo = await servicio.CrearAsync(datos.Otro, Datos(Guid.NewGuid(), datos.Paquete, new TimeOnly(13, 0), datos.Lineas[7]), default);
        Assert.Equal(EstadoApartado.Vigente, segundo.Estado);
        Assert.Equal(2, await db.Apartados.CountAsync(a => a.PaqueteId == datos.Paquete));
        Assert.Null(await servicio.ConsultarAsync(datos.Otro, primero.Id, default));
    }

    [PostgresFact]
    public async Task CambiarLaGrillaNoModificaLaOcupacionYaApartada()
    {
        var datos = await Preparar();
        await using var db = fixture.CrearContext();
        var servicio = Servicio(db);
        var primero = await servicio.CrearAsync(datos.Usuario, Datos(Guid.NewGuid(), datos.Paquete, new TimeOnly(14, 0), datos.Lineas[10]), default);
        var configuracion = await db.ConfiguracionOperativa.SingleAsync();
        configuracion.DuracionActividadMinutos = 5;
        configuracion.MargenMinutos = 0;
        await db.SaveChangesAsync();
        var error = await Assert.ThrowsAsync<OperacionException>(() => servicio.CrearAsync(datos.Otro,
            Datos(Guid.NewGuid(), datos.Paquete, new TimeOnly(14, 5), datos.Lineas[10]), default));
        Assert.Equal(409, error.Status);
        var siguiente = await servicio.CrearAsync(datos.Otro, Datos(Guid.NewGuid(), datos.Paquete, new TimeOnly(14, 15), datos.Lineas[10]), default);
        Assert.Equal(primero.FinOcupacionUtc, siguiente.InicioUtc);
        // Restaurar configuración compartida por el fixture.
        await using var restaurar = fixture.CrearContext();
        var original = await restaurar.ConfiguracionOperativa.SingleAsync();
        original.DuracionActividadMinutos = 10;
        original.MargenMinutos = 5;
        await restaurar.SaveChangesAsync();
    }

    [PostgresFact]
    public async Task MigracionProtegeDatosYPermiteConservarApartadosVencidos()
    {
        await using var db = fixture.CrearContext();
        var esquema = await db.Database.SqlQueryRaw<string>("SELECT current_schema() AS \"Value\"").SingleAsync();
        Assert.StartsWith("pcp_test_", esquema);
        await db.Database.ExecuteSqlRawAsync("DROP TABLE \"LineasApartado\", \"Apartados\"");
        foreach (var comando in db.GetService<IMigrationsSqlGenerator>().Generate(new ApartadosTemporales().UpOperations, db.Model))
            await db.Database.ExecuteSqlRawAsync(comando.CommandText);
        var protegidas = await db.Database.SqlQueryRaw<int>("""
            SELECT count(*)::int AS "Value" FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE n.nspname = current_schema() AND c.relname IN ('Apartados', 'LineasApartado') AND c.relrowsecurity
                AND NOT has_table_privilege('anon', c.oid, 'SELECT') AND NOT has_table_privilege('authenticated', c.oid, 'SELECT')
            """).SingleAsync();
        Assert.Equal(2, protegidas);
    }

    private sealed class BarreraLectura(Func<Task> esperar) : DbCommandInterceptor
    {
        private bool ejecutada;
        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
            DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (!ejecutada && command.CommandText.Contains("FROM \"LineasApartado\""))
            {
                ejecutada = true;
                await esperar();
            }
            return result;
        }
    }
}
