using System.Buffers.Binary;
using System.Security.Cryptography;
using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Domain.Services;

public static class ValidadorImagen
{
    public const int MaximoBytes = 5 * 1024 * 1024;

    public static async Task<ImagenValidada> ValidarAsync(Stream archivo, string nombre, string tipoContenido, CancellationToken ct)
    {
        using var memoria = new MemoryStream();
        var buffer = new byte[81920];
        int leidos;
        while ((leidos = await archivo.ReadAsync(buffer.AsMemory(), ct)) > 0)
        {
            if (memoria.Length + leidos > MaximoBytes)
                throw new OperacionException(413, "La imagen supera el límite de 5 MB.");
            memoria.Write(buffer, 0, leidos);
        }
        var contenido = memoria.ToArray();
        if (contenido.Length == 0) throw new OperacionException(400, "El archivo está vacío.");
        var formato = Detectar(contenido);
        var extension = Path.GetExtension(nombre).ToLowerInvariant();
        if (extension == ".jpeg") extension = ".jpg";
        if (formato is null || extension != formato.Value.Extension)
            throw new OperacionException(415, "El archivo debe ser una imagen JPG, PNG o WebP y coincidir con su extensión.");
        if (!string.IsNullOrWhiteSpace(tipoContenido) && tipoContenido != "application/octet-stream" &&
            !string.Equals(tipoContenido, formato.Value.Mime, StringComparison.OrdinalIgnoreCase))
            throw new OperacionException(415, "El tipo de contenido no coincide con la firma de la imagen.");
        return new ImagenValidada(contenido, extension, formato.Value.Mime,
            Convert.ToHexString(SHA256.HashData(contenido)).ToLowerInvariant());
    }

    // Comprueba firmas y estructura básica; el servicio no decodifica ni transforma el contenido.
    private static (string Extension, string Mime)? Detectar(byte[] bytes)
    {
        var datos = bytes.AsSpan();
        if (datos.Length >= 45 && datos.StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) &&
            BinaryPrimitives.ReadUInt32BigEndian(datos.Slice(8, 4)) == 13 &&
            datos.Slice(12, 4).SequenceEqual("IHDR"u8) &&
            datos[^12..].SequenceEqual(new byte[] { 0, 0, 0, 0, 73, 69, 78, 68, 174, 66, 96, 130 }))
            return (".png", "image/png");
        if (datos.Length >= 4 && datos[0] == 255 && datos[1] == 216 && datos[2] == 255 &&
            datos[^2] == 255 && datos[^1] == 217)
            return (".jpg", "image/jpeg");
        if (datos.Length >= 20 && datos[..4].SequenceEqual("RIFF"u8) &&
            datos.Slice(8, 4).SequenceEqual("WEBP"u8) &&
            BinaryPrimitives.ReadUInt32LittleEndian(datos.Slice(4, 4)) == datos.Length - 8 &&
            (datos.Slice(12, 4).SequenceEqual("VP8 "u8) || datos.Slice(12, 4).SequenceEqual("VP8L"u8) ||
             datos.Slice(12, 4).SequenceEqual("VP8X"u8)))
            return (".webp", "image/webp");
        return null;
    }
}