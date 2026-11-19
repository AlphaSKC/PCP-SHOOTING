import { useState } from 'react';
import { Link } from 'react-router';
import { AdminPageHeader } from '@/components/layout/AdminPageHeader';
import { Button, ButtonLink, DataTable, FormAlert, SelectField, TextareaField, TextField, type Column } from '@/components/ui';
import { IconPlus } from '@/components/icons/icons';
import type { Settings, StaffUser } from '@/features/admin/seed';
import { newId, resetDemo, update, useAdmin } from '@/features/admin/store';
import { AdminBody, Section } from '@/features/admin/ui';
import { TOTAL_LINES } from '@/features/reservas/config';
import { cx } from '@/lib/format';
import shared from './AdminShared.module.css';
import styles from './AdminConfigPage.module.css';

const ROLE = { admin: 'Administrador', recepcion: 'Recepción' } as const;

export default function AdminConfigPage() {
  const saved = useAdmin((d) => d.settings);
  const staff = useAdmin((d) => d.staff);
  const [s, setS] = useState<Settings>(saved);
  const [invite, setInvite] = useState({ name: '', email: '', role: 'recepcion' as StaffUser['role'] });
  const [msg, setMsg] = useState<{ area: string; text: string } | null>(null);

  const save = (area: string) => {
    update((d) => ({ ...d, settings: s }));
    setMsg({ area, text: 'Cambios guardados.' });
  };
  const toggleLine = (l: number) =>
    setS({ ...s, activeLines: s.activeLines.includes(l) ? s.activeLines.filter((x) => x !== l) : [...s.activeLines, l].sort((a, b) => a - b) });

  const staffCols: Column<StaffUser>[] = [
    { key: 'name', header: 'Nombre', primary: true, render: (u) => <span className={shared.stack}><strong>{u.name}</strong><span>{u.email}</span></span> },
    { key: 'role', header: 'Rol', render: (u) => ROLE[u.role] },
    { key: 'last', header: 'Último acceso', render: (u) => u.lastAccess },
  ];

  return (
    <>
      <title>Configuración · Panel PCP</title>
      <AdminPageHeader title="Configuración" subtitle="Horarios, líneas, usuarios y avisos" actions={<ButtonLink to="/" variant="secondary">Ver sitio</ButtonLink>} />
      <AdminBody>
        <div className={styles.grid}>
          <div className={styles.col}>
            <Section title="Horario y líneas de tiro">
              {msg?.area === 'horario' && <FormAlert tone="success">{msg.text}</FormAlert>}
              <ul className={styles.days}>
                {s.schedule.map((d, i) => (
                  <li key={d.day}>
                    <strong>{d.day}</strong>
                    {d.open ? (
                      <span className={styles.times}>
                        <TextField label={`${d.day} abre`} hideLabel type="time" value={d.from} onChange={(e) => setS({ ...s, schedule: s.schedule.map((x, j) => (j === i ? { ...x, from: e.target.value } : x)) })} />
                        <span>–</span>
                        <TextField label={`${d.day} cierra`} hideLabel type="time" value={d.to} onChange={(e) => setS({ ...s, schedule: s.schedule.map((x, j) => (j === i ? { ...x, to: e.target.value } : x)) })} />
                      </span>
                    ) : (
                      <span className={shared.muted}>Cerrado</span>
                    )}
                    <button type="button" className={shared.linkBtn} onClick={() => setS({ ...s, schedule: s.schedule.map((x, j) => (j === i ? { ...x, open: !x.open } : x)) })}>
                      {d.open ? 'Cerrar' : 'Abrir'}
                    </button>
                  </li>
                ))}
              </ul>
              <div className={shared.grid2}>
                <SelectField
                  label="Intervalo entre horarios"
                  hint="Pendiente de definir con el club."
                  value={String(s.intervalMinutes)}
                  onChange={(e) => setS({ ...s, intervalMinutes: Number(e.target.value) })}
                  options={[30, 45, 60, 90, 120].map((m) => ({ value: String(m), label: `${m} min` }))}
                />
                <TextField label="Reservar con hasta (días)" inputMode="numeric" value={s.bookingWindowDays} onChange={(e) => setS({ ...s, bookingWindowDays: Number(e.target.value.replace(/\D/g, '')) })} />
              </div>
              <div>
                <p className={shared.muted}>Líneas activas (toca para activar o desactivar)</p>
                <div className={styles.lines} role="group" aria-label="Líneas activas">
                  {Array.from({ length: TOTAL_LINES }, (_, i) => i + 1).map((l) => (
                    <button key={l} type="button" aria-pressed={s.activeLines.includes(l)} className={cx(styles.line, s.activeLines.includes(l) && styles.lineOn)} onClick={() => toggleLine(l)}>
                      {l}
                    </button>
                  ))}
                </div>
              </div>
              <div><Button size="lg" onClick={() => save('horario')}>Guardar horario</Button></div>
            </Section>

            <Section title="Actividades y precios">
              <p className={shared.muted}>
                Los paquetes se venden por actividad o tirada (sin duración). Precios, condiciones, renta de rifle y consumibles se administran en{' '}
                <Link to="/admin/paquetes">Paquetes</Link>.
              </p>
            </Section>
          </div>

          <div className={styles.col}>
            <Section title="Usuarios del staff">
              <DataTable caption="Usuarios del staff" columns={staffCols} rows={staff} rowKey={(u) => u.id} />
              <p className={shared.muted}><strong>Administrador:</strong> ve y edita todo. <strong>Recepción:</strong> reservas, llegadas, pedidos y alimentos.</p>
              {msg?.area === 'staff' && <FormAlert tone="success">{msg.text}</FormAlert>}
              <div className={shared.grid2}>
                <TextField label="Nombre" value={invite.name} onChange={(e) => setInvite({ ...invite, name: e.target.value })} />
                <TextField label="Correo" type="email" value={invite.email} onChange={(e) => setInvite({ ...invite, email: e.target.value })} />
                <SelectField label="Rol" value={invite.role} onChange={(e) => setInvite({ ...invite, role: e.target.value as StaffUser['role'] })} options={[{ value: 'recepcion', label: 'Recepción' }, { value: 'admin', label: 'Administrador' }]} />
              </div>
              <div>
                <Button
                  iconStart={<IconPlus size={16} />}
                  disabled={!invite.name.trim() || !/\S+@\S+\.\S+/.test(invite.email)}
                  onClick={() => {
                    update((d) => ({ ...d, staff: [...d.staff, { id: newId('S'), ...invite, lastAccess: 'Invitado' }] }));
                    setMsg({ area: 'staff', text: `Invitación enviada a ${invite.email} (en demo no se envía correo).` });
                    setInvite({ name: '', email: '', role: 'recepcion' });
                  }}
                >
                  Invitar
                </Button>
              </div>
            </Section>

            <Section title="Pagos y avisos">
              {/* La conexión real la configura el backend con las credenciales de Mercado Pago. */}
              <div className={styles.mp}>
                <strong>Mercado Pago · pendiente de conectar</strong>
                <span>El backend guardará las credenciales y recibirá los webhooks de pago.</span>
              </div>
              {msg?.area === 'avisos' && <FormAlert tone="success">{msg.text}</FormAlert>}
              <TextField label="Correo que envía los avisos" type="email" value={s.notifyEmail} onChange={(e) => setS({ ...s, notifyEmail: e.target.value })} />
              <TextareaField label="Plantilla de WhatsApp · pedido listo" rows={3} hint="Usa [nombre] y [número]." value={s.waOrderReady} onChange={(e) => setS({ ...s, waOrderReady: e.target.value })} />
              <TextareaField label="Plantilla de WhatsApp · recordatorio de reserva" rows={3} hint="Usa [nombre], [hora] y [línea]." value={s.waReminder} onChange={(e) => setS({ ...s, waReminder: e.target.value })} />
              <div><Button size="lg" onClick={() => save('avisos')}>Guardar</Button></div>
            </Section>

            <Section title="Datos de demostración">
              <p className={shared.muted}>El panel usa datos de ejemplo guardados en este navegador. Puedes restaurarlos cuando quieras.</p>
              <div>
                <Button variant="secondary" onClick={() => { if (window.confirm('¿Restaurar todos los datos de demostración?')) { resetDemo(); window.location.reload(); } }}>
                  Restaurar datos de demostración
                </Button>
              </div>
            </Section>
          </div>
        </div>
      </AdminBody>
    </>
  );
}
