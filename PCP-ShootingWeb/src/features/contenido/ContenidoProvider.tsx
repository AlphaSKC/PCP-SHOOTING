import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { club as inicial } from '@/app/navigation';
import { DATA_SOURCE } from '@/lib/api';
import { leerContenido, type ContenidoSitio } from './api';

type Club = Omit<typeof inicial, 'address' | 'hours' | 'phone' | 'phoneHref' | 'whatsappHref' | 'email' | 'mapsHref' | 'mapsEmbed' | 'social'> & {
  address: string; hours: string; phone: string; phoneHref: string; whatsappHref: string; email: string; mapsHref: string; mapsEmbed: string;
  social: readonly { label: string; href: string }[];
};
const Contexto = createContext<{ contenido: ContenidoSitio | null; club: Club; actualizar: (datos?: ContenidoSitio) => void }>({ contenido: null, club: inicial, actualizar: () => {} });

export function ContenidoProvider({ children }: { children: ReactNode }) {
  const [contenido, setContenido] = useState<ContenidoSitio | null>(null); const revision = useRef(0);
  const actualizar = useCallback((datos?: ContenidoSitio) => {
    const id = ++revision.current;
    if (datos) { setContenido(datos); return; }
    if (DATA_SOURCE === 'api') void leerContenido().then(d => { if (id === revision.current) setContenido(d); }).catch(() => { /* Conservar contenido previo o datos públicos conocidos del sitio. */ });
  }, []);
  useEffect(() => { actualizar(); return () => { revision.current++; }; }, [actualizar]);
  const club = useMemo<Club>(() => contenido ? {
    ...inicial, address: contenido.direccion, hours: contenido.horarioPublico, phone: contenido.telefono,
    phoneHref: `tel:${contenido.telefonoMarcacion}`, whatsappHref: `https://wa.me/${contenido.whatsAppNumero}`,
    email: contenido.correo, mapsHref: `https://maps.google.com/?q=${encodeURIComponent(contenido.ubicacionMapa)}`,
    mapsEmbed: `https://www.google.com/maps?q=${encodeURIComponent(contenido.ubicacionMapa)}&output=embed`,
    social: [{ label: 'Instagram', href: contenido.instagramUrl }, { label: 'Facebook', href: contenido.facebookUrl },
      { label: 'TikTok', href: contenido.tikTokUrl }, { label: 'YouTube', href: contenido.youTubeUrl }].filter((s): s is { label: string; href: string } => Boolean(s.href)),
  } : inicial, [contenido]);
  return <Contexto.Provider value={{ contenido, club, actualizar }}>{children}</Contexto.Provider>;
}
export const useContenidoSitio = () => useContext(Contexto);
export const useClub = () => useContenidoSitio().club;
