# R05 — Mercado Pago (backend)

## Estado

Checkout, webhook y conciliación manual están implementados. La migración `20261122210353_R05MercadoPago` se aplicó en Supabase el 22 de noviembre; la conciliación no requiere otra migración. Faltan configurar credenciales de prueba, publicar el webhook en HTTPS y recorrer los flujos con una cuenta de prueba. La integración del frontend y la conciliación programada siguen pendientes.

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

La conciliación manual recupera notificaciones perdidas; no hay tarea programada ni se liberan líneas por un error de red. La migración `20261122210353_R05MercadoPago` agrega referencias de Checkout Pro y pagos de reserva; quedó aplicada en Supabase el 22 de noviembre de 2026. Luego de configurar la aplicación y credenciales, publicar el API HTTPS y ejecutar una compra de prueba para pedidos y otra para reserva web/mostrador, incluyendo rechazo, notificación duplicada y confirmación posterior al vencimiento.

## Conciliación manual · 23 nov 2026

Solo Administrador con perfil activo:

```http
POST /api/administracion/mercadopago/pedidos/{pedidoId}/conciliar?pagina=1
POST /api/administracion/mercadopago/reservas/{reservaId}/conciliar?pagina=1
Authorization: Bearer TOKEN_ADMIN
```

No se envían estado, importe ni identificador de pago desde el navegador. La API verifica que exista el pedido/reserva y busca en Mercado Pago por su referencia exacta. Por cada resultado consulta de nuevo `GET /v1/payments/{id}` y comprueba identificador, referencia y moneda antes de reutilizar las validaciones de importe y confirmación del webhook. La búsqueda utiliza la [API oficial de búsqueda de pagos](https://www.mercadopago.com.mx/developers/es/reference/online-payments/subscriptions/search-payments/get).

Respuesta: `{ "consultados": 2, "pagina": 1, "hayMas": false }`. `consultados` cuenta detalles de pagos válidos consultados, **no cobros confirmados**; puede incluir rechazados o importes que no permiten confirmar. Consultar después el pedido/reserva para conocer su estado efectivo. Se recorren hasta 50 resultados por petición, por fecha ascendente; si `hayMas=true`, solicitar la siguiente página. Páginas válidas: 1–10000. El proveedor limita la búsqueda a pagos de los últimos doce meses.

Repetir una página es seguro: no duplica confirmación ni consumo de inventario. Un intento aprobado posterior puede reemplazar el identificador de un intento aún no confirmado; una confirmación existente conserva su identidad y fecha. Cobros adicionales sobre una operación ya confirmada no generan otra entrega ni otra reserva; su devolución/gestión financiera requiere un flujo posterior.

Sin credenciales: 503; recurso inexistente: 404; parámetros inválidos: 400; sin sesión: 401; rol insuficiente: 403; error HTTP del proveedor: 502. Si falla después de procesar parte de una página, se puede repetir. Los errores no se interpretan como rechazos y el webhook también devuelve 502 si no pudo consultar el pago, permitiendo reintentos del proveedor.

## Pruebas sin cuenta real

17 casos HTTP con transporte simulado cubren permisos, checkout web/mostrador/pedido y reintentos, búsqueda paginada, recuperación sin webhook, firma inválida, referencia ajena, importe y moneda incorrectos, rechazo, aprobación tardía/duplicada, error del proveedor, intentos sucesivos y descuento único de inventario de tienda. La simulación sustituye HTTP y transacciones del proveedor en memoria; no sustituye el servicio real de confirmación de pedidos.

Una prueba PostgreSQL en esquema temporal usa transacciones reales y HTTP simulado para persistir una aprobación tardía y repetir la conciliación sin duplicarla. Estas pruebas no sustituyen las compras sandbox ni la validación pública del webhook.
