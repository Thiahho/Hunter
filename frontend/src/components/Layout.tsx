import type { ReactNode } from 'react';
import { NavLink, useNavigate } from 'react-router';
import { useAuthStore } from '../store/authStore';

const navItems = [
  { to: '/app/dashboard', label: 'Dashboard' },
  { to: '/app/prospects', label: 'Prospectos' },
  { to: '/app/leads', label: 'Leads' },
];

export function Layout({ children }: { children: ReactNode }) {
  const user = useAuthStore((state) => state.user);
  const clearSession = useAuthStore((state) => state.clearSession);
  const navigate = useNavigate();

  function handleLogout() {
    clearSession();
    navigate('/login', { replace: true });
  }

  return (
    <div className="flex min-h-screen bg-slate-50 dark:bg-slate-950">
      <aside className="flex w-56 flex-col border-r border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900">
        <div className="px-4 py-5">
          <h1 className="text-base font-semibold text-slate-900 dark:text-slate-100">Hunter CRM AI</h1>
        </div>

        <nav className="flex-1 space-y-1 px-2">
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                `block rounded-md px-3 py-2 text-sm font-medium ${
                  isActive
                    ? 'bg-indigo-50 text-indigo-700 dark:bg-indigo-500/10 dark:text-indigo-300'
                    : 'text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800'
                }`
              }
            >
              {item.label}
            </NavLink>
          ))}
        </nav>

        <div className="border-t border-slate-200 dark:border-slate-800 p-3">
          <p className="truncate text-sm font-medium text-slate-700 dark:text-slate-200">
            {user?.firstName} {user?.lastName}
          </p>
          <p className="truncate text-xs text-slate-400">{user?.email}</p>
          <button
            onClick={handleLogout}
            className="mt-2 w-full rounded-md border border-slate-200 dark:border-slate-700 px-3 py-1.5 text-xs font-medium text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800"
          >
            Cerrar sesión
          </button>
        </div>
      </aside>

      <main className="flex-1 overflow-y-auto p-6">{children}</main>
    </div>
  );
}
