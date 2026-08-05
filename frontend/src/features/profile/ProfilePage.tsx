import { useEffect, useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { changePassword, fetchCurrentUser, generateTelegramLink, updateOwnProfile } from '../../api/auth';
import { useAuthStore } from '../../store/authStore';
import { PasswordInput } from '../../components/PasswordInput';

const inputClass =
  'mt-1 w-full rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 py-2 text-sm text-slate-900 dark:text-slate-100';

export function ProfilePage() {
  const queryClient = useQueryClient();
  const updateStoredUser = useAuthStore((state) => state.updateUser);
  const meQuery = useQuery({ queryKey: ['me'], queryFn: fetchCurrentUser });

  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [phone, setPhone] = useState('');
  const [telegramChatId, setTelegramChatId] = useState('');

  // Se resincroniza cuando llega la respuesta del fetch inicial, no en cada render: si no,
  // pisaría lo que el usuario está tipeando cada vez que React Query revalida en el fondo.
  useEffect(() => {
    if (meQuery.data) {
      setFirstName(meQuery.data.firstName);
      setLastName(meQuery.data.lastName);
      setPhone(meQuery.data.phone ?? '');
      setTelegramChatId(meQuery.data.telegramChatId ?? '');
    }
  }, [meQuery.data]);

  const updateMutation = useMutation({
    mutationFn: updateOwnProfile,
    onSuccess: (user) => {
      updateStoredUser(user);
      queryClient.setQueryData(['me'], user);
    },
  });

  const telegramMutation = useMutation({
    mutationFn: generateTelegramLink,
    onSuccess: (link) => window.open(link.deepLink, '_blank'),
  });

  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [passwordMismatch, setPasswordMismatch] = useState(false);

  const passwordMutation = useMutation({
    mutationFn: changePassword,
    onSuccess: () => {
      setCurrentPassword('');
      setNewPassword('');
      setConfirmPassword('');
    },
  });

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    updateMutation.mutate({
      firstName,
      lastName,
      phone: phone || undefined,
      telegramChatId: telegramChatId || undefined,
    });
  }

  function handleChangePassword(event: FormEvent) {
    event.preventDefault();
    if (newPassword !== confirmPassword) {
      setPasswordMismatch(true);
      return;
    }
    setPasswordMismatch(false);
    passwordMutation.mutate({ currentPassword, newPassword });
  }

  return (
    <div className="max-w-lg space-y-4">
      <div>
        <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-100">Mi perfil</h2>
        <p className="text-sm text-slate-500 dark:text-slate-400">
          Datos personales. El email y el rol no se pueden cambiar desde acá — pedile a un admin.
        </p>
      </div>

      {meQuery.isLoading && <p className="text-sm text-slate-500 dark:text-slate-400">Cargando...</p>}

      {meQuery.data && (
        <form onSubmit={handleSubmit} className="space-y-4 rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-5">
          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">Nombre</label>
            <input type="text" required value={firstName} onChange={(e) => setFirstName(e.target.value)} className={inputClass} />
          </div>

          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">Apellido</label>
            <input type="text" required value={lastName} onChange={(e) => setLastName(e.target.value)} className={inputClass} />
          </div>

          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">Email</label>
            <input type="email" disabled value={meQuery.data.email} className={`${inputClass} opacity-60`} />
          </div>

          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">Teléfono</label>
            <input
              type="text"
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
              className={inputClass}
              placeholder="ej. 011 15 1234-5678"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">Chat ID de Telegram</label>
            <input
              type="text"
              value={telegramChatId}
              onChange={(e) => setTelegramChatId(e.target.value)}
              className={inputClass}
              placeholder="ej. 123456789"
            />
            <p className="mt-1 text-xs text-slate-400">
              Se completa solo con "Conectar Telegram", o lo podés pegar a mano si ya lo sabés (te lo da{' '}
              <span className="font-mono">@userinfobot</span> en Telegram).
            </p>
            <button
              type="button"
              onClick={() => telegramMutation.mutate()}
              disabled={telegramMutation.isPending}
              className="mt-2 rounded-md border border-slate-300 dark:border-slate-700 px-3 py-1.5 text-xs font-medium text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-60"
            >
              {telegramMutation.isPending ? 'Generando link…' : 'Conectar Telegram'}
            </button>
            {telegramMutation.isError && (
              <p className="mt-1 text-xs text-red-600 dark:text-red-400">
                {telegramMutation.error instanceof Error ? telegramMutation.error.message : 'No se pudo conectar Telegram.'}
              </p>
            )}
          </div>

          {updateMutation.isError && (
            <p className="text-sm text-red-600 dark:text-red-400">
              {updateMutation.error instanceof Error ? updateMutation.error.message : 'Ocurrió un error inesperado.'}
            </p>
          )}
          {updateMutation.isSuccess && <p className="text-sm text-emerald-600 dark:text-emerald-400">Guardado ✓</p>}

          <button
            type="submit"
            disabled={updateMutation.isPending}
            className="rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-500 disabled:opacity-60"
          >
            {updateMutation.isPending ? 'Guardando…' : 'Guardar cambios'}
          </button>
        </form>
      )}

      {meQuery.data && (
        <form
          onSubmit={handleChangePassword}
          className="space-y-4 rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-5"
        >
          <h3 className="text-sm font-semibold text-slate-900 dark:text-slate-100">Cambiar contraseña</h3>

          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">Contraseña actual</label>
            <PasswordInput
              required
              autoComplete="current-password"
              value={currentPassword}
              onChange={(e) => setCurrentPassword(e.target.value)}
              className={inputClass}
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">Contraseña nueva</label>
            <PasswordInput
              required
              minLength={8}
              autoComplete="new-password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              className={inputClass}
              placeholder="Mínimo 8 caracteres"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">Confirmar contraseña nueva</label>
            <PasswordInput
              required
              autoComplete="new-password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              className={inputClass}
            />
          </div>

          {passwordMismatch && <p className="text-sm text-red-600 dark:text-red-400">Las contraseñas nuevas no coinciden.</p>}
          {passwordMutation.isError && (
            <p className="text-sm text-red-600 dark:text-red-400">
              {passwordMutation.error instanceof Error ? passwordMutation.error.message : 'Ocurrió un error inesperado.'}
            </p>
          )}
          {passwordMutation.isSuccess && <p className="text-sm text-emerald-600 dark:text-emerald-400">Contraseña actualizada ✓</p>}

          <button
            type="submit"
            disabled={passwordMutation.isPending}
            className="rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-500 disabled:opacity-60"
          >
            {passwordMutation.isPending ? 'Cambiando…' : 'Cambiar contraseña'}
          </button>
        </form>
      )}
    </div>
  );
}
