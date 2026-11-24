/*
 * Almacén de DEMOSTRACIÓN del panel (en memoria + localStorage).
 * Permite que las acciones se reflejen en todo el panel (p. ej. un check-in descuenta inventario).
 * Al integrar la API, cada acción se sustituye por su endpoint y la lectura por TanStack Query.
 */
import { useSyncExternalStore } from 'react';
import { createSeed, todayISO, type AdminDB, type AdminReservation, type MovementType } from './seed';

const KEY = 'pcp.admin.demo';
const VERSION = 1;

function load(): AdminDB {
  try {
    const raw = localStorage.getItem(KEY);
    if (raw) {
      const db = JSON.parse(raw) as AdminDB;
      if (db.version === VERSION) return db;
    }
  } catch {
    /* datos dañados o sin almacenamiento */
  }
  return createSeed();
}

let db: AdminDB = load();
const listeners = new Set<() => void>();

function commit(next: AdminDB) {
  db = next;
  try {
    localStorage.setItem(KEY, JSON.stringify(db));
  } catch {
    /* sin almacenamiento: solo en memoria */
  }
  listeners.forEach((l) => l());
}

export function update(recipe: (draft: AdminDB) => AdminDB) {
  commit(recipe(db));
}

export function resetDemo() {
  commit(createSeed());
}

export function useAdmin<T>(selector: (db: AdminDB) => T): T {
  return useSyncExternalStore(
    (cb) => {
      listeners.add(cb);
      return () => listeners.delete(cb);
    },
    () => selector(db),
  );
}

const stamp = () => new Date().toISOString().slice(0, 16);
let seq = 1000;
export const newId = (prefix: string) => `${prefix}-${++seq}${Math.floor(Math.random() * 90 + 10)}`;

/** Registra un movimiento de inventario y ajusta la existencia. */
export function addMovement(itemId: string, type: MovementType, qty: number, note: string, by: string) {
  update((d) => {
    const delta = type === 'entrada' ? Math.abs(qty) : type === 'salida' ? -Math.abs(qty) : qty;
    return {
      ...d,
      inventory: d.inventory.map((i) => (i.id === itemId ? { ...i, stock: Math.max(0, i.stock + delta) } : i)),
      movements: [{ id: newId('M'), itemId, type, qty: delta, note, at: stamp(), by }, ...d.movements],
    };
  });
}

/**
 * Check-in (CLAUDE.md §6): valida el código, marca la reserva como utilizada
 * y descuenta los consumibles del paquete por persona.
 */
export function checkIn(codeOrId: string, by: string): { ok: true; reservation: AdminReservation } | { ok: false; error: string } {
  const code = codeOrId.trim().toUpperCase();
  const r = db.reservations.find((x) => x.code.toUpperCase() === code || x.id.toUpperCase() === code);
  if (!r) return { ok: false, error: 'No encontramos una reserva con ese código.' };
  if (r.status === 'utilizada') return { ok: false, error: `${r.code} ya registró su llegada.` };
  if (r.status === 'cancelada') return { ok: false, error: `${r.code} está cancelada.` };
  if (r.status === 'pendiente_pago') return { ok: false, error: `${r.code} aún no está pagada.` };
  if (r.date !== todayISO()) return { ok: false, error: `${r.code} es para otro día.` };

  const pkg = db.packages.find((p) => p.id === r.packageId);
  update((d) => ({ ...d, reservations: d.reservations.map((x) => (x.id === r.id ? { ...x, status: 'utilizada' } : x)) }));
  pkg?.consumables.forEach((c) => addMovement(c.itemId, 'salida', c.perPerson * r.people, `Check-in ${r.code} (${pkg.name})`, by));
  return { ok: true, reservation: { ...r, status: 'utilizada' } };
}

/** Descarga un CSV (Excel en español abre bien el BOM UTF-8). */
export function downloadCsv(filename: string, rows: (string | number)[][]) {
  const csv = rows
    .map((r) => r.map((c) => `"${String(c).replace(/"/g, '""')}"`).join(','))
    .join('\r\n');
  const url = URL.createObjectURL(new Blob(['﻿' + csv], { type: 'text/csv;charset=utf-8' }));
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  a.click();
  URL.revokeObjectURL(url);
}

/** Enlace de WhatsApp con mensaje prellenado. */
export const waLink = (phone: string, text: string) =>
  `https://wa.me/52${phone.replace(/\D/g, '').slice(-10)}?text=${encodeURIComponent(text)}`;
