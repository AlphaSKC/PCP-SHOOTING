import { useState } from 'react';
import { Link, Navigate, useLocation } from 'react-router';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { club } from '@/app/navigation';
import { Button, Checkbox, FormAlert, PasswordField, TextField } from '@/components/ui';
import { useAuth } from '@/features/auth/AuthProvider';
import { registerSchema, type RegisterInput, type RegisterValues } from '@/features/auth/schemas';
import { AuthError } from '@/features/auth/types';
import { AuthHeading, AuthShell, AuthSwitch } from './AuthShell';
import { useAfterLogin } from './useAfterLogin';
import styles from './AuthForms.module.css';

export default function RegistroPage() {
  const { user, status, signUp, resendConfirmation } = useAuth();
  const { search } = useLocation();
  const after = useAfterLogin();
  const [formError, setFormError] = useState<string | null>(null);
  const [pendingEmail, setPendingEmail] = useState<string | null>(null);
  const [resent, setResent] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RegisterInput, unknown, RegisterValues>({ resolver: zodResolver(registerSchema) });

  if (status === 'authenticated' && user && !isSubmitting) {
    return <Navigate to={after.target(user)} replace />;
  }

  const onSubmit = handleSubmit(async ({ nombre, email, telefono, password }) => {
    setFormError(null);
    try {
      const result = await signUp({ nombre, email, telefono, password });
      if (result.status === 'signed_in') after.go(result.user);
      else setPendingEmail(result.email);
    } catch (e) {
      setFormError(e instanceof AuthError ? e.message : 'No pudimos crear tu cuenta. Intenta de nuevo.');
    }
  });

  if (pendingEmail) {
    return (
      <>
        <title>Confirma tu correo · PCP Shooting</title>
        <AuthShell>
          <AuthHeading title="Revisa tu correo" lead="Te enviamos un enlace para confirmar tu cuenta." />
          <div className={styles.success}>
            <FormAlert tone="success" inline>Cuenta creada. Falta un paso.</FormAlert>
            <p>
              Abre el enlace que enviamos a <strong>{pendingEmail}</strong> (revisa también spam). Después podrás
              iniciar sesión.
            </p>
            {resent && <FormAlert tone="success">Te reenviamos el enlace.</FormAlert>}
            {formError && <FormAlert>{formError}</FormAlert>}
            <Button
              variant="secondary"
              size="lg"
              fullWidth
              onClick={async () => {
                try {
                  await resendConfirmation(pendingEmail);
                  setResent(true);
                } catch (e) {
                  setFormError(e instanceof AuthError ? e.message : 'No pudimos reenviar el correo.');
                }
              }}
            >
              Reenviar correo
            </Button>
            <Link to={`/login${search}`} className={styles.back}>
              Ir a iniciar sesión
            </Link>
          </div>
        </AuthShell>
      </>
    );
  }

  return (
    <>
      <title>Crear cuenta · PCP Shooting</title>
      <AuthShell tabs search={search}>
        <AuthHeading title="Crea tu cuenta" lead="Reserva más rápido y lleva el control de tus compras." />

        <form className={styles.form} onSubmit={onSubmit} noValidate>
          {formError && <FormAlert>{formError}</FormAlert>}
          <TextField
            label="Nombre completo"
            autoComplete="name"
            placeholder="Tu nombre"
            error={errors.nombre?.message}
            {...register('nombre')}
          />
          <div className={styles.grid2}>
            <TextField
              label="Correo electrónico"
              type="email"
              autoComplete="email"
              placeholder="tucorreo@ejemplo.com"
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
          </div>
          <PasswordField
            label="Contraseña"
            autoComplete="new-password"
            placeholder="Mínimo 8 caracteres"
            error={errors.password?.message}
            {...register('password')}
          />
          <Checkbox
            label={
              <>
                Acepto el{' '}
                <a href={club.rulesHref} target="_blank" rel="noreferrer">
                  reglamento del campo de tiro
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
          <Button type="submit" size="lg" fullWidth loading={isSubmitting}>
            Crear cuenta
          </Button>
        </form>

        <AuthSwitch>
          ¿Ya tienes cuenta?
          <Link to={`/login${search}`}>Inicia sesión</Link>
        </AuthSwitch>
      </AuthShell>
    </>
  );
}
