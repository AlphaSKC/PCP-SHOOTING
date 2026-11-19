import logoWhite from '@/assets/logo-pcp-blanco.png';
import { ButtonLink } from '@/components/ui';
import { IconQr } from '@/components/icons/icons';
import type { SessionUser } from '@/features/auth/types';
import { cx, formatDate } from '@/lib/format';
import styles from './PerfilPage.module.css';

/** Credencial de socio CARTAQ (o invitación a hacerse socio). */
export function MembershipCard({ user }: { user: SessionUser }) {
  const m = user.membership;

  if (!m) {
    return (
      <div id="membresia" className={cx(styles.member, styles.memberEmpty)}>
        <div className={styles.memberText}>
          <img src={logoWhite} alt="" className={styles.memberLogo} width={65} height={36} />
          <span className={styles.memberPlanMuted}>Sin membresía</span>
          <span className={styles.memberName}>Hazte socio CARTAQ</span>
          <span className={styles.memberMeta}>Beneficios y precios especiales en el club.</span>
        </div>
        <ButtonLink to="/membresias" variant="ghost">
          Ver planes
        </ButtonLink>
      </div>
    );
  }

  return (
    <div id="membresia" className={cx(styles.member, m.gold && styles.memberGold)}>
      <div className={styles.memberText}>
        <img src={logoWhite} alt="" className={styles.memberLogo} width={65} height={36} />
        <span className={styles.memberPlan}>{m.plan}</span>
        <span className={styles.memberName}>{user.name}</span>
        <span className={styles.memberMeta}>
          <span className={styles.desktopInline}>Socio n.º</span>
          <span className={styles.mobileInline}>N.º</span> {m.number} ·{' '}
          <span className={styles.desktopInline}>Vigente hasta</span>
          <span className={styles.mobileInline}>Vence</span> {formatDate(m.validUntil)}
        </span>
      </div>
      {/* TODO: QR real de la credencial (lo genera la API; se valida en recepción). */}
      <div className={styles.qr} role="img" aria-label={`Código QR de socio n.º ${m.number}`}>
        <IconQr size={56} strokeWidth={1.6} />
      </div>
    </div>
  );
}
