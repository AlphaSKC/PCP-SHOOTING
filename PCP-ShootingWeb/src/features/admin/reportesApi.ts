/*
 * Reportes · API real (solo Administrador). docs/REPORTES.md
 *   GET /api/reportes/pedidos     ?estado&desdeUtc&hastaUtc        (fecha de creación)
 *   GET /api/reportes/pagos       ?estado&desdeUtc&hastaUtc        (fecha de confirmación)
 *   GET /api/reportes/membresias  ?desde&hasta | vigenteEn          (inicio de vigencia, fechas locales)
 *   GET /api/reportes/inventario  ?categoria&bajoMinimo…           (existencias actuales)
 * Todos: formato=json (paginado, { datos, importeConfirmado }) o formato=csv (hasta 10 000 filas).
 * Rango: "desde" inclusivo, "hasta" exclusivo. Fechas del panel = días locales del club (UTC−5).
 * Reservas, alimentos y eventos aún no tienen reporte en la API.
 */
import { apiDownload, apiGet, type Page, type Query } from '@/lib/api';

type Report<T> = { datos: Page<T>; importeConfirmado: number | null };

export type ReportKind = 'pedidos' | 'pagos' | 'membresias' | 'inventario';

/** Recorre todas las páginas de un reporte (máx. 5 000 filas en pantalla). */
async function fetchAll<T>(kind: ReportKind, query: Query): Promise<{ rows: T[]; total: number; confirmed: number | null }> {
  const rows: T[] = [];
  let total = 0;
  let confirmed: number | null = null;
  for (let pagina = 1; pagina <= 50; pagina++) {
    const r = await apiGet<Report<T>>(`/api/reportes/${kind}`, { ...query, formato: 'json', pagina, tamanoPagina: 100 });
    rows.push(...r.datos.elementos);
    total = r.datos.total;
    confirmed = r.importeConfirmado;
    if (rows.length >= total || r.datos.elementos.length === 0) break;
  }
  return { rows, total, confirmed };
}

/** Día local del club → instante UTC (inicio del día). */
const dayStartUtc = (date: string) => new Date(`${date}T00:00:00-05:00`).toISOString();
const nextDay = (date: string) => {
  const d = new Date(`${date}T12:00:00Z`);
  d.setUTCDate(d.getUTCDate() + 1);
  return d.toISOString().slice(0, 10);
};
/** Rango inclusivo [desde, hasta] en días locales → [desdeUtc, hastaUtc) que pide la API. */
export const utcRange = (from: string, to: string) => ({ desdeUtc: dayStartUtc(from), hastaUtc: dayStartUtc(nextDay(to)) });
export const dateRange = (from: string, to: string) => ({ desde: from, hasta: nextDay(to) });

/** Día local del club de un instante UTC (YYYY-MM-DD). */
export const clubDay = (isoUtc: string) =>
  new Intl.DateTimeFormat('en-CA', { timeZone: 'America/Cancun', year: 'numeric', month: '2-digit', day: '2-digit' }).format(
    new Date(/Z$|[+-]\d\d:\d\d$/.test(isoUtc) ? isoUtc : `${isoUtc}Z`),
  );

export type OrderRow = { id: string; usuarioId: string; creadoEnUtc: string; estado: number; total: number };
export type PaymentRow = { id: string; pedidoId: string; usuarioId: string; importe: number; referenciaExterna: string | null; estado: number; confirmadoEnUtc: string | null };
export type MembershipRow = { id: string; usuarioId: string; pagoId: string; inicioVigencia: string; finVigenciaExclusivo: string; estadoPago: number; importe: number };
export type StockRow = { productoId: string; sku: string; nombre: string; categoria: number; activo: boolean; esVendible: boolean; existencia: number; stockMinimo: number | null; bajoMinimo: boolean };

export const ordersReport = (from: string, to: string) => fetchAll<OrderRow>('pedidos', utcRange(from, to));
export const paymentsReport = (from: string, to: string) => fetchAll<PaymentRow>('pagos', { ...utcRange(from, to), estado: 1 });
export const membershipsReport = (from: string, to: string) => fetchAll<MembershipRow>('membresias', dateRange(from, to));
export const activeMembershipsCount = async (today: string) =>
  (await apiGet<Report<MembershipRow>>('/api/reportes/membresias', { vigenteEn: today, formato: 'json', pagina: 1, tamanoPagina: 1 })).datos.total;
export const stockReport = () => fetchAll<StockRow>('inventario', {});

/** CSV generado por la API (UTF-8, con todas las filas del filtro). */
export function downloadReportCsv(kind: ReportKind, from: string, to: string) {
  const query: Query =
    kind === 'pedidos' ? utcRange(from, to) : kind === 'pagos' ? { ...utcRange(from, to), estado: 1 } : kind === 'membresias' ? dateRange(from, to) : {};
  const suffix = kind === 'inventario' ? 'actual' : `${from}_a_${to}`;
  return apiDownload(`/api/reportes/${kind}`, { ...query, formato: 'csv' }, `reporte-${kind}-${suffix}.csv`);
}

/** Suma por día local para la gráfica (rellena los días sin movimiento). */
export function dailySeries<T>(rows: T[], from: string, to: string, dayOf: (r: T) => string | null, valueOf: (r: T) => number) {
  const sums = new Map<string, number>();
  for (const r of rows) {
    const d = dayOf(r);
    if (d) sums.set(d, (sums.get(d) ?? 0) + valueOf(r));
  }
  const out: { date: string; value: number }[] = [];
  for (let d = from; d <= to && out.length < 400; d = nextDay(d)) out.push({ date: d, value: sums.get(d) ?? 0 });
  return out;
}
