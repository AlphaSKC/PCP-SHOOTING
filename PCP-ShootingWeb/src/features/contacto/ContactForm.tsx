import { useRef, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useClub } from '@/features/contenido/ContenidoProvider';
import { Button, Checkbox, FormAlert, SelectField, TextareaField, TextField } from '@/components/ui';
import { DATA_SOURCE } from '@/lib/api';
import { IconCheck } from '@/components/icons/icons';
import { sendContactMessage } from './api';
import { CONTACT_REASONS, contactSchema, type ContactInput, type ContactValues } from './schemas';
import styles from './ContactForm.module.css';

type Props = {
  /** Valores iniciales (p. ej. desde "Solicitar cotización" del catálogo). */
  defaults?: Partial<ContactInput>;
};

/** Envía a la API real; el modo demo permanece separado. */
export function ContactForm({ defaults }: Props) {
  const club = useClub();
  const [sent, setSent] = useState(false);
  const [error, setError] = useState('');
  const operacion = useRef<{ datos: string; id: string } | null>(null);
  const sitioWeb = useRef<HTMLInputElement>(null);
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<ContactInput, unknown, ContactValues>({
    resolver: zodResolver(contactSchema),
    defaultValues: { motivo: 'reservacion', telefono: '', ...defaults },
  });

  const onSubmit = handleSubmit(async (values) => {
    const datos = JSON.stringify(values);
    if (!operacion.current || operacion.current.datos !== datos) operacion.current = { datos, id: crypto.randomUUID() };
    setError('');
    try { await sendContactMessage(values, operacion.current.id, sitioWeb.current?.value ?? ''); setSent(true); }
    catch (e) { setError(e instanceof Error ? e.message : 'No pudimos guardar el mensaje. Conservamos tus datos para reintentar.'); }
  });

  if (sent) {
    return (
      <div className={styles.success} role="status">
        <span className={styles.successIcon} aria-hidden="true">
          <IconCheck size={28} />
        </span>
        <h3 className={styles.successTitle}>{DATA_SOURCE === 'api' ? '¡Recibimos tu mensaje!' : 'Mensaje de demostración'}</h3>
        <p>
          El equipo podrá revisar tu solicitud. Si es urgente, escríbenos por{' '}
          <a href={club.whatsappHref} target="_blank" rel="noreferrer">
            WhatsApp
          </a>
          .
        </p>
        <Button
          variant="secondary"
          onClick={() => {
            reset({ motivo: 'reservacion', telefono: '' });
            operacion.current = null;
            setSent(false);
          }}
        >
          Enviar otro mensaje
        </Button>
      </div>
    );
  }

  return (
    <form className={styles.form} onSubmit={onSubmit} noValidate>
      {error && <div className={styles.full}><FormAlert tone="error">{error}</FormAlert></div>}
      <div className={styles.honeypot} aria-hidden="true"><label>Sitio web<input ref={sitioWeb} name="sitioWeb" autoComplete="off" tabIndex={-1} /></label></div>
      <TextField
        label="Nombre completo"
        disabled={isSubmitting}
        autoComplete="name"
        placeholder="Tu nombre"
        maxLength={150}
        error={errors.nombre?.message}
        {...register('nombre')}
      />
      <TextField
        label="Teléfono / WhatsApp (opcional)"
        disabled={isSubmitting}
        type="tel"
        inputMode="tel"
        autoComplete="tel"
        placeholder="10 dígitos"
        error={errors.telefono?.message}
        {...register('telefono')}
      />
      <TextField
        label="Correo electrónico"
        disabled={isSubmitting}
        type="email"
        autoComplete="email"
        placeholder="tucorreo@ejemplo.com"
        maxLength={254}
        error={errors.email?.message}
        {...register('email')}
      />
      <SelectField
        label="Motivo"
        disabled={isSubmitting}
        options={CONTACT_REASONS.map((r) => ({ value: r.value, label: r.label }))}
        error={errors.motivo?.message}
        {...register('motivo')}
      />
      <TextareaField
        className={styles.full}
        label="Mensaje"
        disabled={isSubmitting}
        placeholder="Cuéntanos qué necesitas"
        maxLength={2000}
        error={errors.mensaje?.message}
        {...register('mensaje')}
      />
      <Checkbox
        disabled={isSubmitting}
        className={styles.full}
        label={
          <>
            Acepto las{' '}
            <a href={club.privacyHref} target="_blank" rel="noreferrer">
              políticas de privacidad
            </a>
          </>
        }
        error={errors.privacidad?.message}
        {...register('privacidad')}
      />
      <div className={styles.full}>
        <Button type="submit" size="lg" loading={isSubmitting} className={styles.submit}>
          Enviar mensaje
        </Button>
      </div>
    </form>
  );
}
