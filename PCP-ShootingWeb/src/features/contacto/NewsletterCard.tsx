import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Button, TextField } from '@/components/ui';
import { IconCheck } from '@/components/icons/icons';
import { subscribeNewsletter } from './api';
import { newsletterSchema, type NewsletterValues } from './schemas';
import { DATA_SOURCE } from '@/lib/api';
import styles from './NewsletterCard.module.css';

/** "Recibe eventos y promociones": suscripción por correo. Oculta en modo API (POST /api/suscripciones pendiente). */
export function NewsletterCard() {
  if (DATA_SOURCE === 'api') return null;
  return <NewsletterCardDemo />;
}

function NewsletterCardDemo() {
  const [done, setDone] = useState(false);
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<NewsletterValues>({ resolver: zodResolver(newsletterSchema) });

  const onSubmit = handleSubmit(async (values) => {
    await subscribeNewsletter(values);
    setDone(true);
  });

  return (
    <section className={styles.card} aria-labelledby="newsletter-titulo">
      <div className={styles.text}>
        <h2 id="newsletter-titulo" className={styles.title}>
          Recibe eventos y promociones
        </h2>
        <p className={styles.copy}>Torneos, nuevos rifles y fechas especiales directo en tu correo.</p>
      </div>

      {done ? (
        <p className={styles.success} role="status">
          <IconCheck size={20} /> ¡Listo! Te avisaremos de los próximos eventos.
        </p>
      ) : (
        <form className={styles.form} onSubmit={onSubmit} noValidate>
          <TextField
            label="Correo electrónico"
            type="email"
            autoComplete="email"
            placeholder="tucorreo@ejemplo.com"
            error={errors.email?.message}
            className={styles.field}
            {...register('email')}
          />
          <Button type="submit" size="lg" loading={isSubmitting} className={styles.submit}>
            Suscribirme
          </Button>
        </form>
      )}
    </section>
  );
}
