import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { club } from '@/app/navigation';
import { Button, Checkbox, SelectField, TextareaField, TextField } from '@/components/ui';
import { IconCheck } from '@/components/icons/icons';
import { sendContactMessage } from './api';
import { CONTACT_REASONS, contactSchema, type ContactInput, type ContactValues } from './schemas';
import styles from './ContactForm.module.css';

type Props = {
  /** Valores iniciales (p. ej. desde "Solicitar cotización" del catálogo). */
  defaults?: Partial<ContactInput>;
};

export function ContactForm({ defaults }: Props) {
  const [sent, setSent] = useState(false);
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<ContactInput, unknown, ContactValues>({
    resolver: zodResolver(contactSchema),
    defaultValues: { motivo: 'reservacion', ...defaults },
  });

  const onSubmit = handleSubmit(async (values) => {
    await sendContactMessage(values);
    setSent(true);
  });

  if (sent) {
    return (
      <div className={styles.success} role="status">
        <span className={styles.successIcon} aria-hidden="true">
          <IconCheck size={28} />
        </span>
        <h3 className={styles.successTitle}>¡Mensaje enviado!</h3>
        <p>
          Te respondemos lo antes posible. Si es urgente, escríbenos por{' '}
          <a href={club.whatsappHref} target="_blank" rel="noreferrer">
            WhatsApp
          </a>
          .
        </p>
        <Button
          variant="secondary"
          onClick={() => {
            reset({ motivo: 'reservacion' });
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
      <TextField
        label="Nombre completo"
        autoComplete="name"
        placeholder="Tu nombre"
        error={errors.nombre?.message}
        {...register('nombre')}
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
        label="Correo electrónico"
        type="email"
        autoComplete="email"
        placeholder="tucorreo@ejemplo.com"
        error={errors.email?.message}
        {...register('email')}
      />
      <SelectField
        label="Motivo"
        options={CONTACT_REASONS.map((r) => ({ value: r.value, label: r.label }))}
        error={errors.motivo?.message}
        {...register('motivo')}
      />
      <TextareaField
        className={styles.full}
        label="Mensaje"
        placeholder="Cuéntanos qué necesitas"
        error={errors.mensaje?.message}
        {...register('mensaje')}
      />
      <Checkbox
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
