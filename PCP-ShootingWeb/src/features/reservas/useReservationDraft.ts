import { useCallback, useEffect, useState } from 'react';
import type { ReservationDraft } from './types';

const KEY = 'pcp.reserva.borrador';

type Saved = { step: number; draft: ReservationDraft };

export const EMPTY_DRAFT: ReservationDraft = {
  packageId: 'recreativo',
  equipment: 'renta',
  people: 2,
  date: null,
  time: null,
  line: null,
  contact: null,
  waiver: null,
};

function load(): Saved | null {
  try {
    const raw = sessionStorage.getItem(KEY);
    return raw ? (JSON.parse(raw) as Saved) : null;
  } catch {
    return null;
  }
}

/**
 * Estado del wizard de reservas. Se guarda en sessionStorage para no perder el avance
 * si el cliente recarga la página o regresa de otra sección.
 */
export function useReservationDraft(initialPackageId: string | null) {
  const [state, setState] = useState<Saved>(() => {
    const saved = load();
    if (saved && (!initialPackageId || saved.draft.packageId === initialPackageId)) return saved;
    return { step: 0, draft: { ...EMPTY_DRAFT, packageId: initialPackageId ?? EMPTY_DRAFT.packageId } };
  });

  useEffect(() => {
    try {
      sessionStorage.setItem(KEY, JSON.stringify(state));
    } catch {
      /* sin almacenamiento: el borrador vive en memoria */
    }
  }, [state]);

  const update = useCallback((changes: Partial<ReservationDraft>) => {
    setState((s) => ({ ...s, draft: { ...s.draft, ...changes } }));
  }, []);

  const goTo = useCallback((step: number) => {
    setState((s) => ({ ...s, step }));
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }, []);

  const reset = useCallback(() => {
    try {
      sessionStorage.removeItem(KEY);
    } catch {
      /* nada que limpiar */
    }
    setState({ step: 0, draft: EMPTY_DRAFT });
  }, []);

  return { step: state.step, draft: state.draft, update, goTo, reset };
}
