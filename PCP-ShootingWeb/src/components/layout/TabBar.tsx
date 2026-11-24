import { NavLink } from 'react-router';
import type { IconComponent } from '@/components/icons/icons';
import styles from './TabBar.module.css';

export type TabBarItem =
  | { label: string; icon: IconComponent; to: string; end?: boolean }
  | { label: string; icon: IconComponent; onClick: () => void; active?: boolean };

type Props = {
  label: string;
  items: TabBarItem[];
};

/**
 * Barra inferior de pestañas, solo en celular (< 768px).
 * La usan el sitio (BottomTabBar del diseño) y el panel (con "Más").
 */
export function TabBar({ label, items }: Props) {
  return (
    <nav aria-label={label} className={styles.bar}>
      {items.map((item) => {
        const Icon = item.icon;
        const content = (
          <>
            <Icon className={styles.icon} />
            <span className={styles.label}>{item.label}</span>
          </>
        );

        if ('to' in item) {
          return (
            <NavLink
              key={item.label}
              to={item.to}
              end={item.end}
              className={({ isActive }) => `${styles.tab} ${isActive ? styles.active : ''}`}
            >
              {content}
            </NavLink>
          );
        }

        return (
          <button
            key={item.label}
            type="button"
            className={`${styles.tab} ${item.active ? styles.active : ''}`}
            onClick={item.onClick}
          >
            {content}
          </button>
        );
      })}
    </nav>
  );
}
