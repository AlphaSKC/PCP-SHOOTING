# Apartados temporales · R03.2

Permite a un usuario con perfil activo apartar una o varias líneas para el mismo paquete, fecha y horario. **Un apartado no es una reserva confirmada ni un pago**. No crea pedidos, QR de pago/llegada, cobros, participantes o movimientos de inventario.

## Endpoints

| Método | Ruta | Acceso |
| --- | --- | --- |
| POST | `/api/apartados` | Usuario autenticado con perfil activo; crea para sí mismo. |
| GET | `/api/apartados/{id}` | Solo propietario con perfil activo, incluso si el apartado venció. |

Requieren `Authorization: Bearer <access_token>` y no permiten caché. Un usuario distinto, incluido personal administrativo, recibe 404 al consultar un apartado ajeno en esta ruta. Las operaciones de personal para otro cliente y la excepción de anticipación de mostrador corresponden a R04. Un `usuarioId` enviado por el navegador no cambia al propietario.

```json
{
  "idOperacion": "00000000-0000-4000-8000-000000000001",
  "paqueteId": "REEMPLAZAR-POR-ID-DEL-PAQUETE-VISIBLE",
  "fecha": "2026-11-20",
  "hora": "12:00:00",
  "lineas": [
    "e4ec8df3-0000-4000-8000-000000000001",
    "e4ec8df3-0000-4000-8000-000000000002"
  ]
}
```

Sustituir los ejemplos por IDs reales y fecha/hora vigentes. Cada intento nuevo usa un UUID nuevo como `idOperacion`. Un reintento de la misma operación conserva ese UUID y exactamente la selección original. Las líneas son IDs de la respuesta de disponibilidad, no sus números.

Reglas:

- De 1 a 15 líneas distintas, una persona por línea, todas en el mismo turno. No se hacen asignaciones automáticas ni se aceptan grupos parciales.
- La fecha y hora son locales de `America/Cancun`. Deben cumplir el horario semanal, la grilla, la anticipación web, los bloqueos y las líneas activas de [OPERACION.md](OPERACION.md).
- El paquete debe estar visible según [PAQUETES.md](PAQUETES.md). El corte de dos días continúa siendo provisionalmente una ventana móvil de 48 horas.
- Se revalida la selección completa al guardar. Una consulta GET anterior no garantiza que la capacidad siga libre.
- El plazo se toma de `apartadoPagoMinutos`, inicialmente 10. `venceEnUtc` se guarda una sola vez. Cambiar la configuración no modifica los apartados anteriores.
- Inicio, fin de actividad y fin de ocupación, incluido el margen, también se guardan. Si se cambia la grilla, el apartado existente conserva su rango original y bloquea cualquier turno nuevo que se superponga con él.

## Respuesta e idempotencia

POST devuelve 201 con Location apuntando al GET. Ambos devuelven `id`, `paqueteId`, `fecha`, `hora`, `zonaHoraria`, `inicioUtc`, `finActividadUtc`, `finOcupacionUtc`, `creadoEnUtc`, `venceEnUtc`, `estado` y `lineas`.

`id` coincide con `idOperacion`. Las líneas forman un conjunto, por lo que su orden no importa para un reintento. El orden de la respuesta no representa una asignación de participantes.

El mismo propietario, UUID y selección devuelve el apartado original, sin duplicarlo ni reiniciar el contador. Cambiar paquete, fecha, hora, líneas o propietario reutilizando ese UUID devuelve 409. Si el apartado ya venció, un reintento devuelve su información original con `estado=Vencido`; para intentar apartar de nuevo se necesita otro UUID y disponibilidad actual. POST puede devolver 201 para ese reintento histórico: el frontend debe comprobar también `estado` y `venceEnUtc`.

Los estados se calculan al consultar:

- `Vigente`: `venceEnUtc` es estrictamente posterior al reloj UTC del servidor.
- `Vencido`: el reloj llegó al vencimiento o lo superó.

Llegar exactamente al vencimiento libera la capacidad lógica. No se necesita que un proceso borre filas antes de aceptar un apartado nuevo. Los registros vencidos se conservan para historial; R03.3 ampliará la gestión de vencimientos, consolidación y pruebas de límites. No hay endpoint de renovación o cancelación en R03.2.

Errores principales: 400 para datos inválidos, selección duplicada o líneas inexistentes; 401 sin sesión; 403 perfil desactivado; 404 paquete no visible o consulta ajena/inexistente; 409 selección ocupada, bloqueada, fuera de anticipación, sin turno o reutilización incompatible del UUID. Una hora que no pertenece a la grilla devuelve 400.

## Disponibilidad y concurrencia

[GET de disponibilidad](DISPONIBILIDAD.md) ahora incorpora los apartados vigentes. Una línea ocupada por ellos aparece como `Apartada` sin revelar quién la apartó, su UUID ni su plazo privado. Los rangos son semiabiertos `[inicio, finOcupacion)`: un turno que empieza exactamente al terminar la ocupación anterior no se superpone. Un apartado vencido no descuenta líneas.

En PostgreSQL se valida e inserta dentro de una transacción serializable compartida. Los conflictos de serialización se reintentan con lecturas nuevas. Solo un cliente puede ganar una selección solapada; el perdedor recibe 409 si ya no existe capacidad. Cada grupo se guarda completo junto con su auditoría. Las restricciones de unicidad protegen los UUID y referencias duplicadas; la prevención de superposición temporal en esta etapa depende de ese flujo transaccional del backend.

## Persistencia y siguientes entregas

La migración `ApartadosTemporales` crea `Apartados` y `LineasApartado` con índices, claves restrictivas y validaciones de rangos. Activa RLS y revoca acceso directo a `anon` y `authenticated`; el navegador usa la API. Guarda versiones de paquete/configuración como referencia, **no una cotización ni copia completa de precios o consumibles**. R04/R05 deberán definir y persistir esos datos al construir la reserva y el checkout.

R03.2 no confirma ni extiende apartados por retornar del checkout. Mercado Pago todavía no se integra en este flujo. Antes de habilitar compras/reservas reales, R04/R05 deberán relacionar el apartado con la reserva, incorporar la ocupación confirmada y resolver los pagos tardíos o duplicados. No asumir que un apartado sigue vigente porque el navegador terminó de pagar.

Tampoco se verifica ni reserva stock de consumibles en esta etapa. La disponibilidad pública sigue siendo informativa: lo que protege capacidad es POST del apartado, y solo durante su vigencia. Las nuevas reglas de bloqueos, líneas o paquetes pueden afectar una confirmación futura y deberán revalidarse en el flujo posterior.

Las pruebas incluyen HTTP/privacidad, reintentos, vencimiento exacto, plazo configurable, selección atómica, nuevas grillas y solicitudes simultáneas contra PostgreSQL. El frontend permanece sin cambios hasta conectar estos endpoints.
