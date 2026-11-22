# R05 — Mercado Pago (backend)

## Estado

El contrato de backend está implementado localmente. La migración `20261122210353_R05MercadoPago` ya se aplicó en Supabase el 22 de noviembre. Falta conciliación de operaciones, configurar credenciales de prueba, publicar el webhook en HTTPS y recorrer los flujos con una cuenta de prueba. La integración del frontend sigue pendiente.

## Configuración

El backend lee estos valores de la sección `MercadoPago`:

| Clave | Uso |
| --- | --- |
| `AccessToken` | Token de acceso de la aplicación. En desarrollo, usar credenciales de prueba. |
| `WebhookSecret` | Firma secreta de Webhooks de esa aplicación. |
| `NotificationUrl` | URL pública HTTPS del endpoint `/api/webhooks/mercadopago`. |
| `SuccessUrl`, `FailureUrl`, `PendingUrl` | URLs HTTPS del frontend para el retorno visual; no confirman pagos. |
| `UsarEntornoPruebas` | `true` por defecto, redirige a `sandbox_init_point`; cambiar a `false` al configurar producción. |

Configurar los secretos localmente con User Secrets o variables de entorno. No guardar tokens ni secretos en `appsettings.json`, el repositorio, el navegador o el QR.

## Contrato para frontend

Todas las rutas de checkout requieren JWT de usuario activo. El servidor calcula el importe desde el pedido o la reserva y devuelve la URL alojada por Mercado Pago. El frontend puede redirigir a `checkoutUrl` o mostrarla como QR para cobro en mostrador.

| Método y ruta | Acceso | Resultado |
| --- | --- | --- |
| `POST /api/pedidos/{id}/checkout` | Cliente propietario del pedido | 200 `CheckoutMercadoPago` |
| `POST /api/reservas/{id}/checkout` | Cliente propietario de la reserva; debe iniciar antes del vencimiento | 200 `CheckoutMercadoPago` |
| `POST /api/reservas/administracion/{id}/checkout` | Recepción o Administrador | 200 `CheckoutMercadoPago`, para la reserva de mostrador |
| `POST /api/webhooks/mercadopago?data.id={paymentId}` | Mercado Pago, sin JWT | Verifica firma y consulta el pago directamente con Mercado Pago |

Ejemplo de respuesta:

```json
{
  "pagoId": "UUID",
  "preferenciaId": "id de preferencia",
  "checkoutUrl": "URL devuelta por Mercado Pago",
  "importe": 1250.00,
  "estado": "Pendiente"
}
```

`checkoutUrl` es el destino de pago; en entorno de pruebas corresponde a la URL sandbox. No usar la navegación de retorno ni sus parámetros como prueba de pago. El webhook valida `x-signature` con HMAC-SHA256 y después consulta `GET /v1/payments/{id}`. Solo un pago `approved`, de importe exacto y moneda MXN, puede confirmar el pedido o la reserva.

## Plazo de pago y disponibilidad

Los diez minutos limitan el inicio del checkout. El backend registra el intento con la hora de inicio y mantiene ocupadas las líneas mientras el pago está pendiente, aunque llegue la aprobación después del vencimiento original. Cuando Mercado Pago informa un estado terminal rechazado/cancelado, se libera esa protección; si se aprueba, la reserva queda pagada y ocupa la línea hasta el fin del turno.

El checkout de reservas excluye pagos en efectivo/voucher, transferencias y cajeros para evitar medios de pago diferidos. Pedidos de tienda conservan los medios que Mercado Pago habilite por defecto.

## Migración y pruebas reales

La conciliación automática/manual sigue pendiente; por ahora, la notificación vuelve a consultar cada pago en Mercado Pago y procesa eventos duplicados de forma idempotente. La migración `20261122210353_R05MercadoPago` agrega referencias de Checkout Pro y pagos de reserva; quedó aplicada en Supabase el 22 de noviembre de 2026. Luego de configurar la aplicación y credenciales, publicar el API HTTPS y ejecutar una compra de prueba para pedidos y otra para reserva web/mostrador, incluyendo rechazo, notificación duplicada y confirmación posterior al vencimiento.
