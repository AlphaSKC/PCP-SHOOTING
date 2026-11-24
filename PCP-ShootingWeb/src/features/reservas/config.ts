/*
 * Configuración de reservas. Varios valores están PENDIENTES de definir con el cliente (CLAUDE.md §8):
 * se dejan aquí para cambiarlos en un solo lugar (y después vendrán de /admin/configuracion vía API).
 */

/** Pasos del wizard (StepBar). */
export const RESERVATION_STEPS = ['Actividad', 'Fecha y línea', 'Datos y carta', 'Pago'];

/** Líneas de tiro del campo. */
export const TOTAL_LINES = 15;

/** Días de operación: lunes (1) a sábado (6). */
export const OPEN_WEEKDAYS = [1, 2, 3, 4, 5, 6];

/** Horarios de inicio. PENDIENTE: intervalos entre horarios y cuándo se libera una línea. */
export const START_TIMES = ['9:00', '10:00', '11:00', '12:00', '13:00', '14:00', '15:00', '16:00'];

/** Cuántos días hábiles hacia adelante se pueden reservar. */
export const BOOKING_WINDOW_DAYS = 12;

/** Personas por reserva (una línea). PENDIENTE: capacidad real por línea. */
export const MIN_PEOPLE = 1;
export const MAX_PEOPLE = 6;

/** Firma en pantalla de la carta responsiva. PENDIENTE: mecanismo legal (firma, validación, almacenamiento). */
export const REQUIRE_SIGNATURE = true;
