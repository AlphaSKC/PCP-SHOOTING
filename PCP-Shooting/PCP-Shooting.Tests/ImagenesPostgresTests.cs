using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Models;
using Xunit;

namespace PCP_Shooting.Tests;

public sealed class ImagenesPostgresTests(PostgresEscriturasFixture fixture) : IClassFixture<PostgresEscriturasFixture>
{
    [PostgresFact]
    public async Task CargaAsignaImagenYReintentosNoReviertenUnReemplazo()
    {
        var storage = new StorageSimulado();
        using var original = new OperacionesFactory(fixture);
        using var factory = Configurar(original, storage);
        using var admin = Cliente(factory, OperacionesFactory.AdminId);
        var (id, sku) = await Producto(admin, CategoriaProducto.Playera);
        var operacion = Guid.NewGuid();
        var primera = await Subir(admin, id, operacion);
        Assert.True(primera.EsImagenActual);
        Assert.Equal(primera, await Subir(admin, id, operacion));
        Assert.Equal(1, storage.Intentos);
        var segunda = await Subir(admin, id, Guid.NewGuid());
        var repetida = await Subir(admin, id, operacion);
        Assert.False(repetida.EsImagenActual);
        var ficha = await admin.GetFromJsonAsync<FichaProducto>("/api/productos/" + sku);
        Assert.Equal(segunda.Url, ficha!.ImagenUrl);
        await using var db = fixture.CrearContext();
        Assert.Equal(2, await db.ImagenesProducto.CountAsync(i => i.ProductoId == id && i.Disponible));
        Assert.Equal(2, storage.Intentos);
    }

    [PostgresFact]
    public async Task FalloDeStorageDejaLaImagenAnteriorYPermiteReintentar()
    {
        var storage = new StorageSimulado { Fallar = true };
        using var original = new OperacionesFactory(fixture);
        using var factory = Configurar(original, storage);
        using var admin = Cliente(factory, OperacionesFactory.AdminId);
        var (id, sku) = await Producto(admin, CategoriaProducto.Alimento);
        var operacion = Guid.NewGuid();
        using (var formulario = Formulario(operacion))
        {
            var fallo = await admin.PostAsync($"/api/productos/{id}/imagen", formulario);
            Assert.Equal(HttpStatusCode.BadGateway, fallo.StatusCode);
        }
        var ficha = await admin.GetFromJsonAsync<FichaProducto>("/api/productos/" + sku);
        Assert.Null(ficha!.ImagenUrl);
        await using (var db = fixture.CrearContext())
            Assert.False((await db.ImagenesProducto.SingleAsync(i => i.Id == operacion)).Disponible);
        var resultado = await Subir(admin, id, operacion);
        Assert.True(resultado.EsImagenActual);
        await using var verificar = fixture.CrearContext();
        Assert.Single(await verificar.ImagenesProducto.Where(i => i.ProductoId == id).ToListAsync());
    }

    [PostgresFact]
    public async Task RecepcionSoloSubeAlimentosYClienteNoSube()
    {
        var storage = new StorageSimulado();
        using var original = new OperacionesFactory(fixture);
        using var factory = Configurar(original, storage);
        using var admin = Cliente(factory, OperacionesFactory.AdminId);
        var (alimento, _) = await Producto(admin, CategoriaProducto.Alimento);
        var (playera, _) = await Producto(admin, CategoriaProducto.Playera);
        await using (var db = fixture.CrearContext())
        {
            var perfil = await db.PerfilesUsuario.SingleAsync(p => p.Id == OperacionesFactory.OtroId);
            perfil.Rol = RolUsuario.Recepcion;
            await db.SaveChangesAsync();
        }
        using var recepcion = Cliente(factory, OperacionesFactory.OtroId);
        using var cliente = Cliente(factory, OperacionesFactory.ClienteId);
        using (var form = Formulario(Guid.NewGuid()))
            Assert.Equal(HttpStatusCode.Forbidden,
                (await recepcion.PostAsync($"/api/productos/{playera}/imagen", form)).StatusCode);
        using (var form = Formulario(Guid.NewGuid()))
            Assert.Equal(HttpStatusCode.Forbidden,
                (await cliente.PostAsync($"/api/productos/{alimento}/imagen", form)).StatusCode);
        var subida = await Subir(recepcion, alimento, Guid.NewGuid());
        Assert.True(subida.EsImagenActual);
        Assert.Equal(1, storage.Intentos);
    }

    [PostgresFact]
    public async Task IdOperacionNoSePuedeReutilizarConOtroContenido()
    {
        var storage = new StorageSimulado();
        using var original = new OperacionesFactory(fixture);
        using var factory = Configurar(original, storage);
        using var admin = Cliente(factory, OperacionesFactory.AdminId);
        var (id, _) = await Producto(admin, CategoriaProducto.Playera);
        var operacion = Guid.NewGuid();
        await Subir(admin, id, operacion);
        var contenido = ImagenesTests.Png;
        contenido[25] ^= 1;
        using var formulario = Formulario(operacion, contenido);
        Assert.Equal(HttpStatusCode.Conflict,
            (await admin.PostAsync($"/api/productos/{id}/imagen", formulario)).StatusCode);
        Assert.Equal(1, storage.Intentos);
    }

    private static WebApplicationFactory<Program> Configurar(OperacionesFactory original, IImagenStorage storage) =>
        original.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IImagenStorage>();
            services.AddSingleton(storage);
        }));

    private static HttpClient Cliente(WebApplicationFactory<Program> factory, Guid id)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("X-Usuario-Prueba", id.ToString());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "sesion-prueba");
        return client;
    }

    private static async Task<(Guid Id, string Sku)> Producto(HttpClient admin, CategoriaProducto categoria)
    {
        var sku = Guid.NewGuid().ToString();
        var response = await admin.PostAsJsonAsync("/api/productos", new GuardarProducto { Sku = sku, Nombre = "Imagen de prueba", Categoria = categoria });
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return ((await response.Content.ReadFromJsonAsync<EscrituraResultado>())!.Id, sku);
    }

    private static MultipartFormDataContent Formulario(Guid operacion, byte[]? bytes = null)
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent(operacion.ToString()), "idOperacion");
        var contenido = new ByteArrayContent(bytes ?? ImagenesTests.Png);
        contenido.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(contenido, "archivo", "pixel.png");
        return form;
    }

    private static async Task<ImagenProductoResultado> Subir(HttpClient client, Guid id, Guid operacion)
    {
        using var form = Formulario(operacion);
        var response = await client.PostAsync($"/api/productos/{id}/imagen", form);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<ImagenProductoResultado>())!;
    }

    private sealed class StorageSimulado : IImagenStorage
    {
        private readonly ConcurrentDictionary<string, byte[]> objetos = new();
        public bool Fallar { get; set; }
        public int Intentos { get; private set; }
        public string ObtenerUrlPublica(string ruta) => "https://imagenes.example/" + ruta;
        public Task SubirAsync(string ruta, ImagenValidada imagen, string token, CancellationToken ct)
        {
            Intentos++;
            if (Fallar)
            {
                Fallar = false;
                throw new OperacionException(502, "Storage no disponible en la prueba.");
            }
            objetos.TryAdd(ruta, imagen.Contenido);
            return Task.CompletedTask;
        }
    }
}
