import { useState } from 'react';
import { Button } from '@/components/ui';
import { IconCheck } from '@/components/icons/icons';
import { useAuth } from '@/features/auth/AuthProvider';
import { PERIODS, PLANS, priceFor, type MembershipPlan, type Period } from '@/features/membresias/plans';
import { cx, formatDate, formatMXN } from '@/lib/format';
import { MembershipSignup } from './MembershipSignup';
import styles from './MembresiasPage.module.css';

const money = (n: number) => formatMXN(n).replace(' MXN', '');

function PlanCard({
  plan,
  period,
  selected,
  onChoose,
}: {
  plan: MembershipPlan;
  period: Period;
  selected: boolean;
  onChoose: () => void;
}) {
  const months = PERIODS.find((p) => p.id === period)!.months;
  const price = priceFor(plan, period);
  return (
    <article className={cx(styles.plan, styles[plan.variant], selected && styles.planSelected)} aria-labelledby={`plan-${plan.id}`}>
      <div className={styles.planHead}>
        <h2 id={`plan-${plan.id}`} className={styles.planName}>
          {plan.name}
        </h2>
        {plan.badge && <span className={styles.badge}>{plan.badge}</span>}
      </div>
      <p className={styles.tagline}>{plan.tagline}</p>
      <p className={styles.price}>
        <strong>{money(price)}</strong>
        <span>{months === 1 ? 'por 1 mes' : 'por 12 meses'}</span>
      </p>
      {months > 1 && <p className={styles.equiv}>Equivale a {money(Math.round(price / 12))} al mes</p>}
      <ul className={styles.benefits}>
        {plan.benefits.map((b, i) => (
          <li key={b} className={cx(i >= plan.benefitsShort && styles.extraBenefit)}>
            <IconCheck size={16} strokeWidth={3} className={styles.benefitIcon} />
            {b}
          </li>
        ))}
      </ul>
      <Button
        size="lg"
        fullWidth
        variant={plan.variant === 'gold' ? 'gold' : 'primary'}
        onClick={onChoose}
        aria-pressed={selected}
        className={styles.choose}
      >
        {selected ? `${plan.name} elegido` : `Elegir ${plan.name}`}
      </Button>
    </article>
  );
}

export default function MembresiasPage() {
  const { user } = useAuth();
  const [period, setPeriod] = useState<Period>('mensual');
  const current = PLANS.find((p) => p.name === user?.membership?.plan);
  const [planId, setPlanId] = useState<string>(current?.id ?? 'socio-oro');
  const plan = PLANS.find((p) => p.id === planId)!;
  const months = PERIODS.find((p) => p.id === period)!.months;

  const choose = (id: string) => {
    setPlanId(id);
    document.getElementById('inscripcion')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  };

  return (
    <>
      <title>Membresías CARTAQ · PCP Shooting</title>
      <meta
        name="description"
        content="Hazte socio de PCP Shooting: reserva primero, paga menos por sesión y forma parte de la comunidad de tiro PCP en Quintana Roo."
      />

      <section className={styles.hero}>
        <div className={`container ${styles.heroInner}`}>
          <span className={styles.eyebrow}>Membresía PCP</span>
          <h1 className={styles.title}>Hazte socio del club</h1>
          <p className={styles.lead}>
            <span className={styles.desktopInline}>
              Reserva primero, paga menos por sesión y forma parte de la comunidad de tiro PCP en Quintana Roo.
            </span>
            <span className={styles.mobileInline}>Reserva primero y paga menos por sesión.</span>
          </p>
          <div role="radiogroup" aria-label="Periodo" className={styles.periods}>
            {PERIODS.map((p) => (
              <button
                key={p.id}
                type="button"
                role="radio"
                aria-checked={period === p.id}
                className={cx(styles.period, period === p.id && styles.periodActive)}
                onClick={() => setPeriod(p.id)}
              >
                {p.label}
                {p.note && ` · ${p.note}`}
              </button>
            ))}
          </div>
          <p className={styles.heroNote}>Pago único por periodo · Renovación manual · Sin cargos automáticos</p>
        </div>
      </section>

      <div className={`container ${styles.main}`}>
        {user?.membership && (
          <div className={styles.renewal} role="status">
            <span>
              Tu membresía <strong>{user.membership.plan}</strong> vence el{' '}
              <strong>{formatDate(user.membership.validUntil)}</strong>. Si renuevas ahora, el nuevo periodo empieza al
              terminar el actual.
            </span>
          </div>
        )}

        <div className={styles.plans}>
          {PLANS.map((p) => (
            <PlanCard key={p.id} plan={p} period={period} selected={p.id === planId} onChoose={() => choose(p.id)} />
          ))}
        </div>

        <section id="inscripcion" className={styles.signup} aria-labelledby="inscripcion-titulo">
          <div className={styles.signupSide}>
            <span className={styles.signupEyebrow}>{user?.membership ? 'Renovación' : 'Inscripción'}</span>
            <h2 id="inscripcion-titulo" className={styles.signupTitle}>
              <span className={styles.desktopInline}>Plan seleccionado</span>
              <span className={styles.mobileInline}>
                {user?.membership ? 'Renovación' : 'Inscripción'} · {plan.name}
              </span>
            </h2>
            <div className={styles.selected}>
              <strong>{plan.name}</strong>
              <span>
                Pago {period === 'anual' ? 'anual' : 'mensual'} · {money(priceFor(plan, period))} ·{' '}
                {months === 1 ? '1 mes' : '12 meses'}
              </span>
            </div>
            <ol className={styles.steps}>
              <li>Crea tu cuenta o inicia sesión</li>
              <li>Acepta la carta responsiva en línea</li>
              <li>Paga y recibe tu credencial digital</li>
            </ol>
          </div>
          <div className={styles.signupMain}>
            <MembershipSignup key={`${plan.id}-${period}`} plan={plan} period={period} />
          </div>
        </section>
      </div>
    </>
  );
}
