import type { ReactNode } from 'react';
import { cx } from '@/lib/format';
import styles from './StatusBadge.module.css';

export type BadgeTone = 'info' | 'warning' | 'success' | 'neutral' | 'danger' | 'gold';

/**
 * Estados conocidos de la plataforma → texto y color (CLAUDE.md §4 y §6).
 * Las claves siguen el formato de la API (snake_case).
 */
export const STATUS = {
  // Reservas
  pendiente_pago: { label: 'Pendiente de pago', tone: 'danger' },
  confirmada: { label: 'Confirmada', tone: 'info' },
  por_llegar: { label: 'Por llegar', tone: 'info' },
  llego: { label: 'Llegó', tone: 'success' },
  utilizada: { label: 'Utilizada', tone: 'neutral' },
  cancelada: { label: 'Cancelada', tone: 'neutral' },
  // Pedidos de tienda
  pagado: { label: 'Pagado', tone: 'info' },
  preparando: { label: 'Preparando', tone: 'warning' },
  listo_para_recoger: { label: 'Listo para recoger', tone: 'success' },
  entregado: { label: 'Entregado', tone: 'neutral' },
  // Alimentos y bebidas
  recibido: { label: 'Recibido', tone: 'info' },
  en_preparacion: { label: 'En preparación', tone: 'warning' },
  listo: { label: 'Listo', tone: 'success' },
  // Membresías, cartas, contenido
  activo: { label: 'Activo', tone: 'success' },
  vencida: { label: 'Vencida', tone: 'danger' },
  firmada: { label: 'Firmada', tone: 'success' },
  pendiente: { label: 'Pendiente', tone: 'danger' },
  borrador: { label: 'Borrador', tone: 'neutral' },
  publicado: { label: 'Publicado', tone: 'success' },
  programado: { label: 'Programado', tone: 'info' },
  pausado: { label: 'Pausado', tone: 'neutral' },
  por_vencer: { label: 'Por vencer', tone: 'warning' },
  agotado: { label: 'Agotado', tone: 'danger' },
  pocas_piezas: { label: 'Pocas piezas', tone: 'warning' },
  bajo_minimo: { label: 'Bajo mínimo', tone: 'danger' },
} as const satisfies Record<string, { label: string; tone: BadgeTone }>;

export type StatusKey = keyof typeof STATUS;

type Props =
  | { status: StatusKey; tone?: never; children?: never; className?: string }
  | { status?: never; tone: BadgeTone; children: ReactNode; className?: string };

/** Etiqueta de estado (chip de color). Usa `status` para estados conocidos o `tone` + texto libre. */
export function StatusBadge({ status, tone, children, className }: Props) {
  const resolvedTone = status ? STATUS[status].tone : tone;
  const label = status ? STATUS[status].label : children;
  return <span className={cx(styles.badge, styles[resolvedTone], className)}>{label}</span>;
}
