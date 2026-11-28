# A01 · Base de órdenes de alimentos

Implementada el 27 nov 2026. Este módulo separa la preparación de la confirmación de pago y de la entrega; usa productos de categoría Alimento y el inventario existentes. No incorpora Mercado Pago ni modifica sus credenciales.

## Activación y alcance

**Desactivado para producción por defecto:** `Alimentos:OrdenesHabilitadas=false` en appsettings. Las escrituras devuelven 503 mientras está desactivado. En pruebas controladas se activa con `Alimentos__OrdenesHabilitadas=true` (reiniciar la aplicación según el entorno). No se activó en Azure ni se modificaron User Secrets para habilitarlo.

La activación operativa requiere resolver OQ-05: protección de existencias mientras se preparan órdenes, tratamiento de faltantes/no recogidos y acceso de barra. Crear comprueba las existencias actuales **sin apartarlas ni descontarlas**; entregar vuelve a comprobarlas. Esto impide entregar por encima de existencias, pero **no garantiza stock a todas las solicitudes aceptadas para preparar**. No se inventó una regla de reservas de stock, sustituciones, cancelaciones o reembolsos. La bandera es de habilitación del módulo, no un horario de cocina ni un botón diario de apertura/cierre.

La carta pública y las lecturas de órdenes siguen disponibles con sus permisos. No se exige reserva para pedir alimentos. Las órdenes web requieren cuenta activa; Recepción puede crear una orden local para un cliente registrado o para una visita sin cuenta, identificada por nombre de recogida. No hay pedidos web anónimos ni entrega en la línea de tiro.

## Rutas

Las rutas nuevas son independientes de `/api/alimentos/{sku}` para no reservar ni romper un SKU llamado `ordenes`.

| Método y ruta | Permiso y resultado |
| --- | --- |
| POST `/api/ordenes-alimentos` | Perfil activo; crea para sí mismo, devuelve 200 con la orden. |
| GET `/api/ordenes-alimentos` | Lista paginada de las órdenes propias, incluidas las creadas por mostrador para ese cliente. |
| GET `/api/ordenes-alimentos/{id}` | Solo orden propia; desconocida/ajena devuelve 404. |
| POST `/api/administracion/ordenes-alimentos` | Administrador/Recepción activos; alta de mostrador. |
| GET `/api/administracion/ordenes-alimentos` | Administrador/Recepción activos; bandeja paginada. |
| GET `/api/administracion/ordenes-alimentos/{id}` | Administrador/Recepción activos; detalle operativo. |
| POST `/api/administracion/ordenes-alimentos/{id}/estado` | Avanza a EnPreparacion o Listo. |
| POST `/api/administracion/ordenes-alimentos/{id}/cobro` | Registra el cobro local completo confirmado por el operador. |
| POST `/api/administracion/ordenes-alimentos/{id}/entrega` | Entrega y descuenta inventario una sola vez. |

El rol Barra no se creó ni obtiene permisos por equivalencia. Todas las lecturas llevan `Cache-Control: no-store`. Los filtros son `pagina` (1 por defecto), `tamanoPagina` (20, máximo 100), `estado`, `pagada`, `desdeUtc` inclusivo y `hastaUtc` exclusivo sobre la fecha de creación. Orden por creación descendente y UUID como desempate. Estados, fechas y paginación inválidos devuelven 400. Respuesta paginada habitual `{ elementos, total, pagina, tamanoPagina }`.

## Alta

```json
{
  "idOperacion": "uuid-nuevo",
  "partidas": [{ "productoId": "uuid-alimento", "cantidad": 2 }],
  "notas": "Sin hielo"
}
```

Los UUID del ejemplo son marcadores: usar valores reales. Entre 1 y 50 partidas diferentes, 1–100 unidades por partida. Precio, nombre y SKU se toman del servidor y se conservan históricamente; cambios posteriores del producto no alteran la orden. No se aceptan otras categorías, productos ocultos/no vendibles/sin precio válido, cantidades duplicadas o existencias insuficientes. No hay mezcla de ropa y alimentos dentro de esta orden.

En mostrador puede enviarse `clienteId` de un perfil activo; su nombre se toma del perfil. Si se omite, `nombreRecogida` es obligatorio (máximo 150 caracteres). Notas opcionales, máximo 500 caracteres, texto plano. Un cliente web no puede elegir otra cuenta. El actor y el origen Web/Mostrador se determinan en el servidor, no desde el cuerpo.

El id de la orden coincide con `idOperacion` de alta. Repetir la misma solicitud por el mismo actor devuelve la orden existente; cambiar cliente, notas, cantidades u origen con el mismo id devuelve 409. Reordenar las partidas no cambia el significado de una solicitud. Un cambio posterior de precio o nombre no invalida el reintento original.

## Preparación, cobro y entrega

La orden devuelve `estado`, `estadoPago`, `version`, `total`, `importeConfirmado`, `metodoCobro`, `referenciaCobro`, fechas UTC y `partidas` históricas con id propio. Estado inicial `Recibido`, pago `Pendiente`.

Cada cambio requiere un UUID nuevo `idOperacion` y la `version` actual obtenida al consultar. Un reintento conserva **ambos y el resto del cuerpo original**. El servidor guarda operación/actor/huella; una repetición válida devuelve el estado actual sin repetir efectos ni retrocederlo. Reutilización incompatible o versión desactualizada devuelve 409. Id/versión vacíos devuelven 400.

```json
{
  "idOperacion": "uuid-nuevo",
  "version": "uuid-version-actual",
  "estado": "EnPreparacion"
}
```

Flujo único `Recibido → EnPreparacion → Listo → Entregado`. No se permite saltar, retroceder ni usar `/estado` para entregar. Preparar y marcar Listo no requieren pago y no alteran el inventario.

En `/cobro`, cuando esté Listo y todavía sin pago:

```json
{
  "idOperacion": "uuid-nuevo",
  "version": "uuid-version-actual",
  "metodo": "Transferencia",
  "referencia": "Referencia del comprobante",
  "importe": 50.00
}
```

Solo `Tarjeta` o `Transferencia`. Importe exacto del total, sin pagos parciales ni efectivo. La referencia es obligatoria y tiene hasta 150 caracteres. **Es un registro manual del operador después de comprobar el cobro fuera de esta API:** no carga una tarjeta, no comprueba un depósito bancario ni reemplaza la futura integración de Mercado Pago. No almacenar números de tarjeta, contraseñas ni credenciales en referencia/notas. Confirmar el cobro marca el pago como Confirmado y no descuenta stock ni entrega automáticamente.

`/entrega` recibe solo `idOperacion` y `version`. Requiere Listo y pago completo confirmado. Comprueba todas las partidas y registra sus salidas junto con el estado Entregado, versión, fechas y auditoría en una transacción SERIALIZABLE. Si falta cualquier producto, responde 409 sin descontar parcialmente ni alterar el pago ya registrado. No se generan sustituciones, saldos ni reembolsos. Una segunda operación distinta no puede entregar una orden entregada; repetir la operación original devuelve éxito sin otro descuento.

Hasta completar el flujo no se puede cambiar la categoría de sus productos. Ocultar un producto o cambiar su precio no cancela las órdenes existentes. F22 bloquea el borrado de productos con partidas de estas órdenes, cualquiera que sea su estado.

## Persistencia y límites de la entrega

Entidades en Persistence/Models: `OrdenAlimentos`, `DetalleOrdenAlimentos`, `OperacionOrdenAlimentos`. Relaciones Restrict, totales/precios decimal(12,2), comprobaciones de estado/pago/entrega y UUID de versión. `MovimientoInventario.DetalleOrdenAlimentosId` es nullable para conservar los movimientos existentes, con FK e índice único para evitar dos descuentos de la misma partida. Las tablas nuevas tienen RLS y permisos directos revocados para `anon` y `authenticated`. Auditoría por solicitud, actor, entidad y campos.

Migración `20261128030835_BaseOrdenesAlimentos` aplicada en Supabase. No se crearon órdenes, cobros ni consumos reales. El flujo se comprobó con perfiles de prueba y PostgreSQL en esquemas aislados.

**El flujo anterior de `/api/pedidos` permanece separado:** sigue admitiendo sus ventas alimenticias existentes y descuenta al confirmar pago. Este desarrollo no migra pedidos antiguos ni redirige el carrito automáticamente. Antes de activar A01, Thiago/equipo deben conectar alimentos a las rutas nuevas y acordar la transición de pedidos anteriores/mixtos, evitando crear la misma compra en ambos sistemas.

El reporte actual F15 sigue incluyendo partidas alimenticias de pedidos generales; **no agrega todavía estas órdenes de cocina**. No se da por completo el módulo comercial de alimentos. Siguen pendientes: pago online, protección del stock durante preparación, resolución de faltantes y no recogidos, permisos de barra, horarios/subcategorías, política visible y métricas/reportes propios. Frontend a cargo de Thiago; no se modificó en este bloque.
