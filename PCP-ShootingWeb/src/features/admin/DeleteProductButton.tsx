import { useState } from 'react';
import { Button, FormAlert } from '@/components/ui';
import { ApiError } from '@/lib/api';
import { deleteProduct } from './productosApi';

/**
 * Botón «Eliminar» para productos creados por error o de prueba (F22).
 * La API solo borra productos SIN historial (pedidos, movimientos de inventario, paquetes); si lo tiene responde 409
 * y se sugiere ocultarlo. Mientras el backend no publique la ruta (404/405) se avisa que está pendiente.
 */
export function DeleteProductButton({ id, name, onDeleted, hideHint = 'ocúltalo con el interruptor de visibilidad' }: { id: string; name: string; onDeleted: () => void; hideHint?: string }) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onDelete() {
    if (!window.confirm(`¿Eliminar «${name}» definitivamente?\n\nSolo se puede si nunca se vendió ni tuvo movimientos de inventario. No se puede deshacer.`)) return;
    setBusy(true);
    setError(null);
    try {
      await deleteProduct(id);
      onDeleted();
    } catch (e) {
      if (e instanceof ApiError && (e.status === 404 || e.status === 405)) {
        setError('Eliminar productos todavía no está disponible en el servidor (pendiente de backend, F22). Mientras tanto, ' + hideHint + '.');
      } else if (e instanceof ApiError && e.status === 409) {
        setError(`No se puede eliminar porque ya tiene historial (ventas, inventario o paquetes). Para que no se vea, ${hideHint}.`);
      } else {
        setError(e instanceof ApiError ? e.message : 'No pudimos eliminar el producto.');
      }
    } finally {
      setBusy(false);
    }
  }

  return (
    <>
      {error && <FormAlert>{error}</FormAlert>}
      <Button variant="text" loading={busy} onClick={() => void onDelete()}>
        Eliminar producto
      </Button>
    </>
  );
}
