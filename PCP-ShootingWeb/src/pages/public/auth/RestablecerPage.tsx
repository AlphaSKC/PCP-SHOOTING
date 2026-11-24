import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Button, FormAlert, PasswordField } from '@/components/ui';
import { useAuth } from '@/features/auth/AuthProvider';
import { passwordSchema, type PasswordValues } from '@/features/auth/schemas';
import { AuthError } from '@/features/auth/types';
import { LoadingScreen } from '@/pages/LoadingScreen';
import { AuthHeading, AuthShell } from './AuthShell';
import styles from './AuthForms.module.css';

/**
 * Destino del enlace "restablecer contraseña" del correo.
 * Supabase canjea el código (PKCE) y abre una sesión temporal de recuperación; aquí se define la nueva contraseña.
 */
export default function RestablecerPage() {
  const { status, recovering, updatePassword } = useAuth();
  const navigate = useNavigate();
  const [done, setDone] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [waited, setWaited] = useState(false);

  // El canje del código tarda un instante; se espera antes de decidir que el enlace no sirve.
  useEffect(() => {
    const t = setTimeout(() => setWaited(true), 2500);
    return () => clearTimeout(t);
  }, []);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<PasswordValues>({ resolver: zodResolver(passwordSchema), defaultValues: { password: '', confirm: '' } });

  const onSubmit = handleSubmit(async ({ password }) => {
    setFormError(null);
    try {
      await updatePassword(password);
      setDone(true);
      setTimeout(() => navigate('/perfil', { replace: true }), 1500);
    } catch (e) {
      setFormError(e instanceof AuthError ? e.message : 'No pudimos cambiar tu contraseña.');
    }
  });

  const ready = recovering || status === 'authenticated';
  if (!ready && !waited) return <LoadingScreen />;

  return (
    <>
      <title>Nueva contraseña · PCP Shooting</title>
      <AuthShell>
        <AuthHeading title="Crea una nueva contraseña" lead="Usa al menos 8 caracteres combinando letras y números." />
        {!ready ? (
          <div className={styles.success}>
            <FormAlert>El enlace no es válido o ya expiró.</FormAlert>
            <p>Los enlaces de recuperación son de un solo uso y vencen pronto. Solicita uno nuevo.</p>
            <Link to="/recuperar" className={styles.back}>
              Solicitar otro enlace
            </Link>
          </div>
        ) : done ? (
          <FormAlert tone="success">Tu contraseña se actualizó. Te llevamos a tu perfil…</FormAlert>
        ) : (
          <form className={styles.form} onSubmit={onSubmit} noValidate>
            {formError && <FormAlert>{formError}</FormAlert>}
            <PasswordField label="Nueva contraseña" autoComplete="new-password" error={errors.password?.message} {...register('password')} />
            <PasswordField label="Confirma la contraseña" autoComplete="new-password" error={errors.confirm?.message} {...register('confirm')} />
            <Button type="submit" size="lg" fullWidth loading={isSubmitting}>
              Guardar contraseña
            </Button>
          </form>
        )}
      </AuthShell>
    </>
  );
}
