# Consulta de disponibilidad · R03.1

Esta entrega calcula turnos y líneas libres usando paquetes visibles, configuración operativa, líneas activas y bloqueos. **Consultar no crea ni bloquea reservas**, no garantiza stock y no descuenta consumibles. Apartados, vencimientos y reservas se implementarán en R03.2/R03.3/R04.

## Endpoint público

`GET /api/disponibilidad?paqueteId={uuid}&fecha=2026-11-20[&hora=11:15&personas=2]`

No requiere sesión para consultar. La creación de reservas seguirá requiriendo cuenta y validaciones del servidor cuando se implemente.

| Parámetro | Regla |
| --- | --- |
| `paqueteId` | UUID obligatorio, no vacío; debe corresponder a un paquete visible en el catálogo público. |
| `fecha` | Obligatoria, `yyyy-MM-dd`, fecha local del club. |
| `hora` | Opcional, hora local `HH:mm` o `HH:mm:ss`; debe coincidir exactamente con un turno configurado, sin segundos adicionales. Si se envía, devuelve solo ese turno. |
| `personas` | Opcional, 1 por defecto, entre 1 y 15. No asigna líneas: comprueba que existan suficientes líneas individuales libres. |

Parámetros inválidos devuelven 400 con Problem Details. Paquetes inexistentes, desactivados o que ya no sean visibles devuelven 404, aplicando las reglas de [PAQUETES.md](PAQUETES.md). No se confía en parámetros adicionales para mostrar paquetes ocultos o saltarse la anticipación.

## Respuesta

```json
{
  "paqueteId": "00000000-0000-0000-0000-000000000001",
  "fecha": "2026-11-20",
  "zonaHoraria": "America/Cancun",
  "consultadoEnUtc": "2026-11-20T15:00:00Z",
  "diaAbierto": true,
  "personas": 1,
  "horarios": [
    {
      "hora": "11:00:00",
      "inicioUtc": "2026-11-20T16:00:00Z",
      "finActividadUtc": "2026-11-20T16:10:00Z",
      "finOcupacionUtc": "2026-11-20T16:15:00Z",
      "disponible": true,
      "lineasLibres": 15,
      "lineas": [
        {
          "id": "e4ec8df3-0000-4000-8000-000000000001",
          "numero": 1,
          "nombre": "Línea 1",
          "disponible": true,
          "estado": "Libre"
        }
      ]
    }
  ]
}
```

El ejemplo abrevia las colecciones: normalmente devuelve todos los turnos del día y las 15 líneas por turno. Las horas de la grilla son locales; los instantes de inicio/fin son UTC. El frontend debe usar la zona indicada al mostrar fechas, no la zona del dispositivo del visitante.

Con los valores iniciales se generan 19 turnos: 11:00, 11:15, …, 15:30. Se usan los valores persistidos de duración y margen, no constantes de 15 minutos. Los campos de fin sirven a la agenda; los paquetes siguen vendiéndose por actividad/tirada y no deben anunciarse como renta por tiempo.

`diaAbierto` indica si el horario semanal permite abrir ese día. Un festivo bloqueado puede devolver `diaAbierto=true` pero todos los turnos sin disponibilidad. Un día semanal cerrado devuelve `diaAbierto=false` y `horarios=[]`. Las fechas pasadas o posteriores al último día de la ventana devuelven `horarios=[]`; `diaAbierto` conserva su significado semanal.

En días de la ventana se incluyen también los turnos que no cumplen la anticipación, marcados como no disponibles. Así el frontend puede mostrar una grilla deshabilitada y el mapa. La hora debe pertenecer a la grilla de un día abierto; si el día está cerrado se devuelve directamente la lista vacía.

## Estados de línea y bloques

- `Libre`: línea activa, dentro de anticipación y sin bloqueos que se superpongan.
- `Bloqueada`: bloqueo activo global o específico para esa línea.
- `Inactiva`: línea desactivada por administración.
- `FueraDeAnticipacion`: línea activa cuyo turno queda fuera de los límites de reserva web. Tiene prioridad sobre el estado de bloqueo; `Inactiva` tiene prioridad sobre ambos.

No se exponen motivos, creadores ni identificadores de los bloqueos. Los cancelados no afectan a la consulta. Se comprueba toda la ocupación `[inicio, finActividad + margen)`: un bloqueo durante el margen también impide usar el turno. Un bloqueo que termina exactamente cuando empieza otro turno no afecta al siguiente. Los bloqueos superpuestos no se cuentan dos veces.

`lineasLibres` cuenta líneas disponibles después de todas esas reglas. El turno tiene `disponible=true` solo si ese número alcanza `personas`. Una solicitud para 15 personas con 14 líneas libres devuelve el mapa de esas líneas y el turno no disponible; no crea asignaciones ni exige que sean líneas contiguas.

## Anticipación y decisión pendiente

Los límites son inclusivos y se aplican al inicio del turno:

`ahora + anticipacionMinimaMinutos <= inicio <= ahora + anticipacionMaximaDias × 24 horas`.

**Interpretación provisional:** los dos días acordados se implementan como una ventana móvil exacta de 48 horas. Sigue pendiente confirmar con el cliente si en realidad quiere dos fechas del calendario local. La configuración permite cambiar el número de días; el modo de interpretar ese corte deberá revisarse si se confirma otra regla. No se marca esta definición comercial como cerrada.

Esta consulta aplica las reglas web incluso si la llama personal autenticado. La excepción de mostrador y las reservas para ese momento se integrarán en R04; no se permite saltar la anticipación con un parámetro público.

## Consistencia e integración futura

En PostgreSQL se leen paquete, configuración, líneas y bloqueos dentro de una transacción `RepeatableRead`, sin escrituras. Una edición simultánea se refleja en una consulta posterior, evitando mezclar partes de diferentes estados del club. La respuesta usa `Cache-Control: no-store`.

Es una fotografía informativa, no una garantía de capacidad futura. R03.2 deberá volver a validar y proteger la capacidad al crear el apartado; R03.3 verificará vencimientos y concurrencia. Antes de habilitar reservas reales será obligatorio incorporar apartados vigentes y reservas confirmadas al cálculo, además del tratamiento de pagos tardíos. No basta con conectar este GET para poner el flujo de reserva en producción.

No se agregan tablas ni migraciones en R03.1. Se reutilizan las migraciones de R01/R02. Las pruebas cubren HTTP, conversión horaria, margen, bloqueos globales, líneas inactivas, anticipación, grupos, cambios de configuración, lectura consistente PostgreSQL y ausencia de escrituras. El frontend permanece sin cambios hasta que el equipo conecte el endpoint.
