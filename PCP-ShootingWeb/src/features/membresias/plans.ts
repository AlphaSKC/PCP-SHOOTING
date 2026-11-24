/*
 * Planes de membresía CARTAQ. DATOS DE EJEMPLO (CLAUDE.md §8): nombres, precios, vigencias
 * y beneficios reales están PENDIENTES de definir con el cliente. Vendrán de GET /api/membresias/planes.
 *
 * Reglas (CLAUDE.md §6): pago ÚNICO por periodo y renovación MANUAL. Sin cobros recurrentes.
 */

export type PlanVariant = 'basic' | 'standard' | 'gold';

export type MembershipPlan = {
  id: string;
  name: string;
  tagline: string;
  /** Precio por un mes. */
  monthly: number;
  benefits: string[];
  /** Beneficios que el diseño de celular muestra (lista corta). */
  benefitsShort: number;
  variant: PlanVariant;
  badge?: string;
};

export const PLANS: MembershipPlan[] = [
  {
    id: 'tirador',
    name: 'Tirador',
    tagline: 'Para quien viene de vez en cuando y quiere precio preferente.',
    monthly: 499,
    benefits: ['Precio preferente en reservas', 'Reserva hasta 7 días antes', 'Credencial digital', 'Boletín de eventos'],
    benefitsShort: 3,
    variant: 'basic',
  },
  {
    id: 'socio',
    name: 'Socio',
    tagline: 'Para quien practica cada semana.',
    monthly: 899,
    benefits: ['4 sesiones incluidas al mes', 'Reserva con prioridad', 'Descuento en tienda de ropa', 'Inscripción preferente a torneos'],
    benefitsShort: 3,
    variant: 'standard',
  },
  {
    id: 'socio-oro',
    name: 'Socio Oro',
    tagline: 'La experiencia completa del club.',
    monthly: 1499,
    benefits: [
      'Sesiones ilimitadas',
      'Línea de tiro preferente',
      'Playera y gorra oficial de bienvenida',
      'Torneos internos incluidos',
      'Invitado sin costo 4 veces al mes',
    ],
    benefitsShort: 4,
    variant: 'gold',
    badge: 'Más completo',
  },
];

export type Period = 'mensual' | 'anual';

export const PERIODS: { id: Period; label: string; months: number; note?: string }[] = [
  { id: 'mensual', label: 'Mensual', months: 1 },
  { id: 'anual', label: 'Anual', months: 12, note: '2 meses gratis' },
];

/** Anual = paga 10 meses y recibe 12 (dato del diseño, pendiente de confirmar). */
export const priceFor = (plan: MembershipPlan, period: Period) => (period === 'anual' ? plan.monthly * 10 : plan.monthly);
