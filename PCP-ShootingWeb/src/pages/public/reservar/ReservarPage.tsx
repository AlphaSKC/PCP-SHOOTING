import { useNavigate, useSearchParams } from 'react-router';
import { PageHero, StepBar } from '@/components/ui';
import { useAuth } from '@/features/auth/AuthProvider';
import { getPackages } from '@/features/reservas/api';
import { RESERVATION_STEPS } from '@/features/reservas/config';
import { ReservationSummary, type SummaryData } from '@/features/reservas/components/ReservationSummary';
import { peopleLabel, shortDay } from '@/features/reservas/format';
import { useAsync } from '@/features/reservas/useAsync';
import { useReservationDraft } from '@/features/reservas/useReservationDraft';
import { LoadingScreen } from '@/pages/LoadingScreen';
import { StepActivity } from './steps/StepActivity';
import { StepContact } from './steps/StepContact';
import { StepPayment } from './steps/StepPayment';
import { StepSchedule } from './steps/StepSchedule';
import { DATA_SOURCE } from '@/lib/api';
import ReservarApiPage from './ReservarApiPage';
import styles from './ReservarPage.module.css';

/** Con VITE_DATA_SOURCE=api la reserva usa la API real; si no, la simulación. */
export default function ReservarPage() {
  return DATA_SOURCE === 'api' ? <ReservarApiPage /> : <ReservarDemoPage />;
}

function ReservarDemoPage() {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const { user } = useAuth();
  const { step, draft, update, goTo, reset } = useReservationDraft(params.get('actividad'));
  const packages = useAsync(getPackages, []);

  const pkg = packages.data?.find((p) => p.id === draft.packageId);
  const total = (pkg?.pricePerPerson ?? 0) * draft.people;

  const summaryData: SummaryData = {
    packageName: pkg?.name,
    equipment: draft.equipment,
    people: draft.people,
    date: draft.date,
    time: draft.time,
    line: draft.line,
    total,
  };

  // Texto corto de la barra inferior en celular.
  const summary = [
    pkg?.name,
    peopleLabel(draft.people, true),
    draft.date && shortDay(draft.date),
    draft.time,
    draft.line && `Línea ${draft.line}`,
  ]
    .filter(Boolean)
    .join(' · ');

  const shared = { draft, update, summary, total };

  return (
    <>
      <title>Reservar línea de tiro · PCP Shooting</title>
      <meta
        name="description"
        content="Reserva tu línea de tiro en PCP Shooting: elige actividad, fecha, horario y una de las 15 líneas. Pago seguro en línea."
      />

      <PageHero
        compact
        eyebrow="Reservas en línea"
        title="Reserva tu línea de tiro"
        lead="15 líneas de tiro · Martes a domingo, 8:00 a 18:00 · Pago seguro en línea"
        hideLeadOnMobile
        aside={<StepBar steps={RESERVATION_STEPS} current={step} />}
      />

      <div className={`container ${styles.layout}`}>
        <div className={styles.main}>
          {!packages.data ? (
            <LoadingScreen />
          ) : step === 0 ? (
            <StepActivity {...shared} packages={packages.data} onNext={() => goTo(1)} />
          ) : step === 1 ? (
            <StepSchedule {...shared} onBack={() => goTo(0)} onNext={() => goTo(2)} />
          ) : step === 2 ? (
            <StepContact {...shared} onBack={() => goTo(1)} onNext={() => goTo(3)} />
          ) : (
            <StepPayment
              {...shared}
              summaryData={summaryData}
              onBack={() => goTo(2)}
              onLineTaken={() => {
                update({ line: null });
                goTo(1);
              }}
              onPaid={(reservation) => {
                reset();
                navigate(`/reservar/confirmacion/${reservation.id}`, { replace: true });
              }}
            />
          )}
        </div>

        <aside className={styles.aside}>
          <ReservationSummary data={summaryData} showMemberNote={!user?.membership} />
        </aside>
      </div>
    </>
  );
}
