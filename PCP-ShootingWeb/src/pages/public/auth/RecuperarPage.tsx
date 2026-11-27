import { useState } from 'react';
import { Link } from 'react-router';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Button, FormAlert, TextField } from '@/components/ui';
import { IconArrowLeft } from '@/components/icons/icons';
import { useAuth } from '@/features/auth/AuthProvider';
import { recoverSchema, type RecoverValues } from '@/features/auth/schemas';
import { AuthHeading, AuthShell } from './AuthShell';
import styles from './AuthForms.module.css';

/** Recuperar contraseña. No hay diseño propio: usa el marco de Login. */
export default function RecuperarPage() {
  const { resetPassword } = useAuth();
  const [sentTo, setSentTo] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RecoverValues>({ resolver: zodResolver(recoverSchema) });

  const onSubmit = handleSubmit(async ({ email }) => {
    setFormError(null);
    try {
      await resetPassword(email);
      setSentTo(email);
    } catch {
      setFormError('No pudimos enviar el enlace. Intenta de nuevo.');
    }
  });

  return (
    <>
      <title>Recuperar contraseña · PCP Shooting</title>
      <AuthShell>
        <AuthHeading
          title="Recupera tu contraseña"
          lead="Escribe el correo de tu cuenta y te enviaremos un enlace para crear una nueva."
        />

        {sentTo ? (
          <div className={styles.success}>
            <FormAlert tone="success" inline>Revisa tu correo</FormAlert>
            <p>
              Si <strong>{sentTo}</strong> tiene una cuenta, recibirás un enlace para restablecer tu contraseña en
              unos minutos. Revisa también la carpeta de spam.
            </p>
            <Button variant="secondary" size="lg" fullWidth onClick={() => setSentTo(null)}>
              Usar otro correo
            </Button>
          </div>
        ) : (
          <form className={styles.form} onSubmit={onSubmit} noValidate>
            {formError && <FormAlert>{formError}</FormAlert>}
            <TextField
              label="Correo electrónico"
              type="email"
              autoComplete="email"
              placeholder="tucorreo@ejemplo.com"
              error={errors.email?.message}
              {...register('email')}
            />
            <Button type="submit" size="lg" fullWidth loading={isSubmitting}>
              Enviar enlace
            </Button>
          </form>
        )}

        <Link to="/login" className={styles.back}>
          <IconArrowLeft size={18} /> Volver a iniciar sesión
        </Link>
      </AuthShell>
    </>
  );
}
