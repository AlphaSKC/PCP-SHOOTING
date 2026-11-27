using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Domain.Services;
using PCP_Shooting.Persistence.Interfaces;

namespace PCP_Shooting.Persistence.Services;

public sealed class SupabaseImagenStorage : IImagenStorage
{
    public const string Bucket = "pcp-productos";
    private readonly HttpClient http;
    private readonly string apiKey;
    private readonly Uri raiz;

    public SupabaseImagenStorage(HttpClient http, IConfiguration config)
    {
        this.http = http;
        apiKey = config["Supabase:PublishableKey"] ?? "";
        var url = config["Supabase:Url"];
        if (!Uri.TryCreate(url, UriKind.Absolute, out var origen) || origen.Scheme != "https" ||
            origen.AbsolutePath != "/" || string.IsNullOrWhiteSpace(apiKey))
            throw new OperacionException(503, "Falta configurar Supabase Storage en el backend.");
        raiz = new Uri(origen, "/storage/v1/");
        http.Timeout = TimeSpan.FromSeconds(30);
    }

    public string ObtenerUrlPublica(string ruta) =>
        new Uri(raiz, "object/public/" + BucketPara(ruta) + "/" + Escapar(ruta)).AbsoluteUri;

    public async Task SubirAsync(string ruta, ImagenValidada imagen, string token, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(raiz, "object/" + BucketPara(ruta) + "/" + Escapar(ruta)));
            request.Headers.Add("apikey", apiKey);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("x-upsert", "false");
            request.Content = new ByteArrayContent(imagen.Contenido);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(imagen.TipoContenido);
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (response.IsSuccessStatusCode) return;
            if (await EsDuplicado(response, ct))
            {
                await VerificarExistente(ruta, imagen.Sha256, ct);
                return;
            }
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new OperacionException(401, "La sesión de Storage venció. Inicia sesión de nuevo.");
            if (response.StatusCode == HttpStatusCode.Forbidden)
                throw new OperacionException(403, "Storage rechazó el permiso de carga para esta imagen.");
            throw new OperacionException(502, "Supabase Storage no pudo guardar la imagen. Reintenta con el mismo IdOperacion.");
        }
        catch (HttpRequestException)
        {
            throw new OperacionException(502, "No se pudo conectar con Supabase Storage. Reintenta con el mismo IdOperacion.");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new OperacionException(504, "Supabase Storage tardó demasiado. Reintenta con el mismo IdOperacion.");
        }
    }

    private async Task VerificarExistente(string ruta, string hash, CancellationToken ct)
    {
        using var response = await http.GetAsync(ObtenerUrlPublica(ruta), HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
            throw new OperacionException(502, "No se pudo verificar la imagen existente. Reintenta la operación.");
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[81920];
        long total = 0;
        int leidos;
        while ((leidos = await stream.ReadAsync(buffer.AsMemory(), ct)) > 0)
        {
            total += leidos;
            if (total > ValidadorImagen.MaximoBytes)
                throw new OperacionException(409, "La ruta ya contiene un archivo diferente.");
            digest.AppendData(buffer, 0, leidos);
        }
        if (Convert.ToHexString(digest.GetHashAndReset()).ToLowerInvariant() != hash)
            throw new OperacionException(409, "La ruta ya contiene un archivo diferente.");
    }

    private static async Task<bool> EsDuplicado(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.StatusCode == HttpStatusCode.Conflict) return true;
        if (response.StatusCode != HttpStatusCode.BadRequest) return false;
        try
        {
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            return json.RootElement.ValueKind == JsonValueKind.Object &&
                json.RootElement.TryGetProperty("error", out var error) &&
                error.ValueKind == JsonValueKind.String && error.GetString() == "Duplicate";
        }
        catch (JsonException) { return false; }
    }

    private static string Escapar(string ruta) =>
        string.Join("/", ruta.Split('/').Select(Uri.EscapeDataString));

    private static string BucketPara(string ruta) => ruta.StartsWith("marcas/", StringComparison.Ordinal) ? "pcp-marcas"
        : ruta.StartsWith("blog/", StringComparison.Ordinal) ? "pcp-blog"
        : ruta.StartsWith("tienda/", StringComparison.Ordinal) ? "pcp-tienda"
        : ruta.StartsWith("galeria/", StringComparison.Ordinal) ? "pcp-galeria" : Bucket;
}
