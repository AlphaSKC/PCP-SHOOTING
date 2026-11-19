import { z } from 'zod';

const email = z
  .string()
  .trim()
  .min(1, 'Escribe tu correo electrónico')
  .email('Escribe un correo válido, por ejemplo tucorreo@ejemplo.com');

// Debe coincidir con la política de Supabase (Authentication → Policies): mínimo 8, letras y números.
const newPassword = z
  .string()
  .min(8, 'Usa al menos 8 caracteres')
  .max(72, 'Máximo 72 caracteres')
  .refine((v) => /[A-Za-zÁÉÍÓÚÑáéíóúñ]/.test(v) && /\d/.test(v), 'Combina letras y números');

const phone = z
  .string()
  .trim()
  .transform((v) => v.replace(/[\s()-]/g, ''))
  .refine((v) => /^(\+?52)?\d{10}$/.test(v), 'Escribe un teléfono de 10 dígitos');

export const loginSchema = z.object({
  email,
  password: z.string().min(1, 'Escribe tu contraseña'),
  remember: z.boolean(),
});
export type LoginValues = z.infer<typeof loginSchema>;

export const registerSchema = z.object({
  nombre: z.string().trim().min(2, 'Escribe tu nombre completo'),
  email,
  telefono: phone,
  password: newPassword,
  acepta: z.literal(true, { error: 'Debes aceptar el reglamento y las políticas de privacidad' }),
});
export type RegisterInput = z.input<typeof registerSchema>;
export type RegisterValues = z.output<typeof registerSchema>;

export const recoverSchema = z.object({ email });
export type RecoverValues = z.infer<typeof recoverSchema>;

export const profileSchema = z.object({
  nombre: z.string().trim().min(2, 'Escribe tu nombre completo'),
  email,
  telefono: phone,
});
export type ProfileInput = z.input<typeof profileSchema>;
export type ProfileValues = z.output<typeof profileSchema>;

export const passwordSchema = z
  .object({
    password: newPassword,
    confirm: z.string(),
  })
  .refine((v) => v.password === v.confirm, { path: ['confirm'], message: 'Las contraseñas no coinciden' });
export type PasswordValues = z.infer<typeof passwordSchema>;

/** Solo permite regresar a rutas internas (evita redirecciones abiertas). */
export function safeNext(next: string | null): string | null {
  if (!next || !next.startsWith('/') || next.startsWith('//')) return null;
  return next;
}
