/*
 * Datos de DEMOSTRACIÓN del panel administrativo (nombres, precios y cantidades de ejemplo).
 * En producción todo viene de la API C# (CLAUDE.md §9: /api/admin/..., /api/reservas, /api/pedidos, etc.).
 */
import { PLANS } from '@/features/membresias/plans';
import { blogPosts } from '@/data/blog';
import { galleryItems } from '@/data/gallery';

export const todayISO = () => new Date().toISOString().slice(0, 10);
const addDays = (n: number) => {
  const d = new Date();
  d.setUTCDate(d.getUTCDate() + n);
  return d.toISOString().slice(0, 10);
};

export type ReservationStatus = 'pendiente_pago' | 'confirmada' | 'utilizada' | 'cancelada';
export type AdminReservation = {
  id: string;
  code: string;
  client: string;
  phone: string;
  packageId: string;
  people: number;
  date: string;
  time: string;
  line: number;
  status: ReservationStatus;
  paid: boolean;
  waiver: 'firmada' | 'pendiente';
  equipment: 'renta' | 'propio';
};

export type LineBlock = { id: string; date: string; line: number; time: string | null; reason: string };

export type OrderStatus = 'pagado' | 'preparando' | 'listo_para_recoger' | 'entregado';
export type AdminOrder = {
  id: string;
  client: string;
  phone: string;
  items: string;
  total: number;
  createdAt: string;
  status: OrderStatus;
};

export type AdminProduct = {
  id: string;
  name: string;
  category: 'Playera' | 'Gorra' | 'Taza' | 'Stickers';
  description: string;
  price: number;
  /** Existencias por talla; productos sin talla usan la clave "Única". */
  stock: Record<string, number>;
  active: boolean;
};

export type AdminRifle = { id: string; name: string; type: string; price: number | null; visible: boolean };

export type Enrollee = { name: string; phone: string; paid: boolean; waiver: 'firmada' | 'pendiente' };
export type AdminEvent = {
  id: string;
  name: string;
  type: 'torneo' | 'clinica' | 'social';
  date: string;
  start: string;
  end: string;
  capacity: number;
  price: number;
  memberPrice: number;
  description: string;
  status: 'publicado' | 'borrador';
  blockLines: boolean;
  enrollees: Enrollee[];
};

export type AdminClient = {
  id: string;
  name: string;
  email: string;
  phone: string;
  plan: string | null;
  validUntil: string | null;
  waiver: 'firmada' | 'pendiente';
  lastVisit: string | null;
};

export type Consumable = { itemId: string; perPerson: number };
export type AdminPackage = {
  id: string;
  name: string;
  description: string;
  includes: string;
  price: number;
  memberPrice: number;
  rentalAvailable: boolean;
  active: boolean;
  consumables: Consumable[];
};

export type InventoryCategory = 'producto' | 'alimento' | 'bebida' | 'consumible';
export type InventoryItem = { id: string; name: string; category: InventoryCategory; unit: string; stock: number; min: number };
export type MovementType = 'entrada' | 'salida' | 'ajuste';
export type InventoryMovement = {
  id: string;
  itemId: string;
  type: MovementType;
  qty: number;
  note: string;
  at: string;
  by: string;
};

export type FoodStatus = 'recibido' | 'en_preparacion' | 'listo' | 'entregado';
export type AdminFoodOrder = {
  id: string;
  client: string;
  items: string;
  total: number;
  where: string;
  status: FoodStatus;
  paid: boolean;
  at: string;
};

export type StaffUser = { id: string; name: string; email: string; role: 'admin' | 'recepcion'; lastAccess: string };

export type DaySchedule = { day: string; open: boolean; from: string; to: string };
export type Settings = {
  schedule: DaySchedule[];
  intervalMinutes: number;
  bookingWindowDays: number;
  activeLines: number[];
  notifyEmail: string;
  waOrderReady: string;
  waReminder: string;
  rentalAvailable: boolean;
};

export type BlogStatus = 'publicado' | 'borrador' | 'programado';
export type AdminPost = { slug: string; title: string; category: string; date: string; status: BlogStatus; body: string };
export type AdminPhoto = { id: string; title: string; album: string };

export type AdminDB = {
  version: number;
  reservations: AdminReservation[];
  blocks: LineBlock[];
  orders: AdminOrder[];
  products: AdminProduct[];
  rifles: AdminRifle[];
  events: AdminEvent[];
  clients: AdminClient[];
  packages: AdminPackage[];
  inventory: InventoryItem[];
  movements: InventoryMovement[];
  foodOrders: AdminFoodOrder[];
  staff: StaffUser[];
  settings: Settings;
  posts: AdminPost[];
  photos: AdminPhoto[];
  /** Ventas de los últimos días (para reportes de demostración). */
  sales: { date: string; reservas: number; tienda: number; alimentos: number; membresias: number; eventos: number }[];
};

const T = todayISO();
const names = ['Ana López', 'Carlos Ruiz', 'Diana Castro', 'Jorge Méndez', 'Laura Pérez', 'Roberto S.', 'Miguel Ángel R.', 'Paola G.', 'Sofía Torres'];

let n = 140;
const res = (client: string, packageId: string, people: number, time: string, line: number, status: ReservationStatus, date = T): AdminReservation => ({
  id: `R-${++n}`,
  code: `PCP-${String(n).padStart(4, '0')}`,
  client,
  phone: `998 000 ${String(n).slice(-4)}`,
  packageId,
  people,
  date,
  time,
  line,
  status,
  paid: status !== 'pendiente_pago',
  waiver: client === 'Roberto S.' ? 'pendiente' : 'firmada',
  equipment: packageId === 'recreativo' ? 'renta' : 'propio',
});

export function createSeed(): AdminDB {
  const sales = Array.from({ length: 30 }, (_, i) => {
    const date = addDays(-29 + i);
    const k = (i * 37) % 11;
    return {
      date,
      reservas: 6000 + k * 600,
      tienda: 1200 + ((i * 13) % 7) * 250,
      alimentos: 700 + ((i * 7) % 5) * 180,
      membresias: i % 6 === 0 ? 1499 : i % 4 === 0 ? 899 : 0,
      eventos: i % 9 === 0 ? 2700 : 0,
    };
  });

  return {
    version: 1,
    reservations: [
      res('Ana López', 'recreativo', 4, '9:00', 1, 'utilizada'),
      res('Ana López', 'recreativo', 4, '9:00', 2, 'utilizada'),
      res('Carlos Ruiz', 'bench-rest', 1, '10:00', 5, 'utilizada'),
      res('Carlos Ruiz', 'bench-rest', 1, '11:00', 5, 'confirmada'),
      res('Sofía Torres', 'plinking', 2, '11:00', 9, 'confirmada'),
      res('Sofía Torres', 'plinking', 2, '11:00', 10, 'confirmada'),
      res('Sofía Torres', 'plinking', 2, '11:00', 11, 'confirmada'),
      res('Sofía Torres', 'plinking', 2, '11:00', 12, 'confirmada'),
      res('Jorge Méndez', 'ipsc', 2, '12:00', 14, 'confirmada'),
      res('Laura Pérez', 'recreativo', 2, '13:00', 3, 'confirmada'),
      res('Miguel Ángel R.', 'bench-rest', 1, '14:00', 6, 'confirmada'),
      res('Paola G.', 'recreativo', 2, '14:00', 7, 'pendiente_pago'),
      res('Roberto S.', 'ipsc', 1, '15:00', 1, 'confirmada'),
      res('Diana Castro', 'plinking', 3, '16:00', 2, 'confirmada'),
      res('Laura Pérez', 'recreativo', 2, '10:00', 4, 'confirmada', addDays(1)),
    ],
    blocks: [{ id: 'B-1', date: T, line: 15, time: null, reason: 'Mantenimiento' }],
    orders: [
      { id: '1042', client: 'Ana López', phone: '998 000 0001', items: 'Playera PCP Negra (M), Gorra PCP Blanca', total: 830, createdAt: `${T}T10:12`, status: 'pagado' },
      { id: '1041', client: 'Carlos Ruiz', phone: '998 000 0002', items: 'Taza PCP ×2', total: 440, createdAt: `${T}T09:30`, status: 'preparando' },
      { id: '1040', client: 'Diana Castro', phone: '998 000 0003', items: 'Paquete de stickers ×3', total: 270, createdAt: `${addDays(-1)}T18:04`, status: 'preparando' },
      { id: '1039', client: 'Jorge Méndez', phone: '998 000 0004', items: 'Playera PCP Roja (G)', total: 450, createdAt: `${addDays(-1)}T12:40`, status: 'listo_para_recoger' },
      { id: '1038', client: 'Laura Pérez', phone: '998 000 0005', items: 'Gorra PCP Negra, Taza PCP', total: 600, createdAt: `${addDays(-2)}T16:15`, status: 'entregado' },
    ],
    products: [
      { id: 'playera-negra', name: 'Playera PCP Negra', category: 'Playera', description: 'Playera de algodón con el logo dorado de PCP Shooting.', price: 450, stock: { CH: 6, M: 12, G: 9, XG: 3 }, active: true },
      { id: 'playera-roja', name: 'Playera PCP Roja', category: 'Playera', description: 'Playera de algodón con logo blanco.', price: 450, stock: { CH: 4, M: 8, G: 6, XG: 2 }, active: true },
      { id: 'playera-blanca', name: 'Playera PCP Blanca', category: 'Playera', description: 'Playera de algodón con logo a todo color.', price: 450, stock: { CH: 2, M: 1, G: 0, XG: 1 }, active: true },
      { id: 'gorra-negra', name: 'Gorra PCP Negra', category: 'Gorra', description: 'Gorra ajustable con logo dorado.', price: 380, stock: { Única: 15 }, active: true },
      { id: 'gorra-blanca', name: 'Gorra PCP Blanca', category: 'Gorra', description: 'Gorra ajustable con logo a todo color.', price: 380, stock: { Única: 11 }, active: true },
      { id: 'taza', name: 'Taza PCP', category: 'Taza', description: 'Disponible en negro, rojo y blanco.', price: 220, stock: { Única: 24 }, active: true },
      { id: 'stickers', name: 'Paquete de stickers', category: 'Stickers', description: 'Versiones negra, blanca y roja.', price: 90, stock: { Única: 0 }, active: false },
    ],
    rifles: [
      { id: 'mordax-venom', name: 'Mordax Venom', type: 'Compacto', price: null, visible: true },
      { id: 'sidewinder', name: 'Sidewinder', type: 'Semiautomático y automático', price: null, visible: true },
      { id: 'aea-sf-standard', name: 'AEA SF Standard', type: 'Semiautomático', price: null, visible: true },
    ],
    events: [
      {
        id: 'torneo-bench-rest', name: 'Torneo de Bench Rest', type: 'torneo', date: '2027-01-23', start: '09:00', end: '14:00', capacity: 30, price: 450, memberPrice: 400,
        description: 'Competencia de precisión con rifle apoyado. Categorías por calibre.', status: 'publicado', blockLines: true,
        enrollees: [
          { name: 'Carlos Ruiz', phone: '998 000 0002', paid: true, waiver: 'firmada' },
          { name: 'Jorge Méndez', phone: '998 000 0004', paid: true, waiver: 'firmada' },
          { name: 'Miguel Ángel R.', phone: '998 000 0006', paid: false, waiver: 'firmada' },
          { name: 'Roberto S.', phone: '998 000 0007', paid: true, waiver: 'pendiente' },
        ],
      },
      { id: 'clinica-ipsc', name: 'Clínica de IPSC para principiantes', type: 'clinica', date: '2027-01-30', start: '10:00', end: '12:00', capacity: 12, price: 450, memberPrice: 400, description: 'Aprende lo básico del tiro dinámico con acompañamiento del staff.', status: 'publicado', blockLines: true, enrollees: [{ name: 'Ana López', phone: '998 000 0001', paid: true, waiver: 'firmada' }] },
      { id: 'tarde-plinking', name: 'Tarde de Plinking', type: 'social', date: '2027-02-06', start: '13:00', end: '16:00', capacity: 24, price: 450, memberPrice: 400, description: 'Blancos reactivos, retos por equipos y premios.', status: 'borrador', blockLines: false, enrollees: [] },
      { id: 'copa-pcp', name: 'Copa PCP Shooting', type: 'torneo', date: '2027-02-20', start: '09:00', end: '16:00', capacity: 40, price: 450, memberPrice: 400, description: 'Torneo abierto a socios e invitados.', status: 'publicado', blockLines: true, enrollees: [] },
    ],
    clients: [
      { id: 'c1', name: 'Ana López', email: 'ana@correo.com', phone: '998 000 0001', plan: 'Socio Oro', validUntil: '2027-01-23', waiver: 'firmada', lastVisit: T },
      { id: 'c2', name: 'Carlos Ruiz', email: 'carlos@correo.com', phone: '998 000 0002', plan: 'Socio', validUntil: addDays(12), waiver: 'firmada', lastVisit: T },
      { id: 'c3', name: 'Diana Castro', email: 'diana@correo.com', phone: '998 000 0003', plan: null, validUntil: null, waiver: 'firmada', lastVisit: addDays(-1) },
      { id: 'c4', name: 'Jorge Méndez', email: 'jorge@correo.com', phone: '998 000 0004', plan: 'Tirador', validUntil: addDays(-8), waiver: 'firmada', lastVisit: addDays(-7) },
      { id: 'c5', name: 'Roberto S.', email: 'roberto@correo.com', phone: '998 000 0007', plan: null, validUntil: null, waiver: 'pendiente', lastVisit: null },
      { id: 'c6', name: 'Laura Pérez', email: 'laura@correo.com', phone: '998 000 0005', plan: 'Socio', validUntil: addDays(90), waiver: 'firmada', lastVisit: addDays(-3) },
    ],
    packages: [
      { id: 'recreativo', name: 'Recreativo', description: 'Ideal para tu primera vez. Renta de rifle PCP, diábolos y área de tiro.', includes: 'Incluye rifle y diábolos', price: 350, memberPrice: 300, rentalAvailable: true, active: true, consumables: [{ itemId: 'diabolos-22', perPerson: 1 }, { itemId: 'blancos-papel', perPerson: 2 }] },
      { id: 'bench-rest', name: 'Bench Rest', description: 'Tiro de precisión con el rifle apoyado en mesa.', includes: 'Incluye blancos de precisión', price: 350, memberPrice: 300, rentalAvailable: true, active: true, consumables: [{ itemId: 'blancos-precision', perPerson: 3 }] },
      { id: 'plinking', name: 'Plinking', description: 'Tiro recreativo a blancos reactivos y variados.', includes: 'Incluye blancos reactivos', price: 350, memberPrice: 300, rentalAvailable: true, active: true, consumables: [{ itemId: 'diabolos-22', perPerson: 1 }] },
      { id: 'ipsc', name: 'IPSC', description: 'Tiro dinámico con desplazamientos y varios blancos.', includes: 'Incluye recorrido y blancos', price: 350, memberPrice: 300, rentalAvailable: true, active: true, consumables: [{ itemId: 'blancos-papel', perPerson: 4 }] },
    ],
    inventory: [
      { id: 'diabolos-22', name: 'Diábolos .22 (lata 500)', category: 'consumible', unit: 'latas', stock: 18, min: 10 },
      { id: 'blancos-papel', name: 'Blancos de papel', category: 'consumible', unit: 'pzas', stock: 120, min: 100 },
      { id: 'blancos-precision', name: 'Blancos de precisión', category: 'consumible', unit: 'pzas', stock: 40, min: 50 },
      { id: 'agua', name: 'Agua natural 600 ml', category: 'bebida', unit: 'pzas', stock: 36, min: 24 },
      { id: 'refresco', name: 'Refresco lata', category: 'bebida', unit: 'pzas', stock: 9, min: 24 },
      { id: 'papas', name: 'Papas fritas', category: 'alimento', unit: 'bolsas', stock: 30, min: 12 },
      { id: 'salchicha', name: 'Salchicha de pavo', category: 'alimento', unit: 'pzas', stock: 20, min: 20 },
      { id: 'inv-playera-negra', name: 'Playera PCP Negra', category: 'producto', unit: 'pzas', stock: 30, min: 10 },
      { id: 'inv-taza', name: 'Taza PCP', category: 'producto', unit: 'pzas', stock: 24, min: 6 },
    ],
    movements: [
      { id: 'M-3', itemId: 'refresco', type: 'salida', qty: 15, note: 'Ventas de la semana', at: `${addDays(-1)}T19:00`, by: 'Karla Méndez' },
      { id: 'M-2', itemId: 'diabolos-22', type: 'entrada', qty: 12, note: 'Compra a proveedor', at: `${addDays(-3)}T11:20`, by: 'Daniel Ramos' },
      { id: 'M-1', itemId: 'blancos-precision', type: 'ajuste', qty: -5, note: 'Conteo físico', at: `${addDays(-5)}T17:45`, by: 'Daniel Ramos' },
    ],
    foodOrders: [
      { id: 'AB-311', client: 'Carlos Ruiz', items: 'Café americano, Barra de cereal', total: 70, where: 'Línea 5', status: 'en_preparacion', paid: false, at: `${T}T10:40` },
      { id: 'AB-310', client: 'Ana López', items: 'Refresco ×2, Hot dog', total: 140, where: 'Línea 1', status: 'listo', paid: false, at: `${T}T10:05` },
      { id: 'AB-309', client: 'Mostrador', items: 'Agua natural ×3', total: 75, where: 'Barra', status: 'entregado', paid: true, at: `${T}T09:20` },
      { id: 'AB-312', client: 'Sofía Torres', items: 'Torta de jamón, Agua fresca', total: 125, where: 'Línea 9', status: 'recibido', paid: false, at: `${T}T11:02` },
    ],
    staff: [
      { id: 's1', name: 'Daniel Ramos', email: 'daniel@pcpshooting.com', role: 'admin', lastAccess: 'Hoy' },
      { id: 's2', name: 'Karla Méndez', email: 'karla@pcpshooting.com', role: 'recepcion', lastAccess: 'Hoy' },
      { id: 's3', name: 'Luis Pech', email: 'luis@pcpshooting.com', role: 'recepcion', lastAccess: 'Ayer' },
    ],
    settings: {
      schedule: ['Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado', 'Domingo'].map((day) => ({
        day,
        open: day !== 'Domingo',
        from: '09:00',
        to: '17:00',
      })),
      intervalMinutes: 60,
      bookingWindowDays: 30,
      activeLines: Array.from({ length: 14 }, (_, i) => i + 1),
      notifyEmail: 'contacto@pcpshooting.com',
      waOrderReady: 'Hola [nombre], tu pedido [número] de PCP Shooting está listo para recoger en el club. ¡Te esperamos!',
      waReminder: 'Hola [nombre], te esperamos mañana a las [hora] en la línea [línea]. Recuerda llegar 10 minutos antes.',
      rentalAvailable: true,
    },
    posts: blogPosts.map((p, i) => ({
      slug: p.slug,
      title: p.title,
      category: p.category,
      date: p.date,
      status: (i === 3 ? 'borrador' : i === 6 ? 'programado' : 'publicado') as BlogStatus,
      body: p.body.join('\n\n'),
    })),
    photos: galleryItems.map((g) => ({ id: g.id, title: g.title, album: g.category })),
    sales,
  };
}

export const PLAN_NAMES = PLANS.map((p) => p.name);
export const DEMO_NAMES = names;
