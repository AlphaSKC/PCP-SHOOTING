import { useState } from 'react';
import { Outlet, useLocation, useNavigate } from 'react-router';
import { adminNav, adminTabBar } from '@/app/navigation';
import { useAuth } from '@/app/auth';
import { AdminSidebar } from '@/components/layout/AdminSidebar';
import { AdminTopBar } from '@/components/layout/AdminTopBar';
import { AdminMenu } from '@/components/layout/AdminMenu';
import { TabBar, type TabBarItem } from '@/components/layout/TabBar';
import { IconGrid } from '@/components/icons/icons';
import styles from './AdminLayout.module.css';

export default function AdminLayout() {
  const { user, signOut } = useAuth();
  const { pathname } = useLocation();
  const navigate = useNavigate();
  const [menuOpen, setMenuOpen] = useState(false);
  const handleSignOut = () => {
    void signOut().then(() => navigate('/login', { replace: true }));
  };

  // RequireRole ya bloquea el acceso; esto es solo para que TypeScript sepa que hay usuario.
  if (!user) return null;

  const items = adminNav.filter((item) => item.roles.includes(user.role));
  const isPinned = adminTabBar.some((to) => (to === '/admin' ? pathname === to : pathname.startsWith(to)));

  const tabs: TabBarItem[] = [
    ...items
      .filter((item) => adminTabBar.includes(item.to))
      .map(({ label, to, icon, end }) => ({ label, to, icon, end })),
    { label: 'Más', icon: IconGrid, onClick: () => setMenuOpen(true), active: !isPinned },
  ];

  return (
    <div className={styles.shell}>
      <a href="#contenido" className="skip-link">
        Saltar al contenido
      </a>
      <AdminSidebar items={items} user={user} onSignOut={handleSignOut} />
      <div className={styles.content}>
        <AdminTopBar user={user} menuOpen={menuOpen} onOpenMenu={() => setMenuOpen(true)} />
        <main id="contenido" className={styles.main}>
          <Outlet />
        </main>
        <TabBar label="Panel" items={tabs} />
      </div>
      <AdminMenu
        open={menuOpen}
        onClose={() => setMenuOpen(false)}
        items={items}
        user={user}
        onSignOut={handleSignOut}
      />
    </div>
  );
}
