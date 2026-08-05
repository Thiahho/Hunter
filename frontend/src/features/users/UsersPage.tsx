import { useState, type FormEvent, type ReactNode } from 'react';
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
import { PasswordInput } from '../../components/PasswordInput';
import { useAuthStore } from '../../store/authStore';

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
  const isOwner = useAuthStore((state) => state.user?.roles.includes('OWNER') ?? false);

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

  function handleSaveContact(id: number, request: UpdateUserRequest) {
    updateMutation.mutate({ id, request });
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
          <PasswordInput
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

      {usersQuery.data && users.length === 0 && (
        <p className="rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 px-4 py-6 text-center text-sm text-slate-400">
          No hay usuarios todavía.
        </p>
      )}

      {usersQuery.data && users.length > 0 && (
        <>
          {/* Tabla en sm: y para arriba; tarjetas apiladas en mobile — esta fila tiene demasiados
              inputs editables (email, área, teléfono, chatId) como para que el scroll horizontal
              de una tabla sea cómodo de usar con el dedo. */}
          <div className="hidden overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-800 sm:block">
            <table className="min-w-full divide-y divide-slate-200 dark:divide-slate-800 text-sm">
              <thead className="bg-slate-50 dark:bg-slate-900">
                <tr>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Nombre</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Email</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Rol</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Área</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Teléfono</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Chat ID de Telegram</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Activo</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 dark:divide-slate-800 bg-white dark:bg-slate-950">
                {users.map((user) => (
                  <UserRow
                    key={user.id}
                    user={user}
                    isSaving={updateMutation.isPending}
                    canEditEmail={isOwner}
                    onAreaChange={(newArea) => handleAreaChange(user, newArea)}
                    onToggleActive={() => handleToggleActive(user)}
                    onSaveContact={(request) => handleSaveContact(user.id, request)}
                  />
                ))}
              </tbody>
            </table>
          </div>

          <div className="space-y-3 sm:hidden">
            {users.map((user) => (
              <UserCard
                key={user.id}
                user={user}
                isSaving={updateMutation.isPending}
                canEditEmail={isOwner}
                onAreaChange={(newArea) => handleAreaChange(user, newArea)}
                onToggleActive={() => handleToggleActive(user)}
                onSaveContact={(request) => handleSaveContact(user.id, request)}
              />
            ))}
          </div>
        </>
      )}
    </div>
  );
}

interface UserFieldsProps {
  user: UserDto;
  isSaving: boolean;
  canEditEmail: boolean;
  onAreaChange: (area: UserArea) => void;
  onToggleActive: () => void;
  onSaveContact: (request: UpdateUserRequest) => void;
}

// Estado compartido entre la fila de tabla (desktop) y la tarjeta (mobile): mismo usuario,
// mismos campos editables, dos presentaciones. Cada vista mantiene su propia instancia del
// hook (una siempre queda oculta por CSS, nunca las dos visibles a la vez), así que no hay
// riesgo real de que diverjan salvo que se resista la ventana a mitad de una edición sin guardar.
function useEditableUserFields(user: UserDto, canEditEmail: boolean, onSaveContact: (request: UpdateUserRequest) => void) {
  const [email, setEmail] = useState(user.email);
  const [phone, setPhone] = useState(user.phone ?? '');
  const [telegramChatId, setTelegramChatId] = useState(user.telegramChatId ?? '');

  const dirty = email !== user.email || phone !== (user.phone ?? '') || telegramChatId !== (user.telegramChatId ?? '');

  function handleSave() {
    onSaveContact({
      email: canEditEmail && email !== user.email ? email : undefined,
      phone: phone || null,
      telegramChatId: telegramChatId || null,
    });
  }

  return { email, setEmail, phone, setPhone, telegramChatId, setTelegramChatId, dirty, handleSave };
}

function UserRow({ user, isSaving, canEditEmail, onAreaChange, onToggleActive, onSaveContact }: UserFieldsProps) {
  const { email, setEmail, phone, setPhone, telegramChatId, setTelegramChatId, dirty, handleSave } =
    useEditableUserFields(user, canEditEmail, onSaveContact);

  return (
    <tr className={user.isActive ? 'hover:bg-slate-50 dark:hover:bg-slate-900' : 'opacity-50'}>
      <td className="px-4 py-2 text-slate-900 dark:text-slate-100">
        {user.firstName} {user.lastName}
      </td>
      <td className="px-4 py-2 text-slate-600 dark:text-slate-300">
        {canEditEmail ? (
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className="w-40 rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-2 py-1 text-xs text-slate-900 dark:text-slate-100"
          />
        ) : (
          user.email
        )}
      </td>
      <td className="px-4 py-2 text-slate-600 dark:text-slate-300">{user.roles.join(', ')}</td>
      <td className="px-4 py-2">
        <select
          value={user.area}
          onChange={(e) => onAreaChange(e.target.value as UserArea)}
          disabled={isSaving}
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
        <input
          type="text"
          value={phone}
          onChange={(e) => setPhone(e.target.value)}
          placeholder="Sin teléfono"
          className="w-32 rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-2 py-1 text-xs text-slate-900 dark:text-slate-100"
        />
      </td>
      <td className="px-4 py-2">
        <input
          type="text"
          value={telegramChatId}
          onChange={(e) => setTelegramChatId(e.target.value)}
          placeholder="Sin vincular"
          className="w-32 rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-2 py-1 text-xs font-mono text-slate-900 dark:text-slate-100"
        />
        {dirty && (
          <button
            type="button"
            onClick={handleSave}
            disabled={isSaving}
            className="ml-1 rounded-md border border-indigo-300 dark:border-indigo-700 px-2 py-1 text-xs font-medium text-indigo-600 dark:text-indigo-400 hover:bg-indigo-50 dark:hover:bg-indigo-500/10 disabled:opacity-60"
          >
            Guardar
          </button>
        )}
      </td>
      <td className="px-4 py-2">
        <label className="flex items-center gap-1.5 text-xs text-slate-600 dark:text-slate-300">
          <input
            type="checkbox"
            checked={user.isActive}
            onChange={onToggleActive}
            disabled={isSaving}
            className="rounded border-slate-300 dark:border-slate-700"
          />
          {user.isActive ? 'Activo' : 'Inactivo'}
        </label>
      </td>
    </tr>
  );
}

const cardFieldInputClass =
  'w-full rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-2 py-1.5 text-sm text-slate-900 dark:text-slate-100';

function CardField({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="mt-3">
      <label className="block text-xs font-medium text-slate-500 dark:text-slate-400">{label}</label>
      <div className="mt-1">{children}</div>
    </div>
  );
}

function UserCard({ user, isSaving, canEditEmail, onAreaChange, onToggleActive, onSaveContact }: UserFieldsProps) {
  const { email, setEmail, phone, setPhone, telegramChatId, setTelegramChatId, dirty, handleSave } =
    useEditableUserFields(user, canEditEmail, onSaveContact);

  return (
    <div
      className={`rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-4 ${
        user.isActive ? '' : 'opacity-50'
      }`}
    >
      <div className="flex items-start justify-between gap-2">
        <div>
          <p className="text-sm font-medium text-slate-900 dark:text-slate-100">
            {user.firstName} {user.lastName}
          </p>
          <p className="text-xs text-slate-500 dark:text-slate-400">{user.roles.join(', ')}</p>
        </div>
        <label className="flex shrink-0 items-center gap-1.5 text-xs text-slate-600 dark:text-slate-300">
          <input
            type="checkbox"
            checked={user.isActive}
            onChange={onToggleActive}
            disabled={isSaving}
            className="rounded border-slate-300 dark:border-slate-700"
          />
          {user.isActive ? 'Activo' : 'Inactivo'}
        </label>
      </div>

      <CardField label="Email">
        {canEditEmail ? (
          <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} className={cardFieldInputClass} />
        ) : (
          <p className="text-sm text-slate-600 dark:text-slate-300">{user.email}</p>
        )}
      </CardField>

      <CardField label="Área">
        <select
          value={user.area}
          onChange={(e) => onAreaChange(e.target.value as UserArea)}
          disabled={isSaving}
          className={cardFieldInputClass}
        >
          {areaOptions.map((option) => (
            <option key={option.value} value={option.value}>
              {areaLabels[option.value]}
            </option>
          ))}
        </select>
      </CardField>

      <CardField label="Teléfono">
        <input
          type="text"
          value={phone}
          onChange={(e) => setPhone(e.target.value)}
          placeholder="Sin teléfono"
          className={cardFieldInputClass}
        />
      </CardField>

      <CardField label="Chat ID de Telegram">
        <input
          type="text"
          value={telegramChatId}
          onChange={(e) => setTelegramChatId(e.target.value)}
          placeholder="Sin vincular"
          className={`${cardFieldInputClass} font-mono`}
        />
      </CardField>

      {dirty && (
        <button
          type="button"
          onClick={handleSave}
          disabled={isSaving}
          className="mt-3 w-full rounded-md bg-indigo-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-indigo-500 disabled:opacity-60"
        >
          Guardar cambios
        </button>
      )}
    </div>
  );
}
