import type { ComponentPropsWithRef, ReactNode } from 'react';
import { Link, type LinkProps } from 'react-router';
import { cx } from '@/lib/format';
import styles from './Button.module.css';

export type ButtonVariant = 'primary' | 'secondary' | 'dark' | 'ghost' | 'gold' | 'text';
export type ButtonSize = 'md' | 'lg';

type StyleProps = {
  /** primary: rojo · secondary: blanco con borde · dark: negro · ghost: contorno sobre fondo negro · gold: Socio Oro · text: solo texto */
  variant?: ButtonVariant;
  /** md: 44px · lg: 52px */
  size?: ButtonSize;
  fullWidth?: boolean;
  iconStart?: ReactNode;
  iconEnd?: ReactNode;
};

function buttonClass({ variant = 'primary', size = 'md', fullWidth }: StyleProps, extra?: string) {
  return cx(styles.button, styles[variant], styles[size], fullWidth && styles.fullWidth, extra);
}

function Content({ iconStart, iconEnd, children, loading }: StyleProps & { children: ReactNode; loading?: boolean }) {
  return (
    <>
      {loading ? <span className={styles.spinner} aria-hidden="true" /> : iconStart}
      <span>{children}</span>
      {iconEnd}
    </>
  );
}

type ButtonProps = StyleProps & ComponentPropsWithRef<'button'> & { loading?: boolean };

export function Button({
  variant,
  size,
  fullWidth,
  iconStart,
  iconEnd,
  loading = false,
  className,
  children,
  disabled,
  type = 'button',
  ...rest
}: ButtonProps) {
  return (
    <button
      type={type}
      className={buttonClass({ variant, size, fullWidth }, className)}
      disabled={disabled || loading}
      aria-busy={loading || undefined}
      {...rest}
    >
      <Content iconStart={iconStart} iconEnd={iconEnd} loading={loading}>
        {children}
      </Content>
    </button>
  );
}

type ButtonLinkProps = StyleProps & Omit<LinkProps, 'className'> & { className?: string };

/** Mismo aspecto que Button, pero navega con React Router. */
export function ButtonLink({ variant, size, fullWidth, iconStart, iconEnd, className, children, ...rest }: ButtonLinkProps) {
  return (
    <Link className={buttonClass({ variant, size, fullWidth }, className)} {...rest}>
      <Content iconStart={iconStart} iconEnd={iconEnd}>
        {children as ReactNode}
      </Content>
    </Link>
  );
}

type ButtonAnchorProps = StyleProps & ComponentPropsWithRef<'a'> & { href: string };

/** Mismo aspecto que Button para enlaces externos (WhatsApp, tel:, mailto:). */
export function ButtonAnchor({ variant, size, fullWidth, iconStart, iconEnd, className, children, ...rest }: ButtonAnchorProps) {
  return (
    <a className={buttonClass({ variant, size, fullWidth }, className)} {...rest}>
      <Content iconStart={iconStart} iconEnd={iconEnd}>
        {children}
      </Content>
    </a>
  );
}
