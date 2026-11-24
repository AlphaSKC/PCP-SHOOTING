using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Domain.Services;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Services;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class AdministradorInicialPostgresTests
{
    [PostgresFact]
    public async Task RequiereCuentaConfirmadaYEsIdempotenteSinCrearOtroAdministrador()
    {
        var fixture = new PostgresEscriturasFixture();
        await fixture.InitializeAsync();
        try
        {
            await using var db = fixture.CrearContext();
            var transacciones = new TransaccionesPersistence(db);
            var ausente = new AdministradorInicialService(db, new CuentaConfirmada(null), transacciones);
            Assert.Equal(404, (await Assert.ThrowsAsync<OperacionException>(() =>
                ausente.InicializarAsync("admin@example.invalid", default))).Status);
            Assert.Empty(await db.PerfilesUsuario.ToListAsync());
            var usuarioId = Guid.NewGuid();
            var inicializador = new AdministradorInicialService(db, new CuentaConfirmada(usuarioId), transacciones);
            Assert.Equal(usuarioId, await inicializador.InicializarAsync("admin@example.invalid", default));
            Assert.Equal(usuarioId, await inicializador.InicializarAsync("admin@example.invalid", default));
            var perfil = Assert.Single(await db.PerfilesUsuario.AsNoTracking().ToListAsync());
            Assert.Equal(RolUsuario.Administrador, perfil.Rol);
            Assert.True(perfil.Activo);
            var segundo = new AdministradorInicialService(db, new CuentaConfirmada(Guid.NewGuid()), transacciones);
            Assert.Equal(409, (await Assert.ThrowsAsync<OperacionException>(() =>
                segundo.InicializarAsync("otro@example.invalid", default))).Status);
            Assert.Single(await db.PerfilesUsuario.AsNoTracking().ToListAsync());
        }
        finally { await fixture.DisposeAsync(); }
    }

    [PostgresFact]
    public async Task DosInicializacionesConcurrentesSoloCreanUnAdministrador()
    {
        var fixture = new PostgresEscriturasFixture();
        await fixture.InitializeAsync();
        try
        {
            var inicio = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            async Task<bool> Inicializar(Guid id)
            {
                await inicio.Task;
                await using var db = fixture.CrearContext();
                var service = new AdministradorInicialService(db, new CuentaConfirmada(id), new TransaccionesPersistence(db));
                try
                {
                    await service.InicializarAsync("admin@example.invalid", default);
                    return true;
                }
                catch (OperacionException exception) when (exception.Status == 409) { return false; }
            }
            var primero = Inicializar(Guid.NewGuid());
            var segundo = Inicializar(Guid.NewGuid());
            inicio.SetResult();
            Assert.Single(await Task.WhenAll(primero, segundo), resultado => resultado);
            await using var verificar = fixture.CrearContext();
            Assert.Single(await verificar.PerfilesUsuario.Where(p => p.Rol == RolUsuario.Administrador).ToListAsync());
        }
        finally { await fixture.DisposeAsync(); }
    }

    private sealed class CuentaConfirmada(Guid? id) : ISupabaseUsuariosPersistence
    {
        public Task<Guid?> BuscarCuentaConfirmadaAsync(string correo, CancellationToken ct) => Task.FromResult(id);
        public Task<string?> ConsultarCorreoAsync(Guid id, CancellationToken ct) => Task.FromResult<string?>(null);
    }
}
