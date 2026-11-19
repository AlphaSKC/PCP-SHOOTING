import { useMemo, useState } from 'react';
import { club } from '@/app/navigation';
import { ButtonAnchor, Chip, ChipGroup, CtaBanner, EmptyState, PageHero, Photo } from '@/components/ui';
import { galleryCategories, galleryItems, type GalleryCategory, type GalleryItem } from '@/data/gallery';
import { cx } from '@/lib/format';
import { Lightbox } from './Lightbox';
import styles from './GaleriaPage.module.css';

const labelOf = (item: GalleryItem) => galleryCategories.find((c) => c.id === item.category)?.label ?? '';
const social = (name: string) => club.social.find((s) => s.label === name)!.href;

export default function GaleriaPage() {
  const [filter, setFilter] = useState<GalleryCategory | 'todo'>('todo');
  const [openIndex, setOpenIndex] = useState<number | null>(null);

  const items = useMemo(
    () => (filter === 'todo' ? galleryItems : galleryItems.filter((g) => g.category === filter)),
    [filter],
  );

  const filters = (
    <ChipGroup label="Filtrar galería">
      {galleryCategories.map((c) => (
        <Chip key={c.id} selected={filter === c.id} onClick={() => setFilter(c.id)}>
          {c.label}
        </Chip>
      ))}
    </ChipGroup>
  );

  return (
    <>
      <title>Galería · PCP Shooting</title>
      <meta name="description" content="Fotos y videos del campo de tiro PCP Shooting en Quintana Roo, torneos y eventos." />

      <PageHero
        tone="light"
        eyebrow="Galería"
        title="Así se vive el campo"
        lead="Fotos y videos del campo de tiro en Quintana Roo, torneos y eventos."
        hideLeadOnMobile
        aside={<div className={styles.desktopFilters}>{filters}</div>}
      />

      <div className={`container ${styles.main}`}>
        <div className={styles.mobileFilters}>{filters}</div>

        {items.length === 0 ? (
          <EmptyState title="Aún no hay fotos en esta categoría" description="Vuelve pronto: subimos fotos de cada evento." />
        ) : (
          <ul className={styles.grid} aria-label="Fotos y videos">
            {items.map((item, i) => (
              <li key={item.id} className={cx(styles.cell, item.featured && styles.featured)}>
                <button
                  type="button"
                  className={styles.tile}
                  aria-label={`Abrir ${item.kind === 'video' ? 'video' : 'foto'}: ${item.title}`}
                  onClick={() => setOpenIndex(i)}
                >
                  <Photo
                    src={item.imageUrl}
                    alt={`${item.kind === 'video' ? 'Video' : 'Foto'}: ${item.title}`}
                    className={cx(item.featured && !item.imageUrl && styles.darkPlaceholder)}
                  />
                  <span className={styles.tag} aria-hidden="true">
                    {labelOf(item)}
                  </span>
                </button>
              </li>
            ))}
          </ul>
        )}

        <CtaBanner
          title="Etiquétanos en tus fotos"
          text="@pcpshooting en Instagram y TikTok"
          actions={
            <>
              <ButtonAnchor href={social('Instagram')} target="_blank" rel="noreferrer" size="lg">
                Instagram
              </ButtonAnchor>
              <ButtonAnchor
                href={social('TikTok')}
                target="_blank"
                rel="noreferrer"
                size="lg"
                variant="ghost"
                className={styles.desktopOnlyFlex}
              >
                TikTok
              </ButtonAnchor>
              <ButtonAnchor
                href={social('YouTube')}
                target="_blank"
                rel="noreferrer"
                size="lg"
                variant="ghost"
                className={styles.desktopOnlyFlex}
              >
                YouTube
              </ButtonAnchor>
            </>
          }
        />
      </div>

      <Lightbox items={items} index={openIndex} onChange={setOpenIndex} categoryLabel={labelOf} />
    </>
  );
}
