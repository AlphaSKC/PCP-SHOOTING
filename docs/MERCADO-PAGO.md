# R05 — Mercado Pago (backend)

## Estado

Checkout, webhook y conciliación manual/programada están implementados. La migración `20261122210353_R05MercadoPago` se aplicó en Supabase el 22 de noviembre y `20261124141606_ConciliacionProgramada` el 24 de noviembre de 2026. Faltan configurar credenciales de prueba, publicar el webhook en HTTPS, conectar frontend y recorrer los flujos con una cuenta de prueba. La tarea programada permanece deshabilitada por defecto.

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

La conciliación manual y la tarea programada recuperan notificaciones perdidas; los errores de red no liberan líneas. Luego de configurar la aplicación y credenciales, publicar la API HTTPS y ejecutar una compra de prueba para pedidos y otra para reserva web/mostrador, incluyendo rechazo, notificación duplicada y confirmación posterior al vencimiento.

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

## Conciliación programada · 24 nov 2026

Un servicio en segundo plano consulta pagos pendientes de pedidos identificados como Mercado Pago y pagos de reservas. Excluye pagos manuales. Reutiliza las comprobaciones del webhook y la confirmación idempotente: consultar varias veces no descuenta inventario otra vez ni crea cobros nuevos.

Configuración bajo `MercadoPago:ConciliacionProgramada`:

| Clave | Valor inicial | Límites |
| --- | --- | --- |
| `Habilitada` | `false` | Activación explícita |
| `IntervaloSegundos` | 120 | 30–3600, espera entre ciclos |
| `Lote` | 10 | 1–100 operaciones por ciclo |
| `PaginasPorOperacion` | 2 | 1–10 páginas por operación y ciclo |
| `TimeoutOperacionSegundos` | 30 | 5–120 |
| `RetrasoMinutos` | 2 | 0–60 de antigüedad mínima |

Para activar, configurar `MercadoPago__ConciliacionProgramada__Habilitada=true` y el secreto `MercadoPago__AccessToken`, y reiniciar la API. Sin token no se ejecutan consultas. La antigüedad se toma desde la creación del pedido o del intento de pago de reserva. Los valores se validan al arrancar. No se activó este servicio en el entorno actual.

Se persisten última revisión y página siguiente; se priorizan operaciones que llevan más tiempo sin revisarse. Un fallo afecta solo esa operación y conserva el progreso para reintentar. La página 0 cierra un resultado terminal; la 1 permite revisar de nuevo un pendiente; las siguientes continúan una búsqueda parcial. Al alcanzar el límite de 10000 páginas se registra una advertencia para revisión manual y se reinicia el recorrido. Los registros de diagnóstico incluyen conteos, identificadores y tipo de error, sin tokens ni respuestas completas del proveedor.

Durante una búsqueda parcial de reserva, `ConciliacionEnCurso` conserva la ocupación aunque una página contenga un intento rechazado: todavía puede existir otro pendiente o aprobado. Se reconoce un intento posterior `pending`, `in_process` o `authorized` sin dejarlo oculto por un rechazo anterior. Completar la búsqueda libera esa protección; la conciliación manual también puede completar una revisión interrumpida. Errores, reinicios o desactivar el servicio no liberan una reserva cuya verificación siga incompleta. Revisarla manualmente antes de intervenir; los futuros flujos F06/R06 deben respetar esta protección.

Reabrir un checkout rechazado reactiva su revisión; en reservas exige seguir dentro del vencimiento original. La migración clasifica pagos anteriores como Mercado Pago solo cuando tienen `PreferenciaMercadoPagoId`. Intentos antiguos sin esa evidencia requieren revisión manual. Pagos fuera de la ventana de búsqueda del proveedor también requieren revisión manual.

El servicio funciona mientras la API esté encendida y debe contemplarse al configurar Azure. No superpone ciclos dentro de una instancia; no implementa un bloqueo distribuido entre réplicas. Varias instancias pueden repetir consultas, mientras la confirmación existente conserva su idempotencia.

Verificación de este cambio: 324 pruebas locales aprobadas, 45 PostgreSQL omitidas en esa ejecución y una prueba PostgreSQL específica aprobada por separado en esquema temporal (migración sobre datos anteriores, clasificación de pagos manuales, confirmación y descuento único). Concurrencia omitida por indicación del usuario. No sustituye pruebas sandbox con credenciales reales.

## Pruebas sin cuenta real

17 casos HTTP con transporte simulado cubren permisos, checkout web/mostrador/pedido y reintentos, búsqueda paginada, recuperación sin webhook, firma inválida, referencia ajena, importe y moneda incorrectos, rechazo, aprobación tardía/duplicada, error del proveedor, intentos sucesivos y descuento único de inventario de tienda. La simulación sustituye HTTP y transacciones del proveedor en memoria; no sustituye el servicio real de confirmación de pedidos.

Una prueba PostgreSQL en esquema temporal usa transacciones reales y HTTP simulado para persistir una aprobación tardía y repetir la conciliación sin duplicarla. Estas pruebas no sustituyen las compras sandbox ni la validación pública del webhook.
