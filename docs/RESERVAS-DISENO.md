# R04 · Diseño de reservaciones

R04.1 (web) implementado: [contrato y límites actuales](RESERVAS-WEB.md). R04.2 (mostrador) pendiente. La confirmación por pago corresponde a R05.

## Resultado previsto

- Reserva separada del apartado, vinculada al usuario, paquete, fecha, hora y líneas elegidas.
- Creación web desde un apartado vigente del propio usuario; operación idempotente.
- Participantes con nombre, nacimiento y relación con su tutor cuando corresponda. Edad mínima de nueve años a la fecha local de la actividad. Firma y validación de responsivas corresponden a R06.
- Guardar copia de precios, descripción del paquete, consumibles por persona y tiempos de ocupación; una edición posterior del catálogo no cambia lo contratado.
- Estado inicial pendiente de pago y plazo del apartado original, sin ampliarlo al crear la reserva. No confirmar ni conservar capacidad indefinidamente por crear un registro sin pago.
- Consultas propias para Cliente; operación de mostrador e historial operativo para Recepción/Administrador.
- Mostrador permite comenzar sin la anticipación mínima web y valida la ocupación completa, incluido margen. El cliente seleccionado y quien captura son identidades distintas.
- No se registra efectivo ni terminal externa en mostrador. La confirmación y el enlace/QR de Mercado Pago pertenecen a R05; no agregar una acción pública para marcar pagada la reserva.
- Sin cancelación o reprogramación por el cliente en el flujo ordinario. Las incidencias de cobro requieren el tratamiento posterior de R05.

## Registro obligatorio confirmado

Toda reserva requiere una cuenta del cliente, incluida la de mostrador. El cliente puede registrarse desde su dispositivo o con ayuda de Recepción usando el flujo existente de Supabase Auth. Después se crea/consulta su perfil y Recepción lo selecciona para capturar la reserva.

La asistencia no equivale a reservar como invitado ni a usar la cuenta del recepcionista. Se conservan los requisitos de confirmación de correo y perfil activo existentes. No crear cuentas ficticias, omitir la activación ni almacenar contraseñas en el backend.

Quien captura y el propietario de la reserva son identidades distintas. El backend debe validar el perfil del cliente seleccionado y registrar al empleado que realizó la operación.

## Validación y entrega

Modelos, configuración EF, servicios, controllers, migración verificada y documentación de contratos. Mantener la omisión de pruebas de concurrencia solicitada para el cierre reciente, indicando el alcance exacto de la verificación. Actualizar el roadmap y proporcionar commit en inglés.

La regla de anticipación máxima continúa provisionalmente en 48 horas. El alcance de tarifas CARTAQ por participante debe definirse antes de aplicar descuentos a terceros; no asumir que la membresía de quien reserva beneficia a todo el grupo.
