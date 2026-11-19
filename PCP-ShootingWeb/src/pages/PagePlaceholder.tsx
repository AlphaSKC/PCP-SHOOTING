import { AdminPageHeader } from '@/components/layout/AdminPageHeader';
import { ButtonLink, Card, PageHero } from '@/components/ui';
import styles from './PagePlaceholder.module.css';

type Props = {
  title: string;
  /** Archivos de diseño en docs/pcp-handoff/design/fuente-dc (o "Sin diseño"). */
  design: string;
  description: string;
  notes?: string[];
};

function DesignCard({ design, notes }: Pick<Props, 'design' | 'notes'>) {
  return (
    <Card className={styles.card}>
      <span className={styles.label}>Referencia de diseño</span>
      <p className={styles.design}>{design}</p>
      {notes && notes.length > 0 && (
        <ul className={styles.notes}>
          {notes.map((note) => (
            <li key={note}>{note}</li>
          ))}
        </ul>
      )}
    </Card>
  );
}

/** Pantalla pública provisional. Se reemplaza al construir cada página. */
export function PagePlaceholder({ title, design, description, notes }: Props) {
  return (
    <>
      <title>{`${title} · PCP Shooting`}</title>
      <PageHero eyebrow="Pantalla pendiente" title={title} lead={description} />
      <div className={`container ${styles.body}`}>
        <DesignCard design={design} notes={notes} />
      </div>
    </>
  );
}

/** Pantalla del panel provisional. */
export function AdminPlaceholder({ title, design, description, notes }: Props) {
  return (
    <>
      <title>{`${title} · Panel PCP`}</title>
      <AdminPageHeader
        title={title}
        subtitle={description}
        actions={
          <ButtonLink to="/" variant="secondary">
            Ver sitio
          </ButtonLink>
        }
      />
      <div className={styles.adminBody}>
        <DesignCard design={design} notes={notes} />
      </div>
    </>
  );
}
