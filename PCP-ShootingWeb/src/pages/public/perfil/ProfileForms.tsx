import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Button, FormAlert, PasswordField, TextField } from '@/components/ui';
import { useAuth } from '@/features/auth/AuthProvider';
import {
  passwordSchema,
  profileSchema,
  type PasswordValues,
  type ProfileInput,
  type ProfileValues,
} from '@/features/auth/schemas';
import { AuthError, type SessionUser } from '@/features/auth/types';
import styles from './PerfilPage.module.css';

/** Datos personales (nombre, correo, teléfono). */
export function PersonalDataForm({ user }: { user: SessionUser }) {
  const { updateProfile } = useAuth();
  const [saved, setSaved] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting, isDirty },
    reset,
  } = useForm<ProfileInput, unknown, ProfileValues>({
    resolver: zodResolver(profileSchema),
    defaultValues: { nombre: user.name, email: user.email, telefono: user.phone },
  });

  const onSubmit = handleSubmit(async (values) => {
    setSaved(null);
    setFormError(null);
    const emailChanged = values.email.trim().toLowerCase() !== user.email.toLowerCase();
    try {
      const u = await updateProfile(values);
      reset({ nombre: u.name, email: u.email, telefono: u.phone });
      const msg = emailChanged
        ? `Datos guardados. Para cambiar tu correo, abre el enlace que enviamos a ${values.email}.`
        : 'Tus datos se guardaron.';
      setSaved(msg);
    } catch (e) {
      const msg = e instanceof AuthError ? e.message : 'No pudimos guardar tus datos. Intenta de nuevo.';
      setFormError(msg);
    }
  });

  return (
    <form className={styles.form} onSubmit={onSubmit} noValidate>
      {!user.profileSynced && (
        <FormAlert inline>No pudimos conectar con el servidor para leer tu perfil. Algunos datos pueden no estar actualizados.</FormAlert>
      )}
      {formError && <FormAlert>{formError}</FormAlert>}
      {saved && !isDirty && <FormAlert tone="success">{saved}</FormAlert>}
      <div className={styles.formGrid}>
        <TextField label="Nombre completo" autoComplete="name" error={errors.nombre?.message} {...register('nombre')} />
        <TextField label="Correo" type="email" autoComplete="email" error={errors.email?.message} {...register('email')} />
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
      <div>
        <Button type="submit" variant="dark" size="lg" loading={isSubmitting} disabled={!isDirty} className={styles.formButton}>
          Guardar cambios
        </Button>
      </div>
    </form>
  );
}

/** Cambiar contraseña (con Supabase: updateUser({ password })). */
export function PasswordForm() {
  const { updatePassword } = useAuth();
  const [saved, setSaved] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<PasswordValues>({ resolver: zodResolver(passwordSchema), defaultValues: { password: '', confirm: '' } });

  const onSubmit = handleSubmit(async ({ password }) => {
    setSaved(false);
    setFormError(null);
    try {
      await updatePassword(password);
      reset();
      setSaved(true);
    } catch (e) {
      const msg = e instanceof AuthError ? e.message : 'No pudimos cambiar tu contraseña. Intenta de nuevo.';
      setFormError(msg);
    }
  });

  return (
    <form className={styles.form} onSubmit={onSubmit} noValidate>
      <h3 className={styles.subTitle}>Cambiar contraseña</h3>
      {formError && <FormAlert>{formError}</FormAlert>}
      {saved && <FormAlert tone="success">Tu contraseña se actualizó.</FormAlert>}
      <div className={styles.formGrid}>
        <PasswordField
          label="Nueva contraseña"
          autoComplete="new-password"
          placeholder="Mínimo 8 caracteres"
          error={errors.password?.message}
          {...register('password')}
        />
        <PasswordField
          label="Confirma la contraseña"
          autoComplete="new-password"
          error={errors.confirm?.message}
          {...register('confirm')}
        />
      </div>
      <div>
        <Button type="submit" variant="secondary" size="lg" loading={isSubmitting} className={styles.formButton}>
          Actualizar contraseña
        </Button>
      </div>
    </form>
  );
}
