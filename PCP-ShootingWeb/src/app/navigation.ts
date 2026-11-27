import {
  IconBag,
  IconBox,
  IconCalendar,
  IconChart,
  IconClipboard,
  IconChat,
  IconCrown,
  IconCup,
  IconDocument,
  IconGrid,
  IconGear,
  IconHome,
  IconShirt,
  IconStar,
  IconTarget,
  IconTicket,
  IconUser,
  IconUsers,
  type IconComponent,
} from '@/components/icons/icons';
import type { Role } from '@/app/auth';

export type NavItem = { label: string; to: string };
export type IconNavItem = NavItem & { icon: IconComponent };
export type AdminNavItem = IconNavItem & { roles: Role[]; end?: boolean };

/** Datos de contacto del club (CLAUDE.md §1). */
export const club = {
  name: 'PCP Shooting',
  address: 'Km 23 Ruta de los Cenotes, Quintana Roo',
  hours: 'Martes a domingo, 8:00 a 18:00 · lunes cerrado',
  phone: '998 159 6699',
  phoneHref: 'tel:+529981596699',
  whatsappHref: 'https://wa.me/529981596699',
  email: 'contacto@pcpshooting.com',
  mapsHref: 'https://maps.google.com/?q=PcP+Shooting+Ruta+de+los+Cenotes',
  mapsEmbed: 'https://www.google.com/maps?q=PcP+Shooting+Ruta+de+los+Cenotes&output=embed',
  social: [
    { label: 'Instagram', href: 'https://www.instagram.com/pcpshooting/' },
    { label: 'Facebook', href: 'https://www.facebook.com/profile.php?id=61594533773594' },
    { label: 'TikTok', href: 'https://www.tiktok.com/@pcp.shooting' },
    { label: 'YouTube', href: 'https://www.youtube.com/@pcpshooting' },
  ],
  privacyHref: 'https://pcpshooting.com/politicas-y-privacidad/',
  rulesHref: 'https://pcpshooting.com/reglamento-campo-de-tiro-y-uso-de-armas/',
} as const;

/** Navegación central del header de escritorio. */
export const mainNav: NavItem[] = [
  { label: 'Catálogo', to: '/catalogo' },
  { label: 'Eventos', to: '/eventos' },
  { label: 'Membresía', to: '/membresias' },
  { label: 'Tienda', to: '/tienda' },
  { label: 'Blog', to: '/blog' },
  { label: 'Contacto', to: '/contacto' },
];

/** Menú lateral (drawer). "Alimentos y bebidas" no está en el diseño: se agregó por alcance. */
export const sideMenu: { heading: string; items: NavItem[] }[] = [
  {
    heading: 'Reserva y juega',
    items: [
      { label: 'Reservar línea de tiro', to: '/reservar' },
      { label: 'Modos y actividades', to: '/nosotros#modos' },
      { label: 'Eventos y horarios', to: '/eventos' },
      { label: 'Membresía', to: '/membresias' },
    ],
  },
  {
    heading: 'Armas y tienda',
    items: [
      { label: 'Catálogo de armas', to: '/catalogo' },
      { label: 'Tienda', to: '/tienda' },
      { label: 'Alimentos y bebidas', to: '/alimentos' },
    ],
  },
  {
    heading: 'El club',
    items: [
      { label: 'Sobre nosotros', to: '/nosotros' },
      { label: 'Galería', to: '/galeria' },
      { label: 'Blog', to: '/blog' },
      { label: 'Contacto', to: '/contacto' },
    ],
  },
];

/** Barra inferior de pestañas (celular). */
export const tabBar: IconNavItem[] = [
  { label: 'Reservar', to: '/reservar', icon: IconTarget },
  { label: 'Eventos', to: '/eventos', icon: IconStar },
  { label: 'Tienda', to: '/tienda', icon: IconShirt },
  { label: 'Socios', to: '/membresias', icon: IconCrown },
  { label: 'Perfil', to: '/perfil', icon: IconUser },
];

/** Columnas del footer. */
export const footerNav: { heading: string; items: NavItem[] }[] = [
  {
    heading: 'El club',
    items: [
      { label: 'Sobre nosotros', to: '/nosotros' },
      { label: 'Modos y actividades', to: '/nosotros#modos' },
      { label: 'Galería', to: '/galeria' },
      { label: 'Membresía', to: '/membresias' },
      { label: 'Eventos y horarios', to: '/eventos' },
    ],
  },
  {
    heading: 'En línea',
    items: [
      { label: 'Reservar línea de tiro', to: '/reservar' },
      { label: 'Catálogo de armas', to: '/catalogo' },
      { label: 'Tienda', to: '/tienda' },
      { label: 'Alimentos y bebidas', to: '/alimentos' },
      { label: 'Blog', to: '/blog' },
      { label: 'Mi perfil', to: '/perfil' },
    ],
  },
];

const STAFF: Role[] = ['admin', 'recepcion'];
const ADMIN: Role[] = ['admin'];

/** Secciones del panel. `roles` define quién las ve (CLAUDE.md §5). */
export const adminNav: AdminNavItem[] = [
  { label: 'Inicio', to: '/admin', icon: IconHome, roles: STAFF, end: true },
  { label: 'Reservas', to: '/admin/reservas', icon: IconCalendar, roles: STAFF },
  { label: 'Pedidos', to: '/admin/pedidos', icon: IconBag, roles: STAFF },
  { label: 'Alimentos', to: '/admin/alimentos', icon: IconCup, roles: STAFF },
  { label: 'Productos', to: '/admin/productos', icon: IconBox, roles: ADMIN },
  { label: 'Paquetes', to: '/admin/paquetes', icon: IconTicket, roles: ADMIN },
  { label: 'Inventario', to: '/admin/inventario', icon: IconClipboard, roles: ADMIN },
  { label: 'Eventos', to: '/admin/eventos', icon: IconStar, roles: ADMIN },
  { label: 'Clientes y socios', to: '/admin/clientes', icon: IconUsers, roles: ADMIN },
  { label: 'Reportes', to: '/admin/reportes', icon: IconChart, roles: ADMIN },
  { label: 'Contenido', to: '/admin/contenido', icon: IconDocument, roles: ADMIN },
  { label: 'Portadas', to: '/admin/portadas', icon: IconGrid, roles: ADMIN },
  { label: 'Mensajes', to: '/admin/contacto', icon: IconChat, roles: ADMIN },
  { label: 'Configuración', to: '/admin/configuracion', icon: IconGear, roles: ADMIN },
];

/** Pestañas fijas del panel en celular (la quinta es "Más", que abre el menú). */
export const adminTabBar = ['/admin', '/admin/reservas', '/admin/pedidos', '/admin/productos'];
