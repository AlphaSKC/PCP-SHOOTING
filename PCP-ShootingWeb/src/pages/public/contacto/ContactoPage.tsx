import { useSearchParams } from 'react-router';
import { club } from '@/app/navigation';
import { ButtonAnchor, PageHero } from '@/components/ui';
import { IconChat, IconClock, IconMail, IconPhone, IconPin, type IconComponent } from '@/components/icons/icons';
import { ContactForm } from '@/features/contacto/ContactForm';
import { CONTACT_REASONS, type ContactInput } from '@/features/contacto/schemas';
import styles from './ContactoPage.module.css';

const info: { icon: IconComponent; label: string; value: string; href?: string }[] = [
  { icon: IconPin, label: 'Dirección', value: club.address, href: club.mapsHref },
  { icon: IconClock, label: 'Horario', value: club.hours },
  { icon: IconPhone, label: 'Teléfono', value: club.phone, href: club.phoneHref },
  { icon: IconMail, label: 'Correo', value: club.email, href: `mailto:${club.email}` },
];

/** Lee ?motivo=cotizacion&modelo=Sidewinder (desde el catálogo) para llenar el formulario. */
function useFormDefaults(): Partial<ContactInput> {
  const [params] = useSearchParams();
  const motivo = params.get('motivo');
  const modelo = params.get('modelo');
  const validMotivo = CONTACT_REASONS.find((r) => r.value === motivo)?.value;
  return {
    ...(validMotivo && { motivo: validMotivo }),
    ...(modelo && { mensaje: `Hola, me interesa cotizar el rifle ${modelo}.` }),
  };
}

export default function ContactoPage() {
  const defaults = useFormDefaults();

  return (
    <>
      <title>Contacto · PCP Shooting</title>
      <meta
        name="description"
        content="Reservaciones, eventos para grupos, compras o dudas sobre rifles PCP. Km 23 Ruta de los Cenotes, Quintana Roo. Tel. 998 159 6699."
      />

      <div className={styles.heroWrap}>
        <PageHero
          eyebrow="Contacto"
          title="Hablemos"
          lead="Reservaciones, eventos para grupos, compras o dudas sobre rifles PCP. Te respondemos lo antes posible."
        />
      </div>

      <div className={`container ${styles.layout}`}>
        {/* Celular: accesos rápidos arriba (MContacto). */}
        <div className={styles.quick}>
          <ButtonAnchor href={club.whatsappHref} target="_blank" rel="noreferrer" variant="dark" size="lg">
            WhatsApp
          </ButtonAnchor>
          <ButtonAnchor href={club.phoneHref} variant="secondary" size="lg">
            Llamar
          </ButtonAnchor>
        </div>

        <section className={styles.formCard} aria-labelledby="mensaje-titulo">
          <h2 id="mensaje-titulo" className={styles.cardTitle}>
            Envíanos un mensaje
          </h2>
          <ContactForm defaults={defaults} />
        </section>

        <ul className={styles.info} aria-label="Datos de contacto">
          {info.map(({ icon: Icon, label, value, href }) => (
            <li key={label} className={styles.infoItem}>
              <span className={styles.infoIcon} aria-hidden="true">
                <Icon size={20} />
              </span>
              <span className={styles.infoText}>
                <span className={styles.infoLabel}>{label}</span>
                {href ? (
                  <a href={href} className={styles.infoValue} {...(href.startsWith('http') && { target: '_blank', rel: 'noreferrer' })}>
                    {value}
                  </a>
                ) : (
                  <span className={styles.infoValue}>{value}</span>
                )}
              </span>
            </li>
          ))}
        </ul>

        <div className={styles.channels}>
          <ButtonAnchor
            href={club.whatsappHref}
            target="_blank"
            rel="noreferrer"
            variant="dark"
            size="lg"
            fullWidth
            iconStart={<IconChat size={22} />}
            className={styles.whatsapp}
          >
            Escríbenos por WhatsApp
          </ButtonAnchor>
          <div className={styles.social}>
            {club.social.map((s) => (
              <ButtonAnchor key={s.label} href={s.href} target="_blank" rel="noreferrer" variant="secondary">
                {s.label}
              </ButtonAnchor>
            ))}
          </div>
        </div>

        <section className={styles.map} aria-label="Mapa de ubicación">
          <iframe
            title="Mapa de Google: PCP Shooting, Ruta de los Cenotes"
            src={club.mapsEmbed}
            loading="lazy"
            referrerPolicy="no-referrer-when-downgrade"
            className={styles.mapFrame}
          />
          <ButtonAnchor href={club.mapsHref} target="_blank" rel="noreferrer" size="lg" className={styles.directions}>
            Cómo llegar
          </ButtonAnchor>
        </section>
      </div>
    </>
  );
}
