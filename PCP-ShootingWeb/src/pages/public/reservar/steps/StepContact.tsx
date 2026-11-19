import { Controller, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { club } from '@/app/navigation';
import { Button, Checkbox, TextField } from '@/components/ui';
import { IconFile } from '@/components/icons/icons';
import { useAuth } from '@/features/auth/AuthProvider';
import { REQUIRE_SIGNATURE } from '@/features/reservas/config';
import { SignaturePad } from '@/features/reservas/components/SignaturePad';
import { WizardActions } from '@/features/reservas/components/WizardActions';
import type { ReservationDraft } from '@/features/reservas/types';
import styles from '../ReservarPage.module.css';

const today = () => new Date().toISOString().slice(0, 10);

const schema = z.object({
  nombre: z.string().trim().min(2, 'Escribe tu nombre completo'),
  email: z.string().trim().min(1, 'Escribe tu correo electrónico').email('Escribe un correo válido'),
  telefono: z
    .string()
    .trim()
    .transform((v) => v.replace(/[\s()-]/g, ''))
    .refine((v) => /^(\+?52)?\d{10}$/.test(v), 'Escribe un teléfono de 10 dígitos'),
  // PENDIENTE: edad mínima y reglas para menores (CLAUDE.md §8).
  nacimiento: z
    .string()
    .min(1, 'Escribe tu fecha de nacimiento')
    .refine((v) => v < today(), 'La fecha no es válida'),
  acepta: z.literal(true, { error: 'Debes aceptar la carta responsiva y el reglamento' }),
  firma: REQUIRE_SIGNATURE ? z.string({ error: 'Firma la carta responsiva' }).min(1, 'Firma la carta responsiva') : z.string().nullable(),
});

type Input = z.input<typeof schema>;
type Values = z.output<typeof schema>;

type Props = {
  draft: ReservationDraft;
  update: (c: Partial<ReservationDraft>) => void;
  onBack: () => void;
  onNext: () => void;
  summary: string;
  total: number;
};

export function StepContact({ draft, update, onBack, onNext, summary, total }: Props) {
  const { user } = useAuth();
  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<Input, unknown, Values>({
    resolver: zodResolver(schema),
    defaultValues: {
      nombre: draft.contact?.nombre ?? user?.name ?? '',
      email: draft.contact?.email ?? user?.email ?? '',
      telefono: draft.contact?.telefono ?? user?.phone ?? '',
      nacimiento: draft.contact?.nacimiento ?? '',
      acepta: (draft.waiver?.accepted || undefined) as true | undefined,
      firma: draft.waiver?.signature ?? undefined,
    },
  });

  const onSubmit = handleSubmit(({ nombre, email, telefono, nacimiento, firma }) => {
    update({
      contact: { nombre, email, telefono, nacimiento },
      waiver: { accepted: true, signature: firma ?? null },
    });
    onNext();
  });

  return (
    <section className={styles.step} aria-labelledby="paso-3">
      <h2 id="paso-3" className={styles.stepTitle}>
        <span className={styles.desktopInline}>3. Tus datos y carta responsiva</span>
        <span className={styles.mobileInline}>Tus datos</span>
      </h2>

      <form id="form-datos" className={styles.contactForm} onSubmit={onSubmit} noValidate>
        <div className={styles.contactGrid}>
          <TextField label="Nombre completo" autoComplete="name" error={errors.nombre?.message} {...register('nombre')} />
          <TextField
            label="Correo electrónico"
            type="email"
            autoComplete="email"
            error={errors.email?.message}
            {...register('email')}
          />
          <TextField
            label="Teléfono / WhatsApp"
            type="tel"
            inputMode="tel"
            autoComplete="tel"
            placeholder="10 dígitos"
            error={errors.telefono?.message}
            {...register('telefono')}
          />
          <TextField
            label="Fecha de nacimiento"
            type="date"
            autoComplete="bday"
            max={today()}
            error={errors.nacimiento?.message}
            {...register('nacimiento')}
          />
        </div>

        <div className={styles.waiver}>
          <div className={styles.waiverHead}>
            <IconFile size={22} className={styles.waiverIcon} />
            <h3 className={styles.waiverTitle}>Carta responsiva</h3>
          </div>
          {/* TEXTO PENDIENTE: el cliente entrega la carta responsiva oficial. */}
          <div tabIndex={0} className={styles.waiverText} aria-label="Texto de la carta responsiva">
            Declaro que conozco y acepto el reglamento del campo de tiro y uso de armas de PCP Shooting, que seguiré en
            todo momento las indicaciones del personal y que usaré el equipo de seguridad. Libero al club de cualquier
            responsabilidad por daños derivados del mal uso del equipo o del incumplimiento del reglamento. Los menores
            de edad deben venir acompañados de su padre, madre o tutor, quien firma por ellos. [TEXTO DE EJEMPLO: el
            club entrega la versión oficial.]
          </div>

          {REQUIRE_SIGNATURE && (
            <div className={styles.signature}>
              <span className={styles.signatureLabel} id="firma-label">
                Firma
              </span>
              <Controller
                control={control}
                name="firma"
                render={({ field }) => (
                  <SignaturePad
                    value={field.value ?? null}
                    onChange={(v) => field.onChange(v ?? undefined)}
                    invalid={Boolean(errors.firma)}
                    describedBy={errors.firma ? 'firma-error' : 'firma-label'}
                  />
                )}
              />
              {errors.firma && (
                <p id="firma-error" className={styles.fieldError}>
                  {errors.firma.message}
                </p>
              )}
            </div>
          )}

          <Checkbox
            label={
              <>
                He leído y acepto la carta responsiva y el{' '}
                <a href={club.rulesHref} target="_blank" rel="noreferrer">
                  reglamento del campo de tiro
                </a>
              </>
            }
            error={errors.acepta?.message}
            {...register('acepta')}
          />
        </div>
      </form>

      <WizardActions
        summary={summary}
        total={total}
        onBack={onBack}
        primary={
          <Button type="submit" form="form-datos" size="lg">
            Continuar al pago
          </Button>
        }
      />
    </section>
  );
}
