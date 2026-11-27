# Definiciones del cliente · cuestionario Parte 2 · 27 nov 2026

Fuente: preguntas de `Cuestionario_Definiciones_PCP_Shooting_Parte2.pdf` (cinco páginas, fechado 24 nov) y respuestas numeradas compartidas por Kevin el 27 nov. El PDF contiene las preguntas; las respuestas provienen del mensaje, no de casillas marcadas en el archivo. Esta incorporación documenta reglas, no constituye implementación, publicación ni prueba de los módulos.

## CARTAQ · OQ-02 / F12

- 1.1: un solo plan, inscripción única y cuota/vigencia anual. Alta desde el pago de la anualidad. **No se proporcionaron importes** ni se confirmó que «inscripción única» implique una cuota adicional a la anualidad.
- 1.2: beneficios referidos al «documento intro» (no adjunto en esta entrega); carga de aire gratuita para socio, invitado paga $250 y consumo duplicado/triplicado tiene costo. La pregunta trata beneficios: **no trasladar $250 al precio de la tirada ni a la cuota CARTAQ**. Falta unidad/alcance del cobro al invitado, consumo incluido y tarifas del exceso.
- 1.3: beneficios solo del titular, no de acompañantes.
- 1.4: Recepción verifica al llegar que pertenezca a CARTAQ, registra el equipo propio en la aplicación y verifica en cada visita que corresponda al registrado y que dispare con menos de 140 julios. Es un requisito nuevo de registro/verificación; no una venta de equipo ni una validación técnica automatizada ya implementada. Campos del registro y evidencia de revisión por acordar.
- 1.5: al renovar **antes del vencimiento** (alcance de la pregunta), la nueva vigencia comienza al terminar la actual. No fijar otro inicio por pago anticipado. Renovación después del vencimiento aún debe concretarse.

Cuota anual define periodicidad y duración; no confirma por sí sola un cargo automático recurrente. La elección técnica de recurrencia sigue en el equipo, como indicó Kevin anteriormente; no volver a pedir al cliente elegir un proveedor o una arquitectura.

Puede diseñarse el plan único y su compra/renovación con precios configurables, sin habilitar contratación con importes inventados o cero por defecto. Falta importe de anualidad, posible inscripción, documento de beneficios y detalle de consumos.

## Responsivas · OQ-03 / R06

- 2.1: texto oficial se enviará después; continúa pendiente.
- 2.2: se requiere **casilla Acepto y firma dibujada en pantalla**.
- 2.3: cada adulto firma su propia responsiva.
- 2.4: menores: nombre del tutor, nombre y edad del menor y firma del tutor. No se añadieron identificación oficial o parentesco como requisitos confirmados.
- 2.5: respondieron «por cada visita», pero la pregunta es **cuánto tiempo conservar el documento** (1/3/5 años u otro). No resuelve retención. No borrar responsivas al finalizar una visita.
- 2.6: Administrador, Recepción y el propio cliente pueden consultar/descargar. «Todos» se refiere a esos actores: **no es acceso público ni permite al cliente consultar documentos ajenos**. En reservas grupales falta concretar qué firmantes/documentos puede ver el organizador.
- 2.7: Recepción revisa la responsiva firmada antes de admitir.
- 2.8: se firma en cada reserva; no hay responsiva permanente reutilizable.

Puede prepararse documento versionado, aceptación, firma, relación con reserva/participante/tutor y revisión de Recepción. No habilitar admisión sin el texto oficial y evidencia válida. Retención y acceso del organizador de grupos quedan por precisar. La captura de firmas del frontend pertenece a Thiago.

## Eventos · OQ-04

- 3.1: competencias, tiradas, eventos y similares; taxonomía concreta configurable, no inventar categorías comerciales como definitivas.
- 3.2: mostrar toda la información preguntada: nombre, fechas/horario, descripción, foto, precio, cupo disponible, requisitos y categorías.
- 3.3: capacidad por persona.
- 3.4: edades y condiciones para menores pendientes de definir; no reutilizar automáticamente la edad mínima de reservas ordinarias.
- 3.5: todos con pago; no habilitar eventos gratuitos por defecto.
- 3.6: una persona puede inscribir a varios; límite por operación/grupo no indicado.
- 3.7: sí se aparta cupo temporalmente durante el proceso de pago.
- 3.8: prefieren menos de 10 minutos, pero **no está decidido**. Parámetro configurable sin afirmar un nuevo valor aprobado; no copiar 10 minutos como regla definitiva de eventos.
- 3.9: algunos eventos bloquean algunas líneas; selección y relación operativa todavía pendientes.
- 3.10: asistencia mediante QR, distinto del QR/enlace de pago.
- 3.11: cancelación por el organizador: pase a otro evento. Reprogramación: inscripción se mueve a la nueva fecha. Falta cómo elegir destino, diferencias de precio/cupo y autorizar/registrar el traslado. No crear un saldo o reembolso automático no solicitado.

Se desbloquea el diseño de cupo por persona, inscripciones múltiples, apartados y pagos. Faltan parámetros/operación para cerrar el módulo real y su reporte. El catálogo informativo ya existente se conserva. La excepción por cancelación del organizador no autoriza cancelación ordinaria de reservas ni resuelve F06/OQ-01.

## Alimentos y bebidas · OQ-05

- 4.1: pago en línea y en el local.
- 4.2: en el local aceptan tarjeta y transferencia; **no se confirmó efectivo**.
- 4.3: se prepara sin exigir pago previo; en la modalidad local se cobra al entregar. El cobro en línea debe convivir con esta modalidad, sin pedir un segundo pago si ya fue confirmado.
- 4.4: bebidas, snacks y comida.
- 4.5: respuesta «no todo el horario». La opción literal del PDF es **«No, todo el horario del club»**. Interpretación provisional: no hay horario especial y se usa el del club; confirmar la puntuación antes de restringir horarios. No asumir que exige horario reducido.
- 4.6: se puede comprar sin reserva.
- 4.7: entrega/recogida en Recepción; no se confirmó reparto en la línea.
- 4.8: flujo Recibido → En preparación → Listo → Entregado aprobado.
- 4.9: Recepción y barra actualizan estados. Los roles actuales son Cliente/Recepción/Administrador: falta decidir si barra usará los permisos de Recepción o necesita un rol limitado. No conceder automáticamente todos los permisos de Recepción a barra ni crear el rol sin acordar su alcance.
- 4.10: inventario se descuenta **al entregar**, una sola vez, no al pagar ni al iniciar preparación.
- 4.11 caso 1: si se agota, se modifica desde Administración y se marca no disponible. Falta el tratamiento del pedido ya aceptado/pagado si luego falta el producto; ocultarlo en la carta no resuelve esa orden.
- 4.11 casos 2/3 y comentario: si no recoge o pide cancelar, no hay reembolso; el cliente solicita mostrar claramente su política de no cancelaciones, reembolsos ni devoluciones. Para pedido no pagado no existe importe a devolver; no convertirlo en cobro automático no autorizado. Faltan cierre del pedido no recogido y tratamiento de inventario preparado/no entregado.

El flujo operativo requiere separar **estado de preparación, estado de pago y entrega**. La tienda actual descuenta al confirmar pago: no debe reutilizarse esa transición sin adaptación para alimentos, porque contradice 4.10. Confirmar un pago online no entrega automáticamente ni descuenta stock. Debe evitarse sobreventa de pedidos aceptados sin descontar físicamente antes de entrega; falta definir la protección de existencias mientras se preparan.

Puede iniciarse el diseño de órdenes de alimentos y sus estados, modalidades de pago, subcategorías y entrega en Recepción, manteniendo abiertos los puntos indicados. Los combos asociados a reservas no quedan incorporados por estas respuestas; su alcance sigue siendo el indicado por frontend en el roadmap.

## Mercado Pago · R05

Kevin informa que el cliente entregó correo y contraseña de la cuenta. No se recibieron ni guardaron esos secretos en esta documentación. Esto cambia la dependencia de acceso a cuenta, pero **no acredita credenciales API configuradas, webhook publicado o pruebas sandbox aprobadas**.

La API actual usa `MercadoPago:AccessToken` y `MercadoPago:WebhookSecret`; revisar URL pública de notificaciones/retornos y entorno antes de probar. Obtener las credenciales desde la aplicación de Mercado Pago, configurarlas como secretos del servidor/Azure y probar el flujo de tienda/reservas existente. No usar correo/contraseña como sustituto del Access Token, ni guardarlos en Git. Referencias oficiales: [credenciales](https://www.mercadopago.com.mx/developers/es/docs/your-integrations/credentials), [webhooks](https://www.mercadopago.com.mx/developers/es/docs/checkout-pro-orders/resources/notifications/webhooks).

## Orden sugerido, sin cambiar prioridades del frontend automáticamente

1. Configurar/verificar R05 y publicar los endpoints ya desarrollados pendientes de despliegue.
2. Completar solicitudes de frontend definidas; **F24 conserva la prioridad expresa del cliente** registrada el 27 nov. F19/F20/F21/F22/F23 siguen en el roadmap y no se consideran resueltos con este cuestionario.
3. Diseñar órdenes de alimentos con las reglas confirmadas y las dudas operativas indicadas.
4. Diseñar CARTAQ y responsivas en paralelo con la entrega de precios, beneficios y texto oficial.
5. Completar inscripciones/eventos cuando se concreten los parámetros faltantes; entonces asistencia e informes correspondientes.

Esta incorporación solo actualiza documentación. Cada desarrollo mantendrá backend/frontend separados según el acuerdo con Kevin y actualizará contratos, migraciones y verificaciones al cerrar.
