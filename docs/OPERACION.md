# Configuración operativa, líneas y bloqueos

Este módulo implementa la base administrativa de las reservaciones. R03.1 consume esta configuración para la [consulta pública de disponibilidad](DISPONIBILIDAD.md) y R03.2 para los [apartados temporales](APARTADOS.md). Todavía no existen reservas confirmadas, cobros ni registros de llegada de ese flujo.

## Valores iniciales acordados

| Regla | Valor inicial |
| --- | --- |
| Zona horaria del club | `America/Cancun`, fija en esta versión |
| Atención | Martes a domingo, 08:00–18:00; lunes cerrado |
| Primer y último inicio reservable | 11:00 y 15:30 |
| Actividad y margen | 10 + 5 minutos |
| Intervalo de inicio de turnos | 15 minutos, calculado como actividad + margen |
| Apartado para pagar | 10 minutos, configurable |
| Anticipación mínima y máxima | 60 minutos y 2 días |
| Llegada anticipada y tolerancia | 3 minutos cada una |
| Líneas | 15, inicialmente activas, una persona por línea |

Los diez minutos de apartado para completar el pago son independientes de la duración de actividad. Estos valores se guardan en PostgreSQL; no son únicamente opciones del frontend. R03.2 conserva el vencimiento y ocupación originales de cada apartado. Las reservas confirmadas conservarán sus tiempos contratados cuando se implemente ese módulo.

## Permisos y endpoints

Todas las rutas parten de `/api/administracion/operacion`, requieren una sesión activa y usan `Authorization: Bearer <access_token>`.

| Método y recurso | Administrador | Recepción |
| --- | --- | --- |
| GET `/configuracion` | Sí | Sí |
| PUT `/configuracion` | Sí | No |
| GET `/lineas` | Sí | Sí |
| PUT `/lineas/{id}` | Sí | No |
| GET `/bloqueos` y `/bloqueos/{id}` | Sí | Sí |
| POST `/bloqueos` | Sí | Sí |
| PUT `/bloqueos/{id}` | Sí | Sí |

Cliente y perfiles desactivados reciben 403; peticiones sin sesión reciben 401. Las escrituras generan auditoría. Todas las ediciones requieren la `version` recibida en la última consulta o escritura. Una versión ausente o vacía devuelve 400; una desactualizada devuelve 409. Los cambios de configuración y de sus siete horarios se guardan juntos en una única operación de base de datos.

## Configuración y horarios

GET devuelve los valores, `version`, `zonaHoraria`, `intervaloTurnosMinutos` y `horarios`. Para editar, enviar todos los valores modificables, los siete horarios y la versión. `zonaHoraria` e `intervaloTurnosMinutos` son informativos y no se editan directamente.

```json
{
  "version": "COPIAR-VERSION-DEL-GET",
  "duracionActividadMinutos": 10,
  "margenMinutos": 5,
  "apartadoPagoMinutos": 10,
  "anticipacionMinimaMinutos": 60,
  "anticipacionMaximaDias": 2,
  "llegadaAnticipadaMinutos": 3,
  "toleranciaLlegadaMinutos": 3,
  "horarios": [
    { "diaSemana": 0, "cerrado": false, "apertura": "08:00:00", "cierre": "18:00:00", "primerTurno": "11:00:00", "ultimoTurno": "15:30:00" },
    { "diaSemana": 1, "cerrado": true, "apertura": "08:00:00", "cierre": "18:00:00", "primerTurno": "11:00:00", "ultimoTurno": "15:30:00" },
    { "diaSemana": 2, "cerrado": false, "apertura": "08:00:00", "cierre": "18:00:00", "primerTurno": "11:00:00", "ultimoTurno": "15:30:00" },
    { "diaSemana": 3, "cerrado": false, "apertura": "08:00:00", "cierre": "18:00:00", "primerTurno": "11:00:00", "ultimoTurno": "15:30:00" },
    { "diaSemana": 4, "cerrado": false, "apertura": "08:00:00", "cierre": "18:00:00", "primerTurno": "11:00:00", "ultimoTurno": "15:30:00" },
    { "diaSemana": 5, "cerrado": false, "apertura": "08:00:00", "cierre": "18:00:00", "primerTurno": "11:00:00", "ultimoTurno": "15:30:00" },
    { "diaSemana": 6, "cerrado": false, "apertura": "08:00:00", "cierre": "18:00:00", "primerTurno": "11:00:00", "ultimoTurno": "15:30:00" }
  ]
}
```

Los días usan `System.DayOfWeek`: domingo=0 y lunes=1. Las horas son locales del club, sin fecha ni zona en esos campos. No se admiten horarios que crucen medianoche. Deben existir los siete días sin duplicados. En días abiertos, los turnos tienen precisión de minutos, el último inicio debe coincidir con la secuencia calculada desde el primero, y el último turno completo, incluido el margen, debe caber antes del cierre. Al cambiar la actividad o margen puede ser necesario ajustar el último turno.

Límites de validación: actividad 1–240 minutos, margen 0–60, apartado 1–60, anticipación mínima 0–10080 minutos, máxima 1–365 días y llegada/tolerancia 0–60 minutos. La anticipación mínima debe ser menor que la máxima. Son límites técnicos de configuración, no nuevos precios ni reglas comerciales del cliente.

## Líneas

GET devuelve las 15 líneas, incluidas las inactivas, ordenadas por número. Cada registro tiene `id`, `numero`, `nombre`, `activa`, `capacidad` y `version`.

PUT permite cambiar únicamente `nombre` (hasta 80 caracteres, no vacío), `activa` y requiere `version`. El número y la capacidad no se cambian desde la API. No hay creación ni borrado de líneas en esta etapa.

## Bloqueos

```json
{
  "lineaId": null,
  "inicioUtc": "2030-01-02T11:00:00-05:00",
  "finUtc": "2030-01-02T11:15:00-05:00",
  "motivo": "Mantenimiento",
  "activo": true
}
```

`lineaId: null` bloquea todo el club; un ID existente bloquea solo esa línea. Las fechas deben incluir zona horaria; se convierten a UTC. El motivo es obligatorio, hasta 300 caracteres. El fin debe ser posterior al inicio. Se pueden representar días completos enviando sus límites locales con offset. Se permiten bloqueos superpuestos: el cálculo posterior de disponibilidad deberá tratar su unión, no contarlos como capacidad adicional consumida.

Para editar o cancelar, enviar PUT con todos los datos, `version` y `activo: false` para cancelar. No se borra el registro, y se conservan creador y fecha inicial. Un bloqueo no activa una línea desactivada ni abre un día cerrado.

El listado acepta `lineaId`, `activo` (true por defecto), `desdeUtc`, `hastaUtc`, `pagina` (1 por defecto) y `tamanoPagina` (20 por defecto, máximo 100). Devuelve `{ elementos, total, pagina, tamanoPagina }`. Filtrar una línea incluye los bloqueos globales. `activo=false` consulta los cancelados; `activo=` permite consultar ambos estados. Activo significa que el bloqueo no se canceló, no que esté ocurriendo en este momento: usar además el intervalo para consultar periodos concretos.

Los rangos son semiabiertos: `[inicio, fin)`. El filtro devuelve bloqueos que se superponen con el rango consultado; uno que termine exactamente al inicio de la consulta no interfiere con ella.

## Migración e integración siguiente

`ConfiguracionOperativaYLineas` crea cuatro tablas, restricciones, índices, versiones, los siete horarios y las 15 líneas. Activa RLS y revoca el acceso directo de `anon` y `authenticated`. La API conecta con su usuario de base de datos autorizado.

La disponibilidad ya combina horario semanal, líneas activas, bloqueos y apartados vigentes. Los vencidos dejan de ocupar capacidad lógica sin borrado. Falta incorporar reservas confirmadas y completar el flujo de cobro/llegada; esta configuración por sí sola no lo habilita.

La pantalla administrativa del frontend no se modifica en esta entrega. Hay peticiones de ejemplo en el archivo HTTP del backend.
