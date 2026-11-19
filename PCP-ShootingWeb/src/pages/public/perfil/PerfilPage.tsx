import { Link, useLocation, useNavigate } from 'react-router';
import { Button, ButtonLink, StatusBadge } from '@/components/ui';
import { IconCheck } from '@/components/icons/icons';
import { useAuth } from '@/features/auth/AuthProvider';
import { firstNameOf, initialsOf, isStaff, ROLE_LABEL } from '@/features/auth/types';
import { myEvents, myOrders, myReservations, myWaiver, type MyReservation } from '@/data/profile';
import { cx, dateTile, formatDate, formatLongDay } from '@/lib/format';
import { MembershipCard } from './MembershipCard';
import { PasswordForm, PersonalDataForm } from './ProfileForms';
import { ProfileSection } from './ProfileSection';
import styles from './PerfilPage.module.css';

const NAV = [
  { hash: '', label: 'Resumen' },
  { hash: '#reservas', label: 'Mis reservas' },
  { hash: '#membresia', label: 'Membresía' },
  { hash: '#eventos', label: 'Mis eventos' },
  { hash: '#pedidos', label: 'Pedidos de la tienda' },
  { hash: '#carta', label: 'Carta responsiva' },
  { hash: '#datos', label: 'Datos y contraseña' },
];

const linesText = (lines: number[]) =>
  lines.length === 1 ? `Línea ${lines[0]}` : `Líneas ${lines.slice(0, -1).join(', ')} y ${lines[lines.length - 1]}`;

function ReservationRow({ r }: { r: MyReservation }) {
  const tile = dateTile(r.date);
  return (
    <li className={styles.reservation}>
      <span className={styles.dateTile} aria-hidden="true">
        <span>{tile.month}</span>
        <strong>{tile.day}</strong>
      </span>
      <div className={styles.resText}>
        <span className={styles.resTitle}>
          {r.activity} · {r.people} <span className={styles.desktopInline}>personas</span>
          <span className={styles.mobileInline}>pers.</span>
        </span>
        <span className={styles.resDetail}>
          <span className={styles.desktopInline}>{formatLongDay(r.date)} · </span>
          {r.time} · {linesText(r.lines)}
          <span className={styles.desktopInline}> · {r.equipment === 'renta' ? 'Renta de rifle' : 'Rifle propio'}</span>
        </span>
      </div>
      <StatusBadge status={r.status} />
      <ButtonLink to={`/reservar/confirmacion/${r.id}`} variant="secondary" className={styles.resButton}>
        Ver QR
      </ButtonLink>
    </li>
  );
}

export default function PerfilPage() {
  const { user, signOut } = useAuth();
  const { hash } = useLocation();
  const navigate = useNavigate();
  if (!user) return null; // RequireAuth garantiza la sesión.

  const m = user.membership;
  const roleText = user.role === 'cliente' ? m?.plan : ROLE_LABEL[user.role];
  const handleSignOut = () => void signOut().then(() => navigate('/', { replace: true }));

  return (
    <>
      <title>Mi perfil · PCP Shooting</title>

      <div className={`container ${styles.layout}`}>
        {/* Barra lateral (escritorio y tableta) */}
        <aside className={styles.sidebar}>
          <div className={styles.who}>
            <span className={styles.avatar} aria-hidden="true">
              {initialsOf(user.name)}
            </span>
            <span className={styles.whoText}>
              <span className={styles.whoName}>{user.name}</span>
              {roleText && <span className={cx(styles.whoRole, m?.gold && styles.whoGold)}>{roleText}</span>}
            </span>
          </div>
          <nav aria-label="Secciones del perfil" className={styles.sideNav}>
            {NAV.map((item) => {
              const active = (hash || '') === item.hash;
              return (
                <a
                  key={item.label}
                  href={item.hash || '#top'}
                  className={cx(styles.sideLink, active && styles.sideLinkActive)}
                  aria-current={active ? 'location' : undefined}
                  onClick={(e) => {
                    e.preventDefault();
                    navigate({ hash: item.hash }, { replace: true });
                    if (!item.hash) window.scrollTo({ top: 0, behavior: 'smooth' });
                  }}
                >
                  {item.label}
                </a>
              );
            })}
            {isStaff(user.role) && (
              <Link to="/admin" className={styles.sideLink}>
                Panel administrativo
              </Link>
            )}
            <button type="button" className={styles.sideLogout} onClick={handleSignOut}>
              Cerrar sesión
            </button>
          </nav>
        </aside>

        <div className={styles.content}>
          {/* Celular: franja negra con saludo y credencial */}
          <div className={styles.top}>
            <div className={styles.heading}>
              <div className={styles.headingText}>
                <span className={styles.eyebrow}>Mi perfil</span>
                <h1 className={styles.hello}>Hola, {firstNameOf(user.name)}</h1>
              </div>
              <ButtonLink to="/reservar" size="lg" className={styles.desktopFlex}>
                Nueva reserva
              </ButtonLink>
            </div>
            <MembershipCard user={user} />
          </div>

          <section id="carta" className={cx(styles.card, styles.waiver)} aria-labelledby="carta-titulo">
            <h2 id="carta-titulo" className={styles.sectionTitle}>
              Carta responsiva
            </h2>
            <span className={styles.signed}>
              <IconCheck size={16} strokeWidth={3} /> Firmada
            </span>
            <p className={styles.muted}>Firmada el {formatDate(myWaiver.signedAt)}. Vigente para todas tus reservas.</p>
            {/* El mecanismo legal de la carta está pendiente (CLAUDE.md §8). */}
            <Button variant="text" disabled className={styles.alignStart} title="Disponible próximamente">
              Descargar PDF
            </Button>
          </section>

          <section id="reservas" className={cx(styles.card, styles.reservations)} aria-labelledby="reservas-titulo">
            <div className={styles.sectionHeadAlways}>
              <h2 id="reservas-titulo" className={styles.sectionTitle}>
                Próximas reservas
              </h2>
              <Link to="/reservar" className={styles.mobileInlineFlex}>
                Nueva
              </Link>
              {/* TODO: historial completo (GET /api/reservas/mias?historial). */}
              <a href="#reservas" className={styles.desktopInlineFlex}>
                Ver historial
              </a>
            </div>
            {myReservations.length === 0 ? (
              <p className={styles.muted}>
                Aún no tienes reservas. <Link to="/reservar">Reserva tu línea de tiro</Link>.
              </p>
            ) : (
              <ul className={styles.resList}>
                {myReservations.map((r) => (
                  <ReservationRow key={r.id} r={r} />
                ))}
              </ul>
            )}
          </section>

          <div className={styles.list}>
            <ProfileSection
              id="eventos"
              title="Mis eventos"
              summary={<span className={styles.countText}>{myEvents.length} inscrito{myEvents.length === 1 ? '' : 's'}</span>}
              className={styles.events}
            >
              <ul className={styles.items}>
                {myEvents.map((e) => (
                  <li key={e.id} className={styles.item}>
                    <span className={styles.itemText}>
                      <strong>{e.name}</strong>
                      <span className={styles.muted}>
                        {formatDate(e.date)} · {e.time}
                      </span>
                    </span>
                    <span className={styles.enrolled}>Inscrito</span>
                  </li>
                ))}
              </ul>
              <Link to="/eventos" className={styles.cardLink}>
                Ver próximos eventos
              </Link>
            </ProfileSection>

            <ProfileSection
              id="pedidos"
              title="Pedidos de la tienda"
              summary={myOrders[0] && <StatusBadge status={myOrders[0].status} />}
              className={styles.orders}
            >
              <ul className={styles.items}>
                {myOrders.map((o) => (
                  <li key={o.id} className={styles.item}>
                    <span className={styles.itemText}>
                      <strong>Pedido #{o.id}</strong>
                      <span className={styles.muted}>{o.items}</span>
                    </span>
                    <StatusBadge status={o.status} />
                  </li>
                ))}
              </ul>
              <Link to="/tienda" className={styles.cardLink}>
                Ir a la tienda
              </Link>
            </ProfileSection>

            <ProfileSection
              id="carta-movil"
              title="Carta responsiva"
              summary={<StatusBadge status={myWaiver.status} />}
              mobileOnly
            >
              <p className={styles.muted}>Firmada el {formatDate(myWaiver.signedAt)}. Vigente para todas tus reservas.</p>
            </ProfileSection>

            <ProfileSection id="membresia-detalle" title="Membresía" mobileOnly>
              {m ? (
                <p className={styles.muted}>
                  Plan {m.plan}, vigente hasta el {formatDate(m.validUntil)}. La renovación es manual: cuando venza,
                  renueva desde <Link to="/membresias">Membresías</Link>.
                </p>
              ) : (
                <p className={styles.muted}>
                  Aún no eres socio. <Link to="/membresias">Conoce los planes CARTAQ</Link>.
                </p>
              )}
            </ProfileSection>

            <ProfileSection id="datos" title="Datos y contraseña" className={styles.data}>
              <h3 className={cx(styles.subTitle, styles.desktopBlock)}>Datos personales</h3>
              <PersonalDataForm user={user} />
              <hr className={styles.divider} />
              <PasswordForm />
            </ProfileSection>
          </div>

          <div className={styles.mobileActions}>
            <ButtonLink to="/reservar" size="lg" fullWidth>
              Nueva reserva
            </ButtonLink>
            <Button variant="secondary" size="lg" fullWidth onClick={handleSignOut}>
              Cerrar sesión
            </Button>
          </div>
        </div>
      </div>
    </>
  );
}
