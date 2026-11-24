import { useEffect, useState } from 'react';
import { useClub } from '@/features/contenido/ContenidoProvider';
import { Button, ButtonAnchor, ButtonLink, CtaBanner, EmptyState, PageHero, Photo } from '@/components/ui';
import type { Rifle } from '@/data/rifles';
import { getRifles } from '@/features/catalogo/api';
import { cx, formatMXN } from '@/lib/format';
import styles from './CatalogoPage.module.css';

const quoteHref = (r: Rifle) => `/contacto?motivo=cotizacion&modelo=${encodeURIComponent(r.name)}`;

function RifleCard({ rifle, index }: { rifle: Rifle; index: number }) {
  const specs = [{ label: 'Calibres', value: rifle.calibers }, ...rifle.specs];
  return (
    <article className={cx(styles.rifle, index % 2 === 1 && styles.reversed)} aria-labelledby={`rifle-${rifle.id}`}>
      <div className={styles.photo}>
        <Photo src={rifle.imageUrl} alt={`Foto: ${rifle.name}`} />
      </div>
      <div className={styles.body}>
        <span className={styles.kicker}>
          {String(index + 1).padStart(2, '0')} · {rifle.type}
        </span>
        <h2 id={`rifle-${rifle.id}`} className={styles.name}>
          {rifle.name}
        </h2>
        <p className={styles.description}>
          <span className={styles.desktopOnly}>{rifle.description}</span>
          <span className={styles.mobileOnly}>{rifle.short}</span>
        </p>
        <dl className={styles.specs}>
          {specs.map((s) => (
            <div key={s.label} className={styles.spec}>
              <dt>{s.label}</dt>
              <dd>{s.value}</dd>
            </div>
          ))}
        </dl>
        <div className={styles.actions}>
          <ButtonLink to={quoteHref(rifle)}>Solicitar cotización</ButtonLink>
          <ButtonLink to="/reservar" variant="secondary">
            Probarlo en el campo
          </ButtonLink>
        </div>
      </div>
    </article>
  );
}

export default function CatalogoPage() {
  const club = useClub();
  const [rifles, setRifles] = useState<Rifle[]>([]);
  const [status, setStatus] = useState<'loading' | 'ready' | 'error'>('loading');
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    let alive = true;
    setStatus('loading');
    getRifles()
      .then((list) => {
        if (!alive) return;
        setRifles(list);
        setStatus('ready');
      })
      .catch(() => alive && setStatus('error'));
    return () => {
      alive = false;
    };
  }, [attempt]);

  return (
    <>
      <title>Catálogo de armas · PCP Shooting</title>
      <meta
        name="description"
        content="Rifles de aire comprimido (PCP) en calibres .22, .25 y .30. Solicita tu cotización o pruébalos en el campo de tiro."
      />

      <PageHero
        eyebrow="Catálogo de armas"
        title="¿Ya tienes un arma? Encuentra la tuya"
        lead="Rifles de aire comprimido (PCP) para tiro deportivo y recreativo. Todos los modelos disponibles en calibres .22, .25 y .30, con energía máxima de 140 joules."
      >
        <ul className={styles.tags} aria-label="Resumen del catálogo">
          {status === 'ready' && rifles.length > 0 && <li>{rifles.length} modelos</li>}
          <li>Calibres .22 · .25 · .30</li>
          <li>Pruébalos en el campo</li>
        </ul>
      </PageHero>

      <div className={`container ${styles.main}`}>
        {status === 'error' ? (
          <EmptyState
            title="No pudimos cargar el catálogo"
            description="Revisa tu conexión e intenta de nuevo."
            action={<Button onClick={() => setAttempt((n) => n + 1)}>Reintentar</Button>}
          />
        ) : status === 'loading' ? (
          <EmptyState title="Cargando catálogo…" />
        ) : rifles.length === 0 ? (
          <EmptyState
            title="Estamos actualizando el catálogo"
            description="Escríbenos y te compartimos los modelos disponibles."
            action={<ButtonLink to="/contacto?motivo=cotizacion">Solicitar información</ButtonLink>}
          />
        ) : (
          <>
          <div className={styles.list}>
            {rifles.map((r, i) => (
              <RifleCard key={r.id} rifle={r} index={i} />
            ))}
          </div>

          <section className={styles.compare} aria-labelledby="compara-titulo">
            <h2 id="compara-titulo" className={styles.compareTitle}>
              Compara modelos
            </h2>
            <div className={styles.tableWrap}>
              <table className={styles.table}>
                <thead>
                  <tr>
                    <th scope="col">Modelo</th>
                    <th scope="col">Calibres</th>
                    <th scope="col">Modo de disparo</th>
                    <th scope="col">Destaca por</th>
                    <th scope="col">Precio</th>
                  </tr>
                </thead>
                <tbody>
                  {rifles.map((r) => (
                    <tr key={r.id}>
                      <th scope="row">{r.name}</th>
                      <td>{r.calibers}</td>
                      <td className={cx(!r.firingMode && styles.muted)}>{r.firingMode ?? 'Consultar'}</td>
                      <td>{r.highlight}</td>
                      <td className={styles.price}>{r.price != null ? formatMXN(r.price) : 'Por cotizar'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
          </>
        )}

        <CtaBanner
          title="¿No sabes cuál elegir?"
          text="Reserva una sesión, prueba los modelos en nuestros 15 puestos de tiro y decide con un experto a tu lado."
          textMobile="Pruébalos en nuestras 15 líneas de tiro."
          actions={
            <>
              <ButtonLink to="/reservar" size="lg">
                Reservar sesión
              </ButtonLink>
              <ButtonAnchor
                href={club.whatsappHref}
                target="_blank"
                rel="noreferrer"
                size="lg"
                variant="ghost"
                className={styles.desktopOnlyFlex}
              >
                Hablar con un asesor
              </ButtonAnchor>
            </>
          }
        />
      </div>
    </>
  );
}
