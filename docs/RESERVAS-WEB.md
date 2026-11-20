# R04.1 · Reservaciones web

El cliente autenticado transforma un apartado propio vigente en una reserva pendiente de pago. No se amplía el plazo: `vencePagoEnUtc` conserva `venceEnUtc` del apartado. Al vencer se devuelve `Vencida` y la disponibilidad libera las líneas mediante la expiración lógica del apartado original.

## Contrato

Todas las rutas requieren JWT y perfil activo. El propietario sale de la sesión, nunca del cuerpo de la petición.

| Método y ruta | Resultado |
| --- | --- |
| `POST /api/reservas` | 201 con la reserva y cabecera Location. |
| `GET /api/reservas/{id}` | Detalle propio; 404 para inexistente o ajena. |
| `GET /api/reservas?pagina=1&tamanoPagina=20` | Historial propio paginado, más recientes primero. |

```json
{
  "idOperacion": "UUID nuevo conservado para reintentos",
  "apartadoId": "UUID del apartado vigente propio",
  "participantes": [
    {
      "lineaId": "UUID de una línea del apartado",
      "nombre": "Nombre del participante",
      "nacimiento": "2000-01-01",
      "tutorNombre": null,
      "tutorNacimiento": null
    }
  ]
}
```

Cada línea apartada debe tener exactamente un participante. Máximo 15 participantes. Edad mínima: nueve años en la fecha local de la actividad. Menores de 18 requieren nombre y nacimiento de un tutor adulto. Esto captura sus datos; la responsiva y su validación pertenecen a R06.

Un mismo `idOperacion` con los mismos datos devuelve la reserva existente, incluso vencida; nunca renueva capacidad. Cambiar el apartado o los participantes bajo ese ID produce 409. Un apartado solo admite una reserva. Apartado ajeno: 404; vencido, paquete cambiado, línea desactivada o bloqueo nuevo: 409. Datos inválidos: 400. Sin sesión: 401; perfil inactivo: 403.

## Importes y datos guardados

El servidor calcula `total = precioPorPersona × participantes`; el cliente no envía precios. Se guarda copia del código, nombre, descripción, Markdown, versión del paquete, precios, consumibles por persona y totales, fecha, hora, zona y tiempos UTC. Los importes usan `numeric(12,2)`. Editar el catálogo después no modifica esa copia.

Por ahora se aplica la tarifa general. `precioSocioInformativo` conserva el precio CARTAQ del paquete solo como referencia. Falta definir con el cliente qué participantes reciben el beneficio antes de aplicar descuentos. Tampoco se garantiza ni descuenta stock al reservar: el consumo único de inventario se implementará en R06.

## Persistencia y siguientes pasos

Tablas separadas: `ReservasWeb`, `ParticipantesReserva` y `ConsumosReserva`. Relaciones restringidas, apartado único y participante único por línea dentro de la reserva. Escritura transaccional serializable con reintentos existentes y auditoría. RLS y revocación de permisos directos para `anon`/`authenticated`; el acceso pasa por la API.

Migración `20261120203716_ReservasWebIniciales` aplicada en Supabase el 20 de noviembre de 2026. Reiniciar el backend para cargar los nuevos endpoints.

La reserva pendiente no ocupa capacidad adicional a su apartado. R05 deberá incorporar la ocupación confirmada a disponibilidad, validar el pago completo y resolver pagos tardíos antes de confirmar. Este bloque no crea pagos ni ofrece una acción para marcar una reserva pagada. R04.2 agregará la captura por Recepción para un cliente registrado, con actor separado y sin anticipación mínima web.

## Verificación

- 11 casos HTTP de R04.1: creación, copia de precios, privacidad, sesión, reintentos, correspondencia de líneas, edades/tutor, vencimiento y cambios de disponibilidad/paquete.
- Prueba PostgreSQL en esquema temporal: migración, RLS, persistencia de copias, decimales, consumibles e integridad de importes.
- Suite local: 262 aprobadas y 37 pruebas PostgreSQL omitidas por configuración; se excluyeron pruebas de concurrencia por indicación del usuario.
- Las pruebas PostgreSQL específicas se ejecutan por separado; no se afirma una ejecución completa de la suite PostgreSQL.
