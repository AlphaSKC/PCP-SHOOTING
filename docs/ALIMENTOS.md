# Carta pública de alimentos y bebidas

Backend de catálogo disponible el 24 nov 2026. Usa los productos existentes de categoría `Alimento` (valor 1), su precio, imagen, notas e inventario. No crea otro catálogo ni copia existencias. Alimentos y bebidas comparten esa categoría; no se añadieron subcategorías, horarios de cocina ni productos de muestra.

La carta está disponible. A01 añade la [base de órdenes de alimentos](ORDENES-ALIMENTOS.md), con preparación, registro manual del cobro local y entrega con inventario; permanece desactivada para producción hasta cerrar las reglas operativas de OQ-05. La conexión del frontend queda a cargo de Thiago.

## Rutas públicas

| Método y ruta | Resultado |
| --- | --- |
| GET `/api/alimentos` | Carta paginada de alimentos activos y vendibles. |
| GET `/api/alimentos/{sku}` | Detalle por SKU exacto, con precio y existencias actuales. |

No requieren sesión. Detalle devuelve 404 si el SKU no existe, está inactivo, no es vendible o pertenece a otra categoría. Los alimentos sin existencias siguen visibles con `agotado=true`; ocultar un producto se realiza desactivándolo en la gestión existente. No hay rutas de escritura bajo `/api/alimentos`: A01 utiliza `/api/ordenes-alimentos` y `/api/administracion/ordenes-alimentos`, sin interferir con los SKU de la carta.

Filtros del listado:

- `pagina`: desde 1; predeterminado 1.
- `tamanoPagina`: de 1 a 100; predeterminado 20.
- `busqueda`: hasta 150 caracteres; busca en SKU, nombre y descripción sin distinguir mayúsculas.
- `precioMinimo` y `precioMaximo`: inclusivos, opcionales, no negativos; mínimo no puede superar máximo.

No se permite seleccionar otra categoría ni incluir ocultos a través de este contrato. Orden por nombre, SKU y UUID ascendentes. Respuesta `{ elementos, total, pagina, tamanoPagina }`; total corresponde a todos los resultados filtrados. Carta vacía y página fuera del conjunto devuelven 200 con lista vacía. Los filtros inválidos devuelven 400.

Cada elemento y el detalle comparten esta forma (ejemplo ilustrativo):

```json
{
  "id": "bc292a54-df06-4cd3-8d37-bf6733dcf253",
  "sku": "ALI-AGUA",
  "nombre": "Agua",
  "descripcion": "Agua embotellada",
  "precio": 25.00,
  "imagenUrl": "https://example.com/agua.webp",
  "notasMarkdown": "- Botella de **500 ml**",
  "existencia": 3,
  "agotado": false
}
```

Precio procede del producto; el catálogo no aplica ni define tarifas nuevas. Imagen, descripción y notas son opcionales y pueden ser nulas. La imagen es la portada del producto existente: no se añade un bucket ni mecanismo de carga nuevo. Frontend debe renderizar Markdown sin HTML crudo y mostrar las imágenes como recursos visuales.

Las existencias se calculan desde los movimientos de inventario actuales; ausencia de movimientos equivale a cero. Agotado significa saldo menor o igual a cero. Consultar no aparta ni descuenta stock y no garantiza existencias al intentar una compra posterior. Las respuestas llevan `Cache-Control: no-store`; una pantalla abierta debe volver a consultar para reflejar cambios.

El DTO público omite mínimos de inventario, auditoría, datos de empleados y configuración administrativa. El catálogo general `/api/productos?categoria=1&esVendible=true` sigue funcionando; `/api/alimentos` ofrece un contrato fijo de carta, y su detalle incluye precio/stock sin depender de una segunda consulta del listado.

## Gestión existente

Se conservan rutas y permisos actuales, sin duplicar servicios de escritura:

- Crear/editar: POST `/api/productos`, PUT `/api/productos/{id}`, enviando `categoria=1`.
- Listar gestión: GET `/api/administracion/productos?categoria=1`.
- Mostrar/ocultar: PATCH `/api/productos/{id}/estado` con `activo`.
- Inventario: GET `/api/inventario/{id}/existencia`, POST `/api/inventario/{id}/movimientos` y consultas existentes.
- Imágenes: carga existente de productos según [IMAGENES-API.md](IMAGENES-API.md).

Administrador conserva sus permisos y Recepción puede gestionar alimentos e inventariarlos según las restricciones vigentes; no obtiene acceso a otras categorías. La carta refleja esos cambios al volver a consultarla. Las consultas públicas no conceden acceso a escritura.

## Estado y dependencias

Sin migración: se reutilizan tablas y datos existentes. Pruebas HTTP cubren aislamiento de categorías/ocultos, precio/stock, búsqueda, filtros, paginación y gestión por Recepción; comprobación PostgreSQL cubre consultas SQL y cambios de stock/visibilidad a través de la API existente. Concurrencia omitida por indicación del usuario.

La carta y su gestión fueron conectadas por Thiago según el roadmap. A01 ya implementa la base de estados de cocina, registro manual del cobro y entrega, con migración aplicada; falta publicar la API e integrar esas rutas. Las escrituras nuevas siguen desactivadas para producción. OQ-05 continúa abierta para pago online, protección de stock durante preparación, permisos de barra, faltantes/no recogidos, horarios/subcategorías y demás decisiones operativas. El reporte F15 mantiene su alcance de partidas en pedidos generales pagados/recogidos; todavía no incorpora las órdenes nuevas.
