import { useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  createUser,
  listUsers,
  updateUser,
  type InvitableRole,
  type UpdateUserRequest,
  type UserArea,
  type UserDto,
} from '../../api/users';

const roleOptions: { value: InvitableRole; label: string }[] = [
  { value: 'ADMIN', label: 'Admin' },
  { value: 'MANAGER', label: 'Manager' },
  { value: 'SELLER', label: 'Vendedor' },
];

const areaOptions: { value: UserArea; label: string }[] = [
  { value: 'Unassigned', label: 'Sin asignar' },
  { value: 'Ventas', label: 'Ventas' },
  { value: 'Administracion', label: 'Administración' },
];

const areaLabels: Record<UserArea, string> = {
  Unassigned: 'Sin asignar',
  Ventas: 'Ventas',
  Administracion: 'Administración',
};

const inputClass =
  'mt-1 w-full rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 py-2 text-sm text-slate-900 dark:text-slate-100';

export function UsersPage() {
  const queryClient = useQueryClient();

  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [role, setRole] = useState<InvitableRole>('SELLER');
  const [area, setArea] = useState<UserArea>('Ventas');
  const [phone, setPhone] = useState('');

  const usersQuery = useQuery({ queryKey: ['users'], queryFn: listUsers });

  const createMutation = useMutation({
    mutationFn: createUser,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      setFirstName('');
      setLastName('');
      setEmail('');
      setPassword('');
      setRole('SELLER');
      setArea('Ventas');
      setPhone('');
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, request }: { id: number; request: UpdateUserRequest }) => updateUser(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
    },
  });

  function handleCreate(event: FormEvent) {
    event.preventDefault();
    createMutation.mutate({
      firstName,
      lastName,
      email,
      password,
      role,
      area,
      phone: phone || undefined,
    });
  }

  function handleAreaChange(user: UserDto, newArea: UserArea) {
    updateMutation.mutate({ id: user.id, request: { area: newArea } });
  }

  function handleToggleActive(user: UserDto) {
    updateMutation.mutate({ id: user.id, request: { isActive: !user.isActive } });
  }

  const users = usersQuery.data ?? [];

  return (
    <div className="max-w-4xl space-y-4">
      <div>
        <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-100">Usuarios</h2>
        <p className="text-sm text-slate-500 dark:text-slate-400">
          Alta de vendedores/administradores, asignación de rol y área. Cada uno tiene que vincular su propio
          Telegram desde su sesión para recibir alertas de leads.
        </p>
      </div>

      <form
        onSubmit={handleCreate}
        className="grid grid-cols-1 gap-4 rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-5 sm:grid-cols-2"
      >
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
          <input type="email" required value={email} onChange={(e) => setEmail(e.target.value)} className={inputClass} />
        </div>
        <div>
          <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">Contraseña</label>
          <input
            type="password"
            required
            minLength={8}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className={inputClass}
            placeholder="Mínimo 8 caracteres"
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">Rol</label>
          <select value={role} onChange={(e) => setRole(e.target.value as InvitableRole)} className={inputClass}>
            {roleOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">Área</label>
          <select value={area} onChange={(e) => setArea(e.target.value as UserArea)} className={inputClass}>
            {areaOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
          <p className="mt-1 text-xs text-slate-400">Determina qué leads le llegan (Ventas vs. Administración).</p>
        </div>
        <div>
          <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">Teléfono (opcional)</label>
          <input type="text" value={phone} onChange={(e) => setPhone(e.target.value)} className={inputClass} placeholder="ej. 011 15 1234-5678" />
        </div>

        {createMutation.isError && (
          <p className="sm:col-span-2 text-sm text-red-600 dark:text-red-400">
            {createMutation.error instanceof Error ? createMutation.error.message : 'Ocurrió un error inesperado.'}
          </p>
        )}

        <div className="sm:col-span-2">
          <button
            type="submit"
            disabled={createMutation.isPending}
            className="rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-500 disabled:opacity-60"
          >
            {createMutation.isPending ? 'Creando…' : 'Crear usuario'}
          </button>
        </div>
      </form>

      {usersQuery.isLoading && <p className="text-sm text-slate-500 dark:text-slate-400">Cargando usuarios...</p>}

      {usersQuery.isError && (
        <p className="text-sm text-red-600 dark:text-red-400">
          {usersQuery.error instanceof Error ? usersQuery.error.message : 'No se pudieron cargar los usuarios.'}
        </p>
      )}

      {usersQuery.data && (
        <div className="overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-800">
          <table className="min-w-full divide-y divide-slate-200 dark:divide-slate-800 text-sm">
            <thead className="bg-slate-50 dark:bg-slate-900">
              <tr>
                <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Nombre</th>
                <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Email</th>
                <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Rol</th>
                <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Área</th>
                <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Telegram</th>
                <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Activo</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-800 bg-white dark:bg-slate-950">
              {users.map((user) => (
                <tr key={user.id} className={user.isActive ? 'hover:bg-slate-50 dark:hover:bg-slate-900' : 'opacity-50'}>
                  <td className="px-4 py-2 text-slate-900 dark:text-slate-100">
                    {user.firstName} {user.lastName}
                  </td>
                  <td className="px-4 py-2 text-slate-600 dark:text-slate-300">{user.email}</td>
                  <td className="px-4 py-2 text-slate-600 dark:text-slate-300">{user.roles.join(', ')}</td>
                  <td className="px-4 py-2">
                    <select
                      value={user.area}
                      onChange={(e) => handleAreaChange(user, e.target.value as UserArea)}
                      disabled={updateMutation.isPending}
                      className="rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-2 py-1 text-xs text-slate-900 dark:text-slate-100"
                    >
                      {areaOptions.map((option) => (
                        <option key={option.value} value={option.value}>
                          {areaLabels[option.value]}
                        </option>
                      ))}
                    </select>
                  </td>
                  <td className="px-4 py-2">
                    {user.telegramChatId ? (
                      <div>
                        <span className="text-xs font-medium text-emerald-600 dark:text-emerald-400">Vinculado ✓</span>
                        <p className="font-mono text-xs text-slate-400" title="Telegram chat_id">
                          {user.telegramChatId}
                        </p>
                      </div>
                    ) : (
                      <span className="text-xs text-slate-400">Sin vincular</span>
                    )}
                  </td>
                  <td className="px-4 py-2">
                    <label className="flex items-center gap-1.5 text-xs text-slate-600 dark:text-slate-300">
                      <input
                        type="checkbox"
                        checked={user.isActive}
                        onChange={() => handleToggleActive(user)}
                        disabled={updateMutation.isPending}
                        className="rounded border-slate-300 dark:border-slate-700"
                      />
                      {user.isActive ? 'Activo' : 'Inactivo'}
                    </label>
                  </td>
                </tr>
              ))}
              {users.length === 0 && (
                <tr>
                  <td colSpan={6} className="px-4 py-6 text-center text-slate-400">
                    No hay usuarios todavía.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
