import { useId, useState, type ComponentPropsWithRef, type ReactNode } from 'react';
import { IconEye, IconEyeOff } from '@/components/icons/icons';
import { cx } from '@/lib/format';
import styles from './Field.module.css';

type FieldBaseProps = {
  label: string;
  /** Contenido a la derecha de la etiqueta (p. ej. "¿Olvidaste tu contraseña?"). */
  labelAside?: ReactNode;
  /** Texto de ayuda bajo el control. */
  hint?: ReactNode;
  /** Mensaje de error; marca el control como inválido. */
  error?: string;
  /** Oculta la etiqueta visualmente (sigue disponible para lectores de pantalla). */
  hideLabel?: boolean;
  className?: string;
};

type RenderProps = { id: string; describedBy: string | undefined; invalid: boolean };

function FieldShell({
  label,
  labelAside,
  hint,
  error,
  hideLabel,
  required,
  className,
  id: idProp,
  children,
}: FieldBaseProps & { required?: boolean; id?: string; children: (p: RenderProps) => ReactNode }) {
  const autoId = useId();
  const id = idProp ?? autoId;
  const hintId = hint ? `${id}-hint` : undefined;
  const errorId = error ? `${id}-error` : undefined;
  const describedBy = [hintId, errorId].filter(Boolean).join(' ') || undefined;

  return (
    <div className={cx(styles.field, className)}>
      <div className={cx(labelAside ? styles.labelRow : undefined)}>
        <label htmlFor={id} className={cx(styles.label, hideLabel && 'visually-hidden')}>
          {label}
          {required && (
            <span className={styles.required} aria-hidden="true">
              {' '}
              *
            </span>
          )}
        </label>
        {labelAside}
      </div>
      {children({ id, describedBy, invalid: Boolean(error) })}
      {hint && (
        <p id={hintId} className={styles.hint}>
          {hint}
        </p>
      )}
      {error && (
        <p id={errorId} className={styles.error}>
          {error}
        </p>
      )}
    </div>
  );
}

type InputProps = FieldBaseProps & Omit<ComponentPropsWithRef<'input'>, 'className'>;

/** Campo de texto con etiqueta (texto, correo, teléfono, contraseña, fecha…). */
export function TextField({ label, labelAside, hint, error, hideLabel, className, id, required, ...rest }: InputProps) {
  return (
    <FieldShell {...{ label, labelAside, hint, error, hideLabel, className, id, required }}>
      {({ id: inputId, describedBy, invalid }) => (
        <input
          id={inputId}
          className={styles.control}
          aria-describedby={describedBy}
          aria-invalid={invalid || undefined}
          required={required}
          {...rest}
        />
      )}
    </FieldShell>
  );
}

/** Contraseña con botón para mostrar/ocultar. */
export function PasswordField({ label, labelAside, hint, error, hideLabel, className, id, required, ...rest }: Omit<InputProps, 'type'>) {
  const [visible, setVisible] = useState(false);
  return (
    <FieldShell {...{ label, labelAside, hint, error, hideLabel, className, id, required }}>
      {({ id: inputId, describedBy, invalid }) => (
        <div className={styles.withButton}>
          <input
            id={inputId}
            type={visible ? 'text' : 'password'}
            className={cx(styles.control, styles.controlWithButton)}
            aria-describedby={describedBy}
            aria-invalid={invalid || undefined}
            required={required}
            {...rest}
          />
          <button
            type="button"
            className={styles.inlineButton}
            aria-label={visible ? 'Ocultar contraseña' : 'Mostrar contraseña'}
            aria-pressed={visible}
            onClick={() => setVisible((v) => !v)}
          >
            {visible ? <IconEyeOff size={20} /> : <IconEye size={20} />}
          </button>
        </div>
      )}
    </FieldShell>
  );
}

type FileFieldProps = FieldBaseProps &
  Omit<ComponentPropsWithRef<'input'>, 'className' | 'type'> & {
    /** Nombre del archivo elegido (para mostrarlo). */
    fileName?: string | null;
  };

/** Subir archivo (identificación oficial, fotos). Con Supabase se sube a Storage. */
export function FileField({ label, labelAside, hint, error, hideLabel, className, id, required, fileName, ...rest }: FileFieldProps) {
  return (
    <FieldShell {...{ label, labelAside, hint, error, hideLabel, className, id, required }}>
      {({ id: inputId, describedBy, invalid }) => (
        <div className={cx(styles.file, invalid && styles.fileInvalid)}>
          <input
            id={inputId}
            type="file"
            className={styles.fileInput}
            aria-describedby={describedBy}
            aria-invalid={invalid || undefined}
            required={required}
            {...rest}
          />
          <span className={styles.fileButton} aria-hidden="true">
            Elegir archivo
          </span>
          <span className={styles.fileName} aria-hidden="true">
            {fileName || 'Ningún archivo seleccionado'}
          </span>
        </div>
      )}
    </FieldShell>
  );
}

/** Aviso de error o éxito de un formulario completo (p. ej. "Correo o contraseña incorrectos"). */
export function FormAlert({ tone = 'error', children }: { tone?: 'error' | 'success'; children: ReactNode }) {
  return (
    <p role={tone === 'error' ? 'alert' : 'status'} className={cx(styles.alert, tone === 'success' && styles.alertSuccess)}>
      {children}
    </p>
  );
}

type SelectProps = FieldBaseProps &
  Omit<ComponentPropsWithRef<'select'>, 'className'> & {
    options: { value: string; label: string }[];
    placeholder?: string;
  };

export function SelectField({ label, hint, error, hideLabel, className, id, required, options, placeholder, ...rest }: SelectProps) {
  return (
    <FieldShell {...{ label, hint, error, hideLabel, className, id, required }}>
      {({ id: selectId, describedBy, invalid }) => (
        <select
          id={selectId}
          className={cx(styles.control, styles.select)}
          aria-describedby={describedBy}
          aria-invalid={invalid || undefined}
          required={required}
          {...rest}
        >
          {placeholder && (
            <option value="" disabled>
              {placeholder}
            </option>
          )}
          {options.map((o) => (
            <option key={o.value} value={o.value}>
              {o.label}
            </option>
          ))}
        </select>
      )}
    </FieldShell>
  );
}

type TextareaProps = FieldBaseProps & Omit<ComponentPropsWithRef<'textarea'>, 'className'>;

export function TextareaField({ label, hint, error, hideLabel, className, id, required, rows = 5, ...rest }: TextareaProps) {
  return (
    <FieldShell {...{ label, hint, error, hideLabel, className, id, required }}>
      {({ id: areaId, describedBy, invalid }) => (
        <textarea
          id={areaId}
          rows={rows}
          className={cx(styles.control, styles.textarea)}
          aria-describedby={describedBy}
          aria-invalid={invalid || undefined}
          required={required}
          {...rest}
        />
      )}
    </FieldShell>
  );
}

type CheckboxProps = Omit<ComponentPropsWithRef<'input'>, 'type' | 'className'> & {
  label: ReactNode;
  error?: string;
  className?: string;
};

/** Casilla con texto a la derecha (aceptar reglamento, recordarme…). */
export function Checkbox({ label, error, className, id: idProp, ...rest }: CheckboxProps) {
  const autoId = useId();
  const id = idProp ?? autoId;
  const errorId = error ? `${id}-error` : undefined;
  return (
    <div className={cx(styles.checkField, className)}>
      <div className={styles.checkRow}>
        <input
          id={id}
          type="checkbox"
          className={styles.checkbox}
          aria-describedby={errorId}
          aria-invalid={error ? true : undefined}
          {...rest}
        />
        <label htmlFor={id} className={styles.checkLabel}>
          {label}
        </label>
      </div>
      {error && (
        <p id={errorId} className={styles.error}>
          {error}
        </p>
      )}
    </div>
  );
}

type SwitchProps = {
  checked: boolean;
  onChange: (checked: boolean) => void;
  label: string;
  /** Muestra la etiqueta junto al interruptor; si no, solo la usa como nombre accesible. */
  showLabel?: boolean;
  disabled?: boolean;
};

/** Interruptor de encendido/apagado (configuración del panel). */
export function Switch({ checked, onChange, label, showLabel = false, disabled }: SwitchProps) {
  const button = (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      aria-label={showLabel ? undefined : label}
      disabled={disabled}
      className={cx(styles.switch, checked && styles.switchOn)}
      onClick={() => onChange(!checked)}
    >
      <span className={styles.thumb} />
    </button>
  );

  if (!showLabel) return button;

  return (
    <label className={styles.switchRow}>
      <span>{label}</span>
      {button}
    </label>
  );
}
