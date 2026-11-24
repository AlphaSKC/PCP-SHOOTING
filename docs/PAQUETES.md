# Paquetes y consumibles · R02

Este módulo administra paquetes vendidos por actividad y por persona. No agrega duración comercial al paquete: el tiempo de ocupación se obtiene de la configuración operativa. El catálogo real, sus precios, beneficios CARTAQ y consumibles exactos siguen pendientes del cliente. No se insertan paquetes de ejemplo en producción.

## Endpoints y permisos

| Método | Ruta | Acceso |
| --- | --- | --- |
| GET | `/api/paquetes` | Público, solo paquetes visibles. |
| GET | `/api/paquetes/{id}` | Público, detalle de paquete visible; 404 para ocultos/inexistentes. |
| GET | `/api/administracion/paquetes` | Administrador y Recepción activos. |
| GET | `/api/administracion/paquetes/{id}` | Administrador y Recepción activos, incluye composición y versión. |
| POST | `/api/administracion/paquetes` | Solo Administrador; crea y devuelve 201 con Location. |
| PUT | `/api/administracion/paquetes/{id}` | Solo Administrador; sustituye datos y composición, o desactiva. |

Las rutas administrativas requieren `Authorization: Bearer <access_token>`. Clientes y perfiles inactivos no pueden consultarlas. Recepción puede consultar paquetes para operar reservas, pero no modificar el catálogo ni gestionar inventario de consumibles.

Ambos listados aceptan `busqueda` (código/nombre/descripción, hasta 150 caracteres), `pagina` y `tamanoPagina` (predeterminados 1 y 20, máximo 100). El administrativo admite además `activo=true/false`; omitirlo incluye ambos. La respuesta es `{ elementos, total, pagina, tamanoPagina }`.

## Consumibles internos en inventario

Se agrega `CategoriaProducto.Consumible = 4`, conservando los valores anteriores: 0 sin clasificar, 1 alimento, 2 playera y 3 decorativo no funcional.

Los consumibles son productos internos, **no vendibles**, y usan los endpoints existentes de productos e inventario. Solo Administrador los gestiona. No aparecen en el catálogo público de productos ni en su consulta por SKU. La base de datos también impide `categoria=4` junto con `esVendible=true`.

```json
{
  "sku": "MATERIAL-INTERNO-001",
  "nombre": "Material para actividad",
  "categoria": 4,
  "esVendible": false,
  "precio": null
}
```

Enviar ese cuerpo a POST `/api/productos` y conservar el `id` devuelto. Sus entradas/salidas, existencia y mínimos se administran con `/api/inventario`; para consultar el conjunto, usar `GET /api/inventario?categoria=4` con sesión de Administrador.

Si un consumible está vinculado a un paquete, no se puede cambiar a otra categoría sin retirar primero esas referencias; la API devuelve 409. Desactivarlo sí está permitido y oculta del catálogo público los paquetes que dependan de él. Eso no sustituye las validaciones de disponibilidad y reservas de R03/R04.

## Crear y editar un paquete

```json
{
  "codigo": "PK-001",
  "nombre": "Actividad del club",
  "descripcion": "Descripción pendiente de contenido definitivo.",
  "incluyeMarkdown": "- Material incluido\n- Acompañamiento del personal",
  "requisitosMarkdown": "- Responsiva por participante",
  "precioPorPersona": null,
  "precioSocioCartaq": null,
  "activo": false,
  "consumibles": [
    {
      "productoId": "REEMPLAZAR-POR-ID-DEL-CONSUMIBLE",
      "cantidadPorPersona": 2
    }
  ]
}
```

Los placeholders del ejemplo deben sustituirse por UUID válidos. PUT recibe el mismo cuerpo completo y un campo `version` copiado del último GET administrativo o escritura. Una versión ausente/vacía produce 400, una antigua 409. Cada escritura genera una versión nueva y auditoría para el paquete y los cambios de su composición. Una edición concurrente no puede guardar solo parte de la composición. En PostgreSQL, la lectura y validación de consumibles y el guardado del paquete se ejecutan en una transacción serializable con reintentos: tampoco puede quedar una referencia inválida si se cambia simultáneamente la categoría del producto.

Reglas:

- Código obligatorio, único incluso en paquetes desactivados, hasta 80 caracteres. Admite letras ASCII, números y guiones simples entre palabras; se guarda en mayúsculas.
- Nombre obligatorio, hasta 200 caracteres. Descripción opcional, hasta 4000. Incluye Markdown obligatorio y requisitos Markdown opcionales, hasta 10000 caracteres cada uno. El frontend debe renderizar Markdown de forma segura, sin insertar HTML sin sanitizar.
- Los precios se almacenan como `numeric(12,2)`: máximo `9999999999.99`, no negativos y hasta dos decimales. `null` indica pendiente, distinto de cero.
- Paquetes inactivos admiten precios pendientes o cero durante desarrollo/configuración. Para activar, el precio general debe ser positivo y el precio CARTAQ también cuando se especifique. No se interpreta cero como una reserva gratuita.
- `precioSocioCartaq=null` significa que no hay tarifa especial configurada; R04 deberá usar la general. La aplicación efectiva de tarifa especial requiere comprobar membresía vigente en el servidor; no se decide por un parámetro enviado desde React. R02 guarda la tarifa, no calcula ni cobra una reserva.
- Consumibles: hasta 100 referencias únicas a productos de categoría 4, cantidad entera entre 1 y 100000 por persona. No se aceptan productos de otras categorías. Para activar el paquete, los consumibles relacionados deben estar activos.
- Se permite una composición vacía para preparar el catálogo o para un paquete que no utilice consumibles. Administrador debe completarla según las definiciones del cliente antes de publicar el catálogo real; la API no inventa cantidades.
- PUT reemplaza la composición completa: omitir un producto de la lista lo retira del paquete. Una lista vacía retira todos. No se borra el producto ni su historial de inventario.
- Para desactivar, enviar PUT con `activo=false`, la composición y la versión actual. No hay borrado físico de paquetes.

## Catálogo público y datos internos

Un paquete es visible cuando está activo, tiene precios de publicación válidos y todos sus consumibles siguen activos, son internos y no vendibles. El detalle público expone código, nombre, descripción, lo que incluye, requisitos y precios. No expone referencias de inventario, SKU, cantidades internas, versiones ni fechas de administración.

La gestión devuelve además `activo`, `visibleEnCatalogo`, fechas, `version` y `consumibles` con producto ID, SKU, nombre, cantidad por persona y estado. Un paquete puede tener `activo=true` y `visibleEnCatalogo=false` si después se desactiva un componente. `visibleEnCatalogo` **no representa stock disponible ni un horario reservable**.

Guardar/editar/publicar un paquete no genera movimientos de inventario. R06 descontará consumibles una sola vez al registrar la llegada. R03/R04 deberán validar disponibilidad y conservar una copia de precio y composición al reservar para que una edición posterior no cambie lo contratado.

## Migración e integración

`PaquetesYConsumibles` crea `Paquetes` y `ConsumiblesPaquete`, claves foráneas restrictivas, índices, precisión decimal, validaciones y control de concurrencia. Amplía la categoría de productos para el tipo interno y agrega la restricción de no venta. Las tablas nuevas tienen RLS y permisos revocados para `anon` y `authenticated`: el acceso pasa por la API.

No cambia los estados de pedidos o pagos. Las pruebas verifican creación, sustitución de componentes, ausencia de descuentos prematuros, publicación, permisos, precisión, auditoría, concurrencia y ejecución de la migración en PostgreSQL temporal.

El frontend sigue con su demostración: mapear `nombre/descripcion/incluyeMarkdown/precioPorPersona/precioSocioCartaq/activo/consumibles` y conservar la versión al editar. Los precios pendientes se muestran como pendientes, no como cero. No habilitar todavía un checkout real: reservas, apartados y Mercado Pago corresponden a R03–R05.
