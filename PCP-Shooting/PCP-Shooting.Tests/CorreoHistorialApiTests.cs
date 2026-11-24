using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PCP_Shooting.Persistence.Interfaces;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class CorreoHistorialApiTests
{
    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("cliente", HttpStatusCode.Forbidden)]
    [InlineData("recepcion", HttpStatusCode.Forbidden)]
    public async Task NoConsultaCorreosSinPermisoAdministrativo(string? identidad, HttpStatusCode esperado)
    {
        using var baseFactory = new PerfilesFactory();
        var cuentas = new CuentasPrueba("privado@example.invalid");
        using var app = baseFactory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.Replace(ServiceDescriptor.Singleton<ISupabaseUsuariosPersistence>(cuentas))));
        using var client = app.CreateClient();
        if (identidad is not null) client.DefaultRequestHeaders.Add("X-Identidad-Prueba", identidad);
        Assert.Equal(esperado, (await client.GetAsync($"/api/administracion/usuarios/{PerfilesFactory.ClienteId}/historial")).StatusCode);
        Assert.Empty(cuentas.Consultas);
    }

    [Theory]
    [InlineData("cliente@example.invalid")]
    [InlineData(null)]
    public async Task FichaAdministrativaIncluyeCorreoDeAuthONull(string? correo)
    {
        using var baseFactory = new PerfilesFactory();
        var cuentas = new CuentasPrueba(correo);
        using var app = baseFactory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.Replace(ServiceDescriptor.Singleton<ISupabaseUsuariosPersistence>(cuentas))));
        using var admin = app.CreateClient();
        admin.DefaultRequestHeaders.Add("X-Identidad-Prueba", "admin");
        var response = await admin.GetAsync($"/api/administracion/usuarios/{PerfilesFactory.ClienteId}/historial");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(correo, body.GetProperty("correo").GetString());
        Assert.Equal(PerfilesFactory.ClienteId, Assert.Single(cuentas.Consultas));
        cuentas.Consultas.Clear();
        Assert.Equal(HttpStatusCode.NotFound,
            (await admin.GetAsync($"/api/administracion/usuarios/{Guid.NewGuid()}/historial")).StatusCode);
        Assert.Empty(cuentas.Consultas);
    }

    private sealed class CuentasPrueba(string? correo) : ISupabaseUsuariosPersistence
    {
        public List<Guid> Consultas { get; } = new();
        public Task<Guid?> BuscarCuentaConfirmadaAsync(string correo, CancellationToken ct) => Task.FromResult<Guid?>(null);
        public Task<string?> ConsultarCorreoAsync(Guid id, CancellationToken ct)
        {
            Consultas.Add(id);
            return Task.FromResult(correo);
        }
    }
}
