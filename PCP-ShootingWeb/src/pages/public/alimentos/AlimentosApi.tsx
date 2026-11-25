import { useEffect, useMemo, useState } from 'react';
import { club } from '@/app/navigation';
import { ButtonAnchor, EmptyState, PageHero, Photo, TextField } from '@/components/ui';
import { IconCup } from '@/components/icons/icons';
import { getPublicMenu, type MenuItem } from '@/features/alimentos/alimentosApi';
import { Markdown } from '@/features/blog/Markdown';
import { useAsync } from '@/features/reservas/useAsync';
import { cx, formatMXN } from '@/lib/format';
import { LoadingScreen } from '@/pages/LoadingScreen';
import styles from './AlimentosPage.module.css';

const REFRESH_MS = 3 * 60 * 1000;

function MenuCard({ item }: { item: MenuItem }) {
  return (
    <li className={cx(styles.item, styles.apiItem, item.soldOut && styles.apiSoldOut)}>
      {item.imageUrl && (
        <div className={styles.apiPhoto}>
          <Photo src={item.imageUrl} alt={item.name} />
        </div>
      )}
      <div className={styles.itemText}>
        <h3 className={styles.itemName}>{item.name}</h3>
        {item.description && <p className={styles.itemDesc}>{item.description}</p>}
        {item.notes && <Markdown className={styles.apiNotes}>{item.notes}</Markdown>}
        <div className={styles.apiFoot}>
          <strong className={styles.itemPrice}>{formatMXN(item.price)}</strong>
          {item.soldOut && <span className={styles.soldOut}>Agotado</span>}
        </div>
      </div>
    </li>
  );
}

/** Carta pública con la API (GET /api/alimentos). Solo consulta: el pedido en línea está pendiente (OQ-05). */
export function AlimentosApiPage() {
  const [refresh, setRefresh] = useState(0);
  const { data: menu, loading, error } = useAsync(getPublicMenu, [refresh]);
  const [q, setQ] = useState('');

  // La carta cambia con el inventario: se vuelve a consultar cada 3 min y al regresar a la pestaña.
  useEffect(() => {
    const timer = window.setInterval(() => setRefresh((n) => n + 1), REFRESH_MS);
    const onVisible = () => document.visibilityState === 'visible' && setRefresh((n) => n + 1);
    document.addEventListener('visibilitychange', onVisible);
    return () => {
      window.clearInterval(timer);
      document.removeEventListener('visibilitychange', onVisible);
    };
  }, []);

  const visible = useMemo(() => {
    const t = q.trim().toLowerCase();
    const list = (menu ?? []).filter((m) => !t || `${m.name} ${m.description ?? ''}`.toLowerCase().includes(t));
    return [...list].sort((a, b) => Number(a.soldOut) - Number(b.soldOut));
  }, [menu, q]);

  return (
    <>
      <title>Alimentos y bebidas · PCP Shooting</title>
      <meta name="description" content="Carta de bebidas, snacks y comida de PCP Shooting. Pide en barra durante tu visita al club." />

      <PageHero eyebrow="Alimentos y bebidas" title="Nuestra carta" lead="Bebidas, snacks y comida para tu visita. Pide en barra durante tu estancia en el club." />

      <div className={`container ${styles.apiLayout}`}>
        <div className={styles.apiInfo} role="note">
          <IconCup size={22} />
          <p>
            <strong>¿Cómo pedir?</strong> Por ahora los alimentos se piden y pagan en la barra del club. El pedido en línea llegará pronto. Precios en pesos mexicanos.
          </p>
        </div>

        <section className={styles.menu} aria-labelledby="carta">
          <div className={styles.menuHead}>
            <h2 id="carta" className={styles.menuTitle}>
              Carta
            </h2>
            {(menu?.length ?? 0) > 6 && <TextField label="Buscar en la carta" hideLabel placeholder="Buscar: agua, café, hamburguesa…" value={q} onChange={(e) => setQ(e.target.value)} />}
          </div>

          {loading && !menu ? (
            <LoadingScreen />
          ) : error && !menu ? (
            <EmptyState title="No pudimos cargar la carta" description="Intenta de nuevo en un momento." />
          ) : visible.length === 0 ? (
            <EmptyState
              title={q ? 'No encontramos ese producto' : 'La carta se publicará pronto'}
              description={q ? 'Prueba con otra palabra.' : 'Mientras tanto, pregunta en barra o por WhatsApp qué tenemos hoy.'}
              action={
                q ? undefined : (
                  <ButtonAnchor href={club.whatsappHref} target="_blank" rel="noreferrer" variant="secondary">
                    Preguntar por WhatsApp
                  </ButtonAnchor>
                )
              }
            />
          ) : (
            <ul className={styles.items}>
              {visible.map((m) => (
                <MenuCard key={m.id} item={m} />
              ))}
            </ul>
          )}
        </section>
      </div>
    </>
  );
}
