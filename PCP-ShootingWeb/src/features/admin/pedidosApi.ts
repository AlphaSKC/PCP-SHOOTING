/*
 * Pedidos de la tienda en el panel · API real (VITE_DATA_SOURCE=api). Administrador y Recepción.
 *   Lista       → GET  /api/administracion/pedidos?estado&desdeUtc&hastaUtc
 *   Detalle     → GET  /api/administracion/pedidos/{id}     (partidas + pagos)
 *   Clientes    → nombre y teléfono vienen en cada pedido (F07; también para Recepción)
 *   Productos   → GET  /api/administracion/productos        (nombres de las partidas)
 *   Pago manual → POST /api/pedidos/{id}/pagos { idOperacion, referenciaExterna } → pago Pendiente
 *                 POST /api/pagos/{pagoId}/confirmacion → pedido Pagado y salida de inventario
 *   Recogida    → POST /api/pedidos/{id}/recogida          (requiere pago completo confirmado)
 *   Cancelar    → POST /api/pedidos/{id}/cancelacion       (solo pendientes de pago)
 * Estados de la API: 0 PendientePago, 1 Pagado, 2 Recogido, 3 Cancelado.
 * "Preparando" y "Listo para recoger" del diseño NO existen en la API.
 */
import { ApiError, apiGet, apiGetAll, apiPost, type WriteResult } from '@/lib/api';
import { folioOf, mapOrderStatus } from '@/features/tienda/api';
import type { OrderStatus } from '@/features/tienda/types';

type ApiAdminOrder = {
  id: string;
  usuarioId: string;
  creadoEnUtc: string;
  estado: number;
  total: number;
  /** F07: datos actuales del perfil (pueden ser null). Visibles para Administrador y Recepción. */
  nombreCliente?: string | null;
  telefonoCliente?: string | null;
};
type ApiPayment = {
  id: string;
  pedidoId: string;
  importe: number;
  referenciaExterna: string | null;
  estado: number;
  confirmadoEnUtc: string | null;
};
type ApiOrderDetail = {
  pedido: ApiAdminOrder;
  detalles: { productoId: string; cantidad: number; precioUnitario: number; talla?: string | null }[];
  pagos: ApiPayment[];
};
type ApiProfile = { id: string; nombre: string; telefono: string | null };

export type Customer = { name: string; phone: string };

export type AdminOrderApi = {
  id: string;
  folio: string;
  customerId: string;
  /** Nombre y teléfono del cliente (de la API; null si el perfil no existe o no tiene teléfono). */
  customerName: string | null;
  customerPhone: string | null;
  createdAt: string;
  status: OrderStatus;
  total: number;
};

export type PaymentStatus = 'pendiente' | 'confirmado' | 'rechazado' | 'cancelado';
const PAYMENT_STATUS: PaymentStatus[] = ['pendiente', 'confirmado', 'rechazado', 'cancelado'];

export type AdminOrderDetailApi = AdminOrderApi & {
  lines: { productId: string; name: string; size: string | null; quantity: number; price: number }[];
  payments: { id: string; amount: number; reference: string; status: PaymentStatus; confirmedAt: string | null }[];
};

const toOrder = (o: ApiAdminOrder): AdminOrderApi => ({
  id: o.id,
  folio: folioOf(o.id),
  customerId: o.usuarioId,
  customerName: o.nombreCliente ?? null,
  customerPhone: o.telefonoCliente ?? null,
  createdAt: o.creadoEnUtc,
  status: mapOrderStatus(o.estado),
  total: o.total,
});

export async function listOrders(): Promise<AdminOrderApi[]> {
  const list = await apiGetAll<ApiAdminOrder>('/api/administracion/pedidos');
  return list.map(toOrder).sort((a, b) => b.createdAt.localeCompare(a.createdAt));
}

/** Nombre y teléfono de los clientes. Recepción no tiene permiso (403): se devuelve un mapa vacío. */
export async function listCustomers(): Promise<Map<string, Customer>> {
  try {
    const list = await apiGetAll<ApiProfile>('/api/perfiles');
    return new Map(list.map((p) => [p.id, { name: p.nombre, phone: p.telefono ?? '' }]));
  } catch (e) {
    if (e instanceof ApiError && e.status === 403) return new Map();
    throw e;
  }
}

/** Nombres de productos (incluye inactivos) para mostrar las partidas. */
export async function listProductNames(): Promise<Map<string, string>> {
  const list = await apiGetAll<{ id: string; nombre: string }>('/api/administracion/productos');
  return new Map(list.map((p) => [p.id, p.nombre]));
}

export async function getOrderDetail(id: string, names: Map<string, string>): Promise<AdminOrderDetailApi> {
  const d = await apiGet<ApiOrderDetail>(`/api/administracion/pedidos/${id}`);
  return {
    ...toOrder(d.pedido),
    lines: d.detalles.map((l) => ({
      productId: l.productoId,
      name: names.get(l.productoId) ?? 'Producto',
      size: l.talla ?? null,
      quantity: l.cantidad,
      price: l.precioUnitario,
    })),
    payments: d.pagos.map((p) => ({
      id: p.id,
      amount: p.importe,
      reference: p.referenciaExterna ?? '',
      status: PAYMENT_STATUS[p.estado] ?? 'pendiente',
      confirmedAt: p.confirmadoEnUtc,
    })),
  };
}

/**
 * Registra el pago manual (si no hay uno pendiente) y lo confirma.
 * `operationId` y la referencia se conservan entre reintentos: la API no duplica el pago.
 */
export async function registerAndConfirmPayment(order: AdminOrderDetailApi, reference: string, operationId: string): Promise<void> {
  const pending = order.payments.find((p) => p.status === 'pendiente');
  const paymentId = pending
    ? pending.id
    : (await apiPost<WriteResult>(`/api/pedidos/${order.id}/pagos`, { idOperacion: operationId, referenciaExterna: reference.trim() })).id;
  await apiPost<WriteResult>(`/api/pagos/${paymentId}/confirmacion`);
}

export const markPickedUp = (id: string) => apiPost<WriteResult>(`/api/pedidos/${id}/recogida`);
export const cancelOrderAdmin = (id: string) => apiPost<WriteResult>(`/api/pedidos/${id}/cancelacion`);
