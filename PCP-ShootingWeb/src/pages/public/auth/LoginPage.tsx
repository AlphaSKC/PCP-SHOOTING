import { useState } from 'react';
import { Link, Navigate, useLocation, useSearchParams } from 'react-router';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Button, Checkbox, FormAlert, PasswordField, TextField } from '@/components/ui';
import { useAuth } from '@/features/auth/AuthProvider';
import { GOOGLE_ENABLED } from '@/features/auth/authService';
import { loginSchema, type LoginValues } from '@/features/auth/schemas';
import { AuthError } from '@/features/auth/types';
import { AuthHeading, AuthShell, AuthSwitch, OrDivider } from './AuthShell';
import { useAfterLogin } from './useAfterLogin';
import styles from './AuthForms.module.css';

export default function LoginPage() {
  const { user, status, signIn, signInWithGoogle, resendConfirmation } = useAuth();
  const [params] = useSearchParams();
  const confirmed = params.get('confirmado') === '1';
  // Errores que Supabase devuelve en la URL (enlace vencido, OAuth cancelado…).
  const urlError = params.get('error_description') || params.get('error');
  const [unconfirmedEmail, setUnconfirmedEmail] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const { search } = useLocation();
  const after = useAfterLogin();
  const [formError, setFormError] = useState<string | null>(null);
  const [googleLoading, setGoogleLoading] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '', remember: false },
  });

  // Si ya hay sesión, no tiene sentido ver el login.
  if (status === 'authenticated' && user && !isSubmitting && !googleLoading) {
    return <Navigate to={after.target(user)} replace />;
  }

  const onSubmit = handleSubmit(async ({ email, password, remember }) => {
    setFormError(null);
    setNotice(null);
    setUnconfirmedEmail(null);
    try {
      after.go(await signIn(email, password, remember));
    } catch (e) {
      const message = e instanceof AuthError ? e.message : 'No pudimos iniciar sesión. Intenta de nuevo.';
      setFormError(message);
      if (message.startsWith('Aún no confirmas')) setUnconfirmedEmail(email);
    }
  });

  const onGoogle = async () => {
    setFormError(null);
    setGoogleLoading(true);
    try {
      after.go(await signInWithGoogle(params.get('next') ?? undefined));
    } catch (e) {
      setFormError(e instanceof AuthError ? e.message : 'No pudimos conectar con Google. Intenta de nuevo.');
      setGoogleLoading(false);
    }
  };

  return (
    <>
      <title>Iniciar sesión · PCP Shooting</title>
      <AuthShell tabs search={search}>
        <AuthHeading title="Bienvenido de vuelta" lead="Ingresa con tu correo y contraseña." />

        <form className={styles.form} onSubmit={onSubmit} noValidate>
          {confirmed && !formError && <FormAlert tone="success" inline>¡Correo confirmado! Ya puedes iniciar sesión.</FormAlert>}
          {urlError && !formError && <FormAlert inline>El enlace no es válido o ya expiró. Inicia sesión o solicita uno nuevo.</FormAlert>}
          {notice && <FormAlert tone="success" inline>{notice}</FormAlert>}
          {formError && <FormAlert>{formError}</FormAlert>}
          {unconfirmedEmail && (
            <Button
              variant="secondary"
              onClick={async () => {
                try {
                  await resendConfirmation(unconfirmedEmail);
                  setFormError(null);
                  setNotice('Te reenviamos el enlace de confirmación. Revisa tu correo.');
                } catch (e) {
                  setFormError(e instanceof AuthError ? e.message : 'No pudimos reenviar el correo.');
                }
              }}
            >
              Reenviar correo de confirmación
            </Button>
          )}
          <TextField
            label="Correo electrónico"
            type="email"
            autoComplete="email"
            placeholder="tucorreo@ejemplo.com"
            error={errors.email?.message}
            {...register('email')}
          />
          <PasswordField
            label="Contraseña"
            autoComplete="current-password"
            placeholder="Tu contraseña"
            labelAside={
              <Link to="/recuperar" className={styles.desktopForgot}>
                ¿Olvidaste tu contraseña?
              </Link>
            }
            error={errors.password?.message}
            {...register('password')}
          />
          <div className={styles.row}>
            <Checkbox label="Mantener sesión iniciada" {...register('remember')} />
            <Link to="/recuperar" className={styles.mobileForgot}>
              ¿Olvidaste tu contraseña?
            </Link>
          </div>
          <Button type="submit" size="lg" fullWidth loading={isSubmitting}>
            Iniciar sesión
          </Button>
        </form>

        {GOOGLE_ENABLED && (
          <>
            <OrDivider />
            <Button variant="secondary" size="lg" fullWidth loading={googleLoading} onClick={onGoogle}>
              Continuar con Google
            </Button>
          </>
        )}

        <AuthSwitch>
          ¿No tienes cuenta?
          <Link to={`/registro${search}`}>Crea una aquí</Link>
        </AuthSwitch>
      </AuthShell>
    </>
  );
}
