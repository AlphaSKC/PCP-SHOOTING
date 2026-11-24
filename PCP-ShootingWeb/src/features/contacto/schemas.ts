import { z } from 'zod';

export const CONTACT_REASONS = [
  { value: 'reservacion', label: 'Reservación' },
  { value: 'grupos', label: 'Eventos y grupos' },
  { value: 'tienda', label: 'Compra en tienda' },
  { value: 'cotizacion', label: 'Información de catálogo' },
  { value: 'otro', label: 'Otro' },
] as const;

type Reason = (typeof CONTACT_REASONS)[number]['value'];
const reasonValues = CONTACT_REASONS.map((r) => r.value) as [Reason, ...Reason[]];

const email = z.string().trim().min(1, 'Escribe tu correo electrónico').max(254, 'Máximo 254 caracteres').email('Escribe un correo válido, por ejemplo tucorreo@ejemplo.com');

export const contactSchema = z.object({
  nombre: z.string().trim().min(2, 'Escribe tu nombre').max(150, 'Máximo 150 caracteres'),
  telefono: z
    .string()
    .trim()
    .transform((v) => v.replace(/[\s()-]/g, ''))
    .refine((v) => v === '' || /^(\+?52)?\d{10}$/.test(v), 'Escribe un teléfono de 10 dígitos o deja el campo vacío'),
  email,
  motivo: z.enum(reasonValues),
  mensaje: z.string().trim().min(10, 'Cuéntanos un poco más (mínimo 10 caracteres)').max(2000, 'Máximo 2000 caracteres'),
  privacidad: z.literal(true, { error: 'Debes aceptar las políticas de privacidad' }),
});

export type ContactInput = z.input<typeof contactSchema>;
export type ContactValues = z.output<typeof contactSchema>;

export const newsletterSchema = z.object({ email });
export type NewsletterValues = z.infer<typeof newsletterSchema>;
