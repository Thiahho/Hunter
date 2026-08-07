import { useState, type ReactNode } from 'react';
import { NavLink, useNavigate } from 'react-router';
import { useMutation, useQuery } from '@tanstack/react-query';
import { useAuthStore } from '../store/authStore';
import { fetchCurrentUser, generateTelegramLink } from '../api/auth';

const navItems = [
  { to: '/app/dashboard', label: 'Dashboard' },
  { to: '/app/prospects', label: 'Prospectos' },
  { to: '/app/prospects/search', label: 'Buscar prospectos' },
  { to: '/app/leads', label: 'Leads' },
  { to: '/app/messages', label: 'Mensajes' },
  { to: '/app/templates', label: 'Plantillas' },
  { to: '/app/profile', label: 'Mi perfil' },
];

const userManagementRoles = ['OWNER', 'ADMIN'];

export function Layout({ children }: { children: ReactNode }) {
  const user = useAuthStore((state) => state.user);
  const clearSession = useAuthStore((state) => state.clearSession);
  const navigate = useNavigate();
  const [menuOpen, setMenuOpen] = useState(false);

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

  const navLinkClass = ({ isActive }: { isActive: boolean }) =>
    `block rounded-md px-3 py-2 text-sm font-medium ${
      isActive
        ? 'bg-indigo-50 text-indigo-700 dark:bg-indigo-500/10 dark:text-indigo-300'
        : 'text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800'
    }`;

  return (
    <div className="flex min-h-screen bg-slate-50 dark:bg-slate-950">
      {/* Barra superior solo en mobile: el sidebar completo no entra en una pantalla chica, así
          que ahí queda oculto detrás de este botón en vez de ocupar espacio fijo siempre. */}
      <div className="fixed inset-x-0 top-0 z-30 flex items-center justify-between border-b border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 px-4 py-3 md:hidden">
        <h1 className="text-sm font-semibold text-slate-900 dark:text-slate-100">DIFRANI | Hunter</h1>
        <button
          onClick={() => setMenuOpen(true)}
          aria-label="Abrir menú"
          className="rounded-md p-2 text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800"
        >
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" className="h-5 w-5">
            <path fillRule="evenodd" d="M2 4.75A.75.75 0 0 1 2.75 4h14.5a.75.75 0 0 1 0 1.5H2.75A.75.75 0 0 1 2 4.75Zm0 5A.75.75 0 0 1 2.75 9h14.5a.75.75 0 0 1 0 1.5H2.75A.75.75 0 0 1 2 9.75Zm0 5a.75.75 0 0 1 .75-.75h14.5a.75.75 0 0 1 0 1.5H2.75a.75.75 0 0 1-.75-.75Z" clipRule="evenodd" />
          </svg>
        </button>
      </div>

      {menuOpen && (
        <div className="fixed inset-0 z-40 bg-black/40 md:hidden" onClick={() => setMenuOpen(false)} />
      )}

      <aside
        className={`fixed inset-y-0 left-0 z-50 flex w-64 flex-col border-r border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 transition-transform duration-200 md:static md:z-auto md:w-56 md:translate-x-0 ${
          menuOpen ? 'translate-x-0' : '-translate-x-full'
        }`}
      >
        <div className="flex items-center justify-between px-4 py-5">
          <h1 className="text-base font-semibold text-slate-900 dark:text-slate-100">DIFRANI | Hunter CRM AI</h1>
          <button
            onClick={() => setMenuOpen(false)}
            aria-label="Cerrar menú"
            className="rounded-md p-1 text-slate-500 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 md:hidden"
          >
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" className="h-5 w-5">
              <path d="M6.28 5.22a.75.75 0 0 0-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 1 0 1.06 1.06L10 11.06l3.72 3.72a.75.75 0 1 0 1.06-1.06L11.06 10l3.72-3.72a.75.75 0 0 0-1.06-1.06L10 8.94 6.28 5.22Z" />
            </svg>
          </button>
        </div>

        <nav className="flex-1 space-y-1 px-2">
          {navItems.map((item) => (
            <NavLink key={item.to} to={item.to} className={navLinkClass} onClick={() => setMenuOpen(false)}>
              {item.label}
            </NavLink>
          ))}
          {user?.roles.some((role) => userManagementRoles.includes(role)) && (
            <NavLink to="/app/users" className={navLinkClass} onClick={() => setMenuOpen(false)}>
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

      <main className="flex-1 overflow-y-auto overflow-x-hidden p-4 pt-20 md:p-6 md:pt-6">{children}</main>
    </div>
  );
}
