import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { club } from '@/app/navigation';
import { Button, ButtonLink, Checkbox, FileField, FormAlert, TextField } from '@/components/ui';
import { IconCheck } from '@/components/icons/icons';
import { useAuth } from '@/features/auth/AuthProvider';
import { PaymentRedirect } from '@/features/pagos/PaymentMethods';
import { PERIODS, priceFor, type MembershipPlan, type Period } from '@/features/membresias/plans';
import { formatDate, formatMXN } from '@/lib/format';
import styles from './MembresiasPage.module.css';

const MAX_MB = 5;
const ACCEPTED = ['image/jpeg', 'image/png', 'image/webp', 'application/pdf'];

const schema = z.object({
  nombre: z.string().trim().min(2, 'Escribe tu nombre completo'),
  email: z.string().trim().min(1, 'Escribe tu correo electrónico').email('Escribe un correo válido'),
  telefono: z
    .string()
    .trim()
    .transform((v) => v.replace(/[\s()-]/g, ''))
    .refine((v) => /^(\+?52)?\d{10}$/.test(v), 'Escribe un teléfono de 10 dígitos'),
  // PENDIENTE: requisitos exactos de identificación (CLAUDE.md §8). Con Supabase se sube a Storage.
  identificacion: z
    .custom<FileList>()
    .refine((f) => f instanceof FileList && f.length > 0, 'Sube una foto o PDF de tu identificación')
    .refine((f) => !(f instanceof FileList) || !f[0] || ACCEPTED.includes(f[0].type), 'Usa una imagen (JPG, PNG) o un PDF')
    .refine((f) => !(f instanceof FileList) || !f[0] || f[0].size <= MAX_MB * 1024 * 1024, `El archivo debe pesar menos de ${MAX_MB} MB`),
  acepta: z.literal(true, { error: 'Debes aceptar la carta responsiva, el reglamento y las políticas' }),
});
type Input = z.input<typeof schema>;
type Values = z.output<typeof schema>;

type Props = { plan: MembershipPlan; period: Period };

/** Inscripción / renovación: datos + identificación → pago único (Mercado Pago, simulado). */
export function MembershipSignup({ plan, period }: Props) {
  const { user, activateMembership } = useAuth();
  const [phase, setPhase] = useState<'idle' | 'paying'>('idle');
  const [error, setError] = useState<string | null>(null);
  const [doneUntil, setDoneUntil] = useState<string | null>(null);
  const months = PERIODS.find((p) => p.id === period)!.months;
  const price = priceFor(plan, period);
  const renewing = Boolean(user?.membership);

  const {
    register,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<Input, unknown, Values>({
    resolver: zodResolver(schema),
    defaultValues: { nombre: user?.name ?? '', email: user?.email ?? '', telefono: user?.phone ?? '' },
  });
  const file = watch('identificacion') as FileList | undefined;

  const onSubmit = handleSubmit(async () => {
    setError(null);
    setPhase('paying');
    try {
      // TODO(API): POST /api/membresias (+ subir identificación) → preferencia de Mercado Pago.
      const u = await activateMembership({ name: plan.name, gold: plan.variant === 'gold' }, months);
      setDoneUntil(u.membership?.validUntil ?? null);
    } catch {
      setError('No pudimos completar el pago. Intenta de nuevo.');
    } finally {
      setPhase('idle');
    }
  });

  if (doneUntil) {
    return (
      <div className={styles.done} role="status">
        <span className={styles.doneCheck} aria-hidden="true">
          <IconCheck size={28} strokeWidth={3} />
        </span>
        <h3>{renewing ? '¡Membresía renovada!' : '¡Bienvenido al club!'}</h3>
        <p>
          Tu plan <strong>{plan.name}</strong> está vigente hasta el <strong>{formatDate(doneUntil)}</strong>. Tu credencial
          digital ya está en tu perfil.
        </p>
        <ButtonLink to="/perfil#membresia" size="lg">
          Ver mi credencial
        </ButtonLink>
      </div>
    );
  }

  if (!user) {
    return (
      <div className={styles.needAccount}>
        <h3>Crea tu cuenta para continuar</h3>
        <p>Tu membresía queda ligada a tu cuenta: ahí verás tu credencial digital, vigencia y renovaciones.</p>
        <div className={styles.needActions}>
          <ButtonLink to={`/registro?next=${encodeURIComponent('/membresias#inscripcion')}`} size="lg">
            Crear cuenta
          </ButtonLink>
          <ButtonLink to={`/login?next=${encodeURIComponent('/membresias#inscripcion')}`} variant="secondary" size="lg">
            Ya tengo cuenta
          </ButtonLink>
        </div>
      </div>
    );
  }

  return (
    <form className={styles.form} onSubmit={onSubmit} noValidate>
      {error && <FormAlert>{error}</FormAlert>}
      <div className={styles.formGrid}>
        <TextField label="Nombre completo" autoComplete="name" error={errors.nombre?.message} {...register('nombre')} />
        <TextField label="Correo electrónico" type="email" autoComplete="email" error={errors.email?.message} {...register('email')} />
        <TextField
          label="Teléfono / WhatsApp"
          type="tel"
          inputMode="tel"
          autoComplete="tel"
          error={errors.telefono?.message}
          {...register('telefono')}
        />
        <FileField
          label="Identificación oficial"
          accept={ACCEPTED.join(',')}
          fileName={file?.[0]?.name}
          hint={`INE o pasaporte. JPG, PNG o PDF de hasta ${MAX_MB} MB.`}
          error={errors.identificacion?.message as string | undefined}
          {...register('identificacion')}
        />
      </div>
      <Checkbox
        label={
          <>
            Acepto la carta responsiva, el{' '}
            <a href={club.rulesHref} target="_blank" rel="noreferrer">
              reglamento del campo
            </a>{' '}
            y las{' '}
            <a href={club.privacyHref} target="_blank" rel="noreferrer">
              políticas de privacidad
            </a>
          </>
        }
        error={errors.acepta?.message}
        {...register('acepta')}
      />
      {phase === 'paying' && <PaymentRedirect />}
      <div>
        <Button type="submit" size="lg" loading={phase === 'paying'} className={styles.submit}>
          Pagar {formatMXN(price)} · {months === 1 ? '1 mes' : `${months} meses`}
        </Button>
      </div>
      <p className={styles.fine}>Pago único por el periodo elegido. Sin cargos automáticos: renuevas cuando tú quieras.</p>
    </form>
  );
}
