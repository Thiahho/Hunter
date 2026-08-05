import type { ReactNode } from 'react';
import { NavLink, useNavigate } from 'react-router';
import { useMutation, useQuery } from '@tanstack/react-query';
import { useAuthStore } from '../store/authStore';
import { fetchCurrentUser, generateTelegramLink } from '../api/auth';

const navItems = [
  { to: '/app/dashboard', label: 'Dashboard' },
  { to: '/app/prospects', label: 'Prospectos' },
  { to: '/app/prospects/search', label: 'Buscar prospectos' },
  { to: '/app/leads', label: 'Leads' },
  { to: '/app/profile', label: 'Mi perfil' },
];

const userManagementRoles = ['OWNER', 'ADMIN'];

export function Layout({ children }: { children: ReactNode }) {
  const user = useAuthStore((state) => state.user);
  const clearSession = useAuthStore((state) => state.clearSession);
  const navigate = useNavigate();

  // refetchOnWindowFocus es lo que hace que, al volver de Telegram a esta pestaña después de
  // completar /start, el estado "conectado" aparezca solo, sin que el usuario tenga que hacer nada.
  const meQuery = useQuery({ queryKey: ['me'], queryFn: fetchCurrentUser, refetchOnWindowFocus: true });
  const telegramMutation = useMutation({
    mutationFn: generateTelegramLink,
    onSuccess: (link) => window.open(link.deepLink, '_blank'),
  });

  function handleLogout() {
    clearSession();
    navigate('/login', { replace: true });
  }

  return (
    <div className="flex min-h-screen bg-slate-50 dark:bg-slate-950">
      <aside className="flex w-56 flex-col border-r border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900">
        <div className="px-4 py-5">
          <h1 className="text-base font-semibold text-slate-900 dark:text-slate-100">DIFRANI | Hunter CRM AI</h1>
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
          {user?.roles.some((role) => userManagementRoles.includes(role)) && (
            <NavLink
              to="/app/users"
              className={({ isActive }) =>
                `block rounded-md px-3 py-2 text-sm font-medium ${
                  isActive
                    ? 'bg-indigo-50 text-indigo-700 dark:bg-indigo-500/10 dark:text-indigo-300'
                    : 'text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800'
                }`
              }
            >
              Usuarios
            </NavLink>
          )}
        </nav>

        <div className="border-t border-slate-200 dark:border-slate-800 p-3">
          <p className="truncate text-sm font-medium text-slate-700 dark:text-slate-200">
            {user?.firstName} {user?.lastName}
          </p>
          <p className="truncate text-xs text-slate-400">{user?.email}</p>

          {meQuery.data?.telegramConnected ? (
            <p className="mt-2 text-xs font-medium text-emerald-600 dark:text-emerald-400">Telegram conectado ✓</p>
          ) : (
            <button
              onClick={() => telegramMutation.mutate()}
              disabled={telegramMutation.isPending}
              className="mt-2 w-full rounded-md border border-slate-200 dark:border-slate-700 px-3 py-1.5 text-xs font-medium text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-60"
            >
              {telegramMutation.isPending ? 'Generando link…' : 'Conectar Telegram'}
            </button>
          )}
          {telegramMutation.isError && (
            <p className="mt-1 text-xs text-red-600 dark:text-red-400">
              {telegramMutation.error instanceof Error ? telegramMutation.error.message : 'No se pudo conectar Telegram.'}
            </p>
          )}

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
