import type { ReactNode } from 'react';
import { useClub } from '@/features/contenido/ContenidoProvider';
import { AdminPageHeader } from '@/components/layout/AdminPageHeader';
import { ButtonAnchor, ButtonLink, EmptyState, PageHero } from '@/components/ui';
import { AdminBody } from '@/features/admin/ui';
import styles from './Pending.module.css';

/*
 * Pantallas que aún no tienen API. Regla del proyecto: en modo API NO se muestran datos de ejemplo;
 * se explica qué falta y se ofrece un camino (WhatsApp, otra sección). Lo pendiente se lista en docs/ROADMAP.md §8.
 */

type PublicProps = { pageTitle: string; eyebrow: string; title: string; lead?: string; message: string; actions?: ReactNode };

/** Página pública "pendiente": hero + aviso + contacto. */
export function PendingPublicPage({ pageTitle, eyebrow, title, lead, message, actions }: PublicProps) {
  const club = useClub();
  return (
    <>
      <title>{`${pageTitle} · PCP Shooting`}</title>
      <PageHero eyebrow={eyebrow} title={title} lead={lead} />
      <div className={`container ${styles.main}`}>
        <EmptyState
          title="Muy pronto en línea"
          description={message}
          action={
            actions ?? (
              <div className={styles.actions}>
                <ButtonAnchor href={club.whatsappHref} target="_blank" rel="noreferrer">
                  Escríbenos por WhatsApp
                </ButtonAnchor>
                <ButtonLink to="/reservar" variant="secondary">
                  Reservar línea
                </ButtonLink>
              </div>
            )
          }
        />
      </div>
    </>
  );
}

type AdminProps = { pageTitle: string; title: string; subtitle: string; message: string; missing: string[]; actions?: ReactNode };

/** Pantalla del panel "pendiente": explica qué endpoints faltan (para el equipo). */
export function PendingAdminPage({ pageTitle, title, subtitle, message, missing, actions }: AdminProps) {
  return (
    <>
      <title>{`${pageTitle} · Panel PCP`}</title>
      <AdminPageHeader title={title} subtitle={subtitle} />
      <AdminBody>
        <EmptyState
          title="Pendiente: falta la API"
          description={message}
          action={
            <div className={styles.admin}>
              <ul className={styles.missing}>
                {missing.map((m) => (
                  <li key={m}>{m}</li>
                ))}
              </ul>
              {actions}
            </div>
          }
        />
      </AdminBody>
    </>
  );
}
