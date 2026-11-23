using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Services;

public sealed class MercadoPagoService(
    PcpShootingDbContext db,
    IHttpClientFactory httpClients,
    IOptions<MercadoPagoOptions> options,
    ITransaccionesPersistence transacciones,
    IEscriturasService escrituras,
    TimeProvider reloj) : IMercadoPagoService
{
    private readonly MercadoPagoOptions config = options.Value;

    public async Task<CheckoutMercadoPago> CrearCheckoutPedidoAsync(Guid usuarioId, Guid pedidoId, CancellationToken ct)
    {
        var pedido = await db.Pedidos.AsNoTracking().Include(p => p.Detalles).ThenInclude(d => d.Producto)
            .SingleOrDefaultAsync(p => p.Id == pedidoId && p.UsuarioId == usuarioId, ct)
            ?? throw new OperacionException(404, "Pedido inexistente.");
        if (pedido.Estado != EstadoPedido.PendientePago) throw new OperacionException(409, "El pedido ya no está pendiente de pago.");
        var pagoExistente = await db.Pagos.AsNoTracking().SingleOrDefaultAsync(p => p.PedidoId == pedidoId && p.PreferenciaMercadoPagoId != null, ct);
        if (pagoExistente is not null) return new(pagoExistente.Id, pagoExistente.PreferenciaMercadoPagoId!, pagoExistente.CheckoutUrl!, pagoExistente.Importe, pagoExistente.Estado.ToString());
        var total = pedido.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario);
        if (total <= 0) throw new OperacionException(409, "El pedido no tiene un importe válido.");
        var (pagoId, crear) = await transacciones.EjecutarAsync<(Guid Id, bool Crear)>(async () =>
        {
            var current = await db.Pedidos.Include(p => p.Detalles).SingleOrDefaultAsync(p => p.Id == pedidoId && p.UsuarioId == usuarioId, ct)
                ?? throw new OperacionException(404, "Pedido inexistente.");
            if (current.Estado != EstadoPedido.PendientePago) throw new OperacionException(409, "El pedido ya no está pendiente de pago.");
            var existing = await db.Pagos.SingleOrDefaultAsync(p => p.PedidoId == pedidoId && p.ReferenciaExterna == null, ct);
            if (existing is not null)
            {
                if (existing.PreferenciaMercadoPagoId is not null && existing.CheckoutUrl is not null) return (existing.Id, false);
                if (existing.Estado != EstadoPago.Rechazado) return (existing.Id, false);
                existing.Estado = EstadoPago.Pendiente;
                return (existing.Id, true);
            }
            var pago = new Pago { Id = Guid.NewGuid(), PedidoId = pedidoId, Importe = total, Estado = EstadoPago.Pendiente };
            db.Pagos.Add(pago);
            return (pago.Id, true);
        }, ct);
        if (!crear)
        {
            var existing = await db.Pagos.AsNoTracking().SingleAsync(p => p.Id == pagoId, ct);
            if (existing.PreferenciaMercadoPagoId is null || existing.CheckoutUrl is null)
                throw new OperacionException(409, "La creación del checkout sigue en proceso; vuelve a intentarlo en unos segundos.");
            return new(existing.Id, existing.PreferenciaMercadoPagoId!, existing.CheckoutUrl!, existing.Importe, existing.Estado.ToString());
        }
        var referencia = $"pedido:{pedido.Id:D}";
        try
        {
            var preference = await CrearPreferenciaAsync(referencia, total, pedido.Detalles.Select(d =>
                (d.Producto.Nombre, d.Cantidad, d.PrecioUnitario)).ToArray(), ct);
            return await transacciones.EjecutarAsync<CheckoutMercadoPago>(async () =>
            {
                var pago = await db.Pagos.SingleAsync(p => p.Id == pagoId, ct);
                pago.PreferenciaMercadoPagoId = preference.Id;
                pago.CheckoutUrl = preference.Url;
                return new(pago.Id, preference.Id, preference.Url, total, pago.Estado.ToString());
            }, ct);
        }
        catch
        {
            await transacciones.EjecutarAsync(async () =>
            {
                var pago = await db.Pagos.SingleOrDefaultAsync(p => p.Id == pagoId, ct);
                if (pago is not null && pago.PreferenciaMercadoPagoId is null) pago.Estado = EstadoPago.Rechazado;
                return true;
            }, ct);
            throw;
        }
    }

    public async Task<CheckoutMercadoPago> CrearCheckoutReservaAsync(Guid usuarioId, Guid reservaId, CancellationToken ct)
    {
        var reserva = await db.ReservasWeb.AsNoTracking().Include(r => r.Participantes)
            .SingleOrDefaultAsync(r => r.Id == reservaId && r.UsuarioId == usuarioId, ct)
            ?? throw new OperacionException(404, "Reserva inexistente.");
        return await CrearCheckoutReservaInternoAsync(reserva, ct);
    }

    public async Task<CheckoutMercadoPago> CrearCheckoutReservaMostradorAsync(Guid reservaId, CancellationToken ct)
    {
        var reserva = await db.ReservasWeb.AsNoTracking().Include(r => r.Participantes)
            .SingleOrDefaultAsync(r => r.Id == reservaId, ct) ?? throw new OperacionException(404, "Reserva inexistente.");
        return await CrearCheckoutReservaInternoAsync(reserva, ct);
    }

    private async Task<CheckoutMercadoPago> CrearCheckoutReservaInternoAsync(ReservaWeb reserva, CancellationToken ct)
    {
        if (reserva.PagoConfirmadoEnUtc is not null) throw new OperacionException(409, "La reserva ya está pagada.");
        var existing = await db.PagosReserva.AsNoTracking().SingleOrDefaultAsync(p => p.ReservaId == reserva.Id, ct);
        if (existing is not null)
        {
            if (existing.PreferenciaMercadoPagoId is null || existing.CheckoutUrl is null)
                throw new OperacionException(409, "La creación del checkout sigue en proceso; vuelve a intentarlo en unos segundos.");
            if (existing.Estado == EstadoPago.Rechazado && reserva.VencePagoEnUtc <= reloj.GetUtcNow().UtcDateTime)
                throw new OperacionException(409, "El plazo para iniciar un nuevo intento de pago venció.");
            return new(existing.Id, existing.PreferenciaMercadoPagoId, existing.CheckoutUrl, existing.Importe, existing.Estado.ToString());
        }
        var inicioIntentoUtc = reloj.GetUtcNow().UtcDateTime;
        if (reserva.VencePagoEnUtc <= inicioIntentoUtc) throw new OperacionException(409, "El plazo para iniciar el pago venció.");
        var (intento, crearPreferencia) = await transacciones.EjecutarAsync<(Guid Id, bool CrearPreferencia)>(async () =>
        {
            var actual = await db.ReservasWeb.SingleAsync(r => r.Id == reserva.Id, ct);
            var otro = await db.PagosReserva.SingleOrDefaultAsync(p => p.ReservaId == reserva.Id, ct);
            if (otro is not null)
            {
                if (otro.PreferenciaMercadoPagoId is null && otro.Estado == EstadoPago.Rechazado && actual.VencePagoEnUtc > inicioIntentoUtc)
                {
                    otro.Estado = EstadoPago.Pendiente;
                    otro.CreadoEnUtc = inicioIntentoUtc;
                    return (otro.Id, true);
                }
                return (otro.Id, false);
            }
            if (actual.PagoConfirmadoEnUtc is not null || actual.VencePagoEnUtc <= inicioIntentoUtc)
                throw new OperacionException(409, "El plazo para iniciar el pago venció.");
            var pago = new PagoReserva { Id = Guid.NewGuid(), ReservaId = reserva.Id, Importe = reserva.Total,
                Estado = EstadoPago.Pendiente, CreadoEnUtc = inicioIntentoUtc };
            db.PagosReserva.Add(pago);
            return (pago.Id, true);
        }, ct);
        if (!crearPreferencia)
        {
            var existente = await db.PagosReserva.AsNoTracking().SingleAsync(p => p.Id == intento, ct);
            if (existente.PreferenciaMercadoPagoId is null || existente.CheckoutUrl is null)
                throw new OperacionException(409, "La creación del checkout sigue en proceso; vuelve a intentarlo en unos segundos.");
            return new(existente.Id, existente.PreferenciaMercadoPagoId, existente.CheckoutUrl, existente.Importe, existente.Estado.ToString());
        }
        var pagoExistente = await db.PagosReserva.AsNoTracking().SingleAsync(p => p.Id == intento, ct);
        if (pagoExistente.PreferenciaMercadoPagoId is not null && pagoExistente.CheckoutUrl is not null)
            return new(pagoExistente.Id, pagoExistente.PreferenciaMercadoPagoId, pagoExistente.CheckoutUrl, pagoExistente.Importe, pagoExistente.Estado.ToString());
        var reference = $"reserva:{reserva.Id:D}";
        try
        {
            var preference = await CrearPreferenciaAsync(reference, reserva.Total,
                [("Reserva " + reserva.NombrePaquete, reserva.Participantes.Count, reserva.PrecioPorPersona)], ct, restringirMediosPago: true);
            return await transacciones.EjecutarAsync<CheckoutMercadoPago>(async () =>
            {
                var pago = await db.PagosReserva.SingleAsync(p => p.Id == intento, ct);
                pago.PreferenciaMercadoPagoId = preference.Id;
                pago.CheckoutUrl = preference.Url;
                return new(pago.Id, preference.Id, preference.Url, pago.Importe, pago.Estado.ToString());
            }, ct);
        }
        catch
        {
            await transacciones.EjecutarAsync(async () =>
            {
                var pago = await db.PagosReserva.SingleOrDefaultAsync(p => p.Id == intento, ct);
                if (pago is not null && pago.PreferenciaMercadoPagoId is null) pago.Estado = EstadoPago.Rechazado;
                return true;
            }, ct);
            throw;
        }
    }

    public async Task<bool> ProcesarNotificacionAsync(string? dataId, string? firma, string? requestId, CancellationToken ct)
    {
        if (!ValidarFirma(dataId, firma, requestId)) return false;
        if (!long.TryParse(dataId, NumberStyles.None, CultureInfo.InvariantCulture, out var paymentId) || paymentId <= 0) return false;
        return await ConsultarYProcesarAsync(paymentId, null, ct);
    }

    public async Task<ConciliacionMercadoPago> ConciliarAsync(string tipo, Guid id, int pagina, CancellationToken ct)
    {
        if (tipo is not ("pedidos" or "reservas") || id == Guid.Empty || pagina < 1 || pagina > 10000)
            throw new OperacionException(400, "Indica pedidos o reservas, un identificador y una página entre 1 y 10000.");
        var existe = tipo == "pedidos" ? await db.Pedidos.AnyAsync(p => p.Id == id, ct) : await db.ReservasWeb.AnyAsync(r => r.Id == id, ct);
        if (!existe) throw new OperacionException(404, "Pedido o reserva inexistente.");
        if (string.IsNullOrWhiteSpace(config.AccessToken)) throw new OperacionException(503, "Mercado Pago aún no está configurado en el backend.");
        var referencia = (tipo == "pedidos" ? "pedido:" : "reserva:") + id.ToString("D");
        const int limite = 50;
        var offset = (pagina - 1) * limite;
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"v1/payments/search?external_reference={Uri.EscapeDataString(referencia)}&sort=date_created&criteria=asc&limit={limite}&offset={offset}");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.AccessToken);
        using var response = await httpClients.CreateClient("MercadoPago").SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) throw new OperacionException(502, "Mercado Pago no pudo consultar los pagos. Reintenta la conciliación.");
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        if (!doc.RootElement.TryGetProperty("results", out var resultados) || resultados.ValueKind != JsonValueKind.Array || resultados.GetArrayLength() > limite)
            throw new OperacionException(502, "Mercado Pago devolvió una búsqueda inválida.");
        var consultados = 0;
        foreach (var resultado in resultados.EnumerateArray())
        {
            if (!resultado.TryGetProperty("id", out var nodoId) || !long.TryParse(nodoId.ToString(), out var pagoId) || pagoId <= 0)
                throw new OperacionException(502, "Mercado Pago devolvió un identificador de pago inválido.");
            // Consultar el detalle: el resultado de búsqueda no basta para confirmar un cobro.
            if (await ConsultarYProcesarAsync(pagoId, referencia, ct)) consultados++;
        }
        var hayMas = doc.RootElement.TryGetProperty("paging", out var paging) && paging.TryGetProperty("total", out var total)
            ? total.GetInt32() > offset + resultados.GetArrayLength() : resultados.GetArrayLength() == limite;
        return new(consultados, pagina, hayMas);
    }

    private async Task<bool> ConsultarYProcesarAsync(long paymentId, string? referenciaEsperada, CancellationToken ct)
    {
        var client = httpClients.CreateClient("MercadoPago");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"v1/payments/{paymentId}");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.AccessToken);
        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) throw new OperacionException(502, "Mercado Pago no pudo consultar el pago. Reintenta la operación.");
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var root = doc.RootElement;
        if (!root.TryGetProperty("id", out var idNode) || idNode.ToString() != paymentId.ToString(CultureInfo.InvariantCulture)) return false;
        var status = root.TryGetProperty("status", out var statusNode) ? statusNode.GetString() : null;
        var reference = root.TryGetProperty("external_reference", out var referenceNode) ? referenceNode.GetString() : null;
        var amount = root.TryGetProperty("transaction_amount", out var amountNode) ? amountNode.GetDecimal() : 0m;
        var currency = root.TryGetProperty("currency_id", out var currencyNode) ? currencyNode.GetString() : null;
        if (string.IsNullOrWhiteSpace(reference) || amount <= 0 || currency != "MXN") return false;
        if (referenciaEsperada is not null && reference != referenciaEsperada) return false;
        if (reference.StartsWith("pedido:", StringComparison.Ordinal))
            await ProcesarPagoPedidoAsync(paymentId.ToString(CultureInfo.InvariantCulture), reference[7..], amount, status, ct);
        else if (reference.StartsWith("reserva:", StringComparison.Ordinal))
            await ProcesarPagoReservaAsync(paymentId.ToString(CultureInfo.InvariantCulture), reference[8..], amount, status, ct);
        else return false;
        return true;
    }

    private async Task ProcesarPagoPedidoAsync(string mpId, string pedidoTexto, decimal importe, string? status, CancellationToken ct)
    {
        if (!Guid.TryParse(pedidoTexto, out var pedidoId)) return;
        var pagoId = await transacciones.EjecutarAsync(async () =>
        {
            var pedido = await db.Pedidos.Include(p => p.Detalles).Include(p => p.Pagos).SingleOrDefaultAsync(p => p.Id == pedidoId, ct);
            if (pedido is null || pedido.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario) != importe) return Guid.Empty;
            var pago = await db.Pagos.SingleOrDefaultAsync(p => p.ReferenciaExterna == mpId, ct);
            if (pago is not null && pago.PedidoId != pedidoId) return Guid.Empty;
            pago ??= pedido.Pagos.FirstOrDefault(p => p.ReferenciaExterna is null && p.Estado == EstadoPago.Pendiente);
            pago ??= pedido.Pagos.FirstOrDefault(p => p.PreferenciaMercadoPagoId != null);
            if (pago is not null && pago.ReferenciaExterna is not null && pago.ReferenciaExterna != mpId)
            {
                if (status != "approved" || pago.Estado is EstadoPago.Confirmado or EstadoPago.Cancelado) return Guid.Empty;
                pago.ReferenciaExterna = mpId;
                pago.Estado = EstadoPago.Pendiente;
                pago.ConfirmadoEnUtc = null;
            }
            if (pago is not null && pago.Estado == EstadoPago.Rechazado && status == "approved")
                pago.Estado = EstadoPago.Pendiente;
            if (pago is null && status == "approved")
            {
                pago = new Pago { Id = Guid.NewGuid(), PedidoId = pedido.Id, Importe = importe, ReferenciaExterna = mpId, Estado = EstadoPago.Pendiente };
                db.Pagos.Add(pago);
            }
            if (pago is null) return Guid.Empty;
            if (pago.ReferenciaExterna is null) pago.ReferenciaExterna = mpId;
            if (status is "rejected" or "cancelled" && pago.Estado == EstadoPago.Pendiente) pago.Estado = EstadoPago.Rechazado;
            return pago.Id;
        }, ct);
        if (pagoId != Guid.Empty && status == "approved") await escrituras.ConfirmarPagoAsync(pagoId, ct);
    }

    private Task ProcesarPagoReservaAsync(string mpId, string reservaTexto, decimal importe, string? status, CancellationToken ct)
    {
        if (!Guid.TryParse(reservaTexto, out var reservaId)) return Task.CompletedTask;
        return transacciones.EjecutarAsync(async () =>
        {
            var pago = await db.PagosReserva.Include(p => p.Reserva).SingleOrDefaultAsync(p => p.ReservaId == reservaId, ct);
            if (pago is null || pago.Importe != importe) return true;
            if (pago.ReferenciaExterna is not null && pago.ReferenciaExterna != mpId &&
                (status != "approved" || pago.Estado is EstadoPago.Confirmado or EstadoPago.Cancelado)) return true;
            pago.ReferenciaExterna = mpId;
            if (status == "approved")
            {
                if (pago.Estado == EstadoPago.Confirmado) return true;
                pago.Estado = EstadoPago.Confirmado;
                pago.ConfirmadoEnUtc = reloj.GetUtcNow().UtcDateTime;
                pago.Reserva.PagoConfirmadoEnUtc = pago.ConfirmadoEnUtc;
            }
            else if (status is "rejected" or "cancelled" && pago.Estado == EstadoPago.Pendiente)
                pago.Estado = EstadoPago.Rechazado;
            return true;
        }, ct);
    }

    private async Task<(string Id, string Url)> CrearPreferenciaAsync(string reference, decimal total,
        (string Nombre, int Cantidad, decimal Precio)[] lineas, CancellationToken ct, bool restringirMediosPago = false)
    {
        if (string.IsNullOrWhiteSpace(config.AccessToken) || string.IsNullOrWhiteSpace(config.NotificationUrl) ||
            string.IsNullOrWhiteSpace(config.SuccessUrl) || string.IsNullOrWhiteSpace(config.FailureUrl) || string.IsNullOrWhiteSpace(config.PendingUrl))
            throw new OperacionException(503, "Mercado Pago aún no está configurado en el backend.");
        if (!Uri.TryCreate(config.NotificationUrl, UriKind.Absolute, out var callback) || callback.Scheme != Uri.UriSchemeHttps ||
            !new[] { config.SuccessUrl, config.FailureUrl, config.PendingUrl }.All(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps))
            throw new OperacionException(503, "Mercado Pago requiere URLs HTTPS válidas.");
        var payload = new
        {
            items = lineas.Select(item => new { title = item.Nombre, quantity = item.Cantidad, unit_price = item.Precio, currency_id = "MXN" }),
            external_reference = reference,
            notification_url = config.NotificationUrl,
            back_urls = new { success = config.SuccessUrl, failure = config.FailureUrl, pending = config.PendingUrl },
            auto_return = "approved",
            payment_methods = restringirMediosPago ? new
            {
                excluded_payment_types = new[] { new { id = "ticket" }, new { id = "bank_transfer" }, new { id = "atm" }, new { id = "prepaid_card" } }
            } : null
        };
        var client = httpClients.CreateClient("MercadoPago");
        using var request = new HttpRequestMessage(HttpMethod.Post, "checkout/preferences");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.AccessToken);
        request.Content = System.Net.Http.Json.JsonContent.Create(payload);
        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) throw new OperacionException(502, "Mercado Pago no pudo crear el checkout.");
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var root = json.RootElement;
        var id = root.GetProperty("id").GetString();
        var campoUrl = config.UsarEntornoPruebas ? "sandbox_init_point" : "init_point";
        var url = root.TryGetProperty(campoUrl, out var checkout) ? checkout.GetString() : null;
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(url)) throw new OperacionException(502, "Mercado Pago devolvió un checkout incompleto.");
        return (id, url);
    }

    private bool ValidarFirma(string? dataId, string? firma, string? requestId)
    {
        if (string.IsNullOrWhiteSpace(config.WebhookSecret) || string.IsNullOrWhiteSpace(firma)) return false;
        var valores = firma.Split(',').Select(par => par.Split('=', 2)).Where(par => par.Length == 2)
            .ToDictionary(par => par[0].Trim(), par => par[1].Trim(), StringComparer.OrdinalIgnoreCase);
        if (!valores.TryGetValue("ts", out var ts) || !valores.TryGetValue("v1", out var hash)) return false;
        var partes = new List<string>();
        if (!string.IsNullOrWhiteSpace(dataId)) partes.Add($"id:{dataId.ToLowerInvariant()};");
        if (!string.IsNullOrWhiteSpace(requestId)) partes.Add($"request-id:{requestId};");
        partes.Add($"ts:{ts};");
        var esperado = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(config.WebhookSecret), Encoding.UTF8.GetBytes(string.Concat(partes))));
        try { return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(esperado), Convert.FromHexString(hash)); }
        catch (FormatException) { return false; }
    }
}
