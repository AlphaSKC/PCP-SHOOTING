import { useEffect, useState } from 'react';

type State<T> = { data: T | undefined; loading: boolean; error: Error | null };

/**
 * Carga datos asíncronos y descarta respuestas viejas si cambian las dependencias.
 * Provisional: al integrar la API real se reemplaza por TanStack Query (CLAUDE.md §10).
 */
export function useAsync<T>(load: (() => Promise<T>) | null, deps: unknown[]): State<T> {
  const [state, setState] = useState<State<T>>({ data: undefined, loading: Boolean(load), error: null });

  useEffect(() => {
    if (!load) {
      setState({ data: undefined, loading: false, error: null });
      return;
    }
    let active = true;
    setState((s) => ({ ...s, loading: true, error: null }));
    load().then(
      (data) => active && setState({ data, loading: false, error: null }),
      (error: unknown) =>
        active && setState({ data: undefined, loading: false, error: error instanceof Error ? error : new Error(String(error)) }),
    );
    return () => {
      active = false;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps);

  return state;
}
