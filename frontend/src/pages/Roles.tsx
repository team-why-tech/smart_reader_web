import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import * as rolesApi from '../api/roles';
import type { RoleDto } from '../types';
import toast from 'react-hot-toast';
import { LoadingSpinner } from '../components/LoadingSpinner';

const schema = z.object({
  name: z.string().min(1, 'Name is required').max(50),
  description: z.string().max(255).optional(),
});

type FormData = z.infer<typeof schema>;

export function Roles() {
  const [roles, setRoles] = useState<RoleDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState<RoleDto | null>(null);
  const [creating, setCreating] = useState(false);
  const [deleteConfirm, setDeleteConfirm] = useState<number | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormData>({ resolver: zodResolver(schema) });

  useEffect(() => {
    loadRoles();
  }, []);

  async function loadRoles() {
    setLoading(true);
    try {
      const res = await rolesApi.getRoles();
      if (res.data.success) setRoles(res.data.data ?? []);
    } catch {
      toast.error('Failed to load roles');
    } finally {
      setLoading(false);
    }
  }

  function openCreate() {
    setEditing(null);
    setCreating(true);
    reset({ name: '', description: '' });
  }

  function openEdit(role: RoleDto) {
    setCreating(false);
    setEditing(role);
    reset({ name: role.name, description: role.description ?? '' });
  }

  function closeModal() {
    setEditing(null);
    setCreating(false);
  }

  async function onSubmit(data: FormData) {
    try {
      if (editing) {
        const res = await rolesApi.updateRole(editing.id, data);
        if (res.data.success) {
          toast.success('Role updated');
          closeModal();
          loadRoles();
        } else {
          toast.error(res.data.message ?? 'Update failed');
        }
      } else {
        const res = await rolesApi.createRole(data);
        if (res.data.success) {
          toast.success('Role created');
          closeModal();
          loadRoles();
        } else {
          toast.error(res.data.message ?? 'Create failed');
        }
      }
    } catch {
      toast.error('Operation failed');
    }
  }

  async function onDelete(id: number) {
    try {
      const res = await rolesApi.deleteRole(id);
      if (res.data.success) {
        toast.success('Role deleted');
        setDeleteConfirm(null);
        loadRoles();
      } else {
        toast.error(res.data.message ?? 'Delete failed');
      }
    } catch {
      toast.error('Failed to delete role');
    }
  }

  if (loading) return <LoadingSpinner />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Roles</h1>
          <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">{roles.length} total roles</p>
        </div>
        <button onClick={openCreate} className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-500">
          Add Role
        </button>
      </div>

      <div className="overflow-x-auto rounded-xl border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900">
        <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-800">
          <thead className="bg-gray-50 dark:bg-gray-800/50">
            <tr>
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-gray-500 dark:text-gray-400">ID</th>
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-gray-500 dark:text-gray-400">Name</th>
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-gray-500 dark:text-gray-400">Description</th>
              <th className="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wider text-gray-500 dark:text-gray-400">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200 dark:divide-gray-800">
            {roles.map((role) => (
              <tr key={role.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                <td className="whitespace-nowrap px-4 py-3 text-sm text-gray-500 dark:text-gray-400">{role.id}</td>
                <td className="whitespace-nowrap px-4 py-3 text-sm font-medium text-gray-900 dark:text-white">{role.name}</td>
                <td className="px-4 py-3 text-sm text-gray-500 dark:text-gray-400">{role.description ?? '—'}</td>
                <td className="whitespace-nowrap px-4 py-3 text-right">
                  <button onClick={() => openEdit(role)} className="mr-2 text-sm font-medium text-indigo-600 hover:text-indigo-500 dark:text-indigo-400">Edit</button>
                  {deleteConfirm === role.id ? (
                    <span className="inline-flex gap-1">
                      <button onClick={() => onDelete(role.id)} className="text-sm font-medium text-red-600 hover:text-red-500">Confirm</button>
                      <button onClick={() => setDeleteConfirm(null)} className="text-sm font-medium text-gray-500 hover:text-gray-400">Cancel</button>
                    </span>
                  ) : (
                    <button onClick={() => setDeleteConfirm(role.id)} className="text-sm font-medium text-red-600 hover:text-red-500 dark:text-red-400">Delete</button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Create/Edit Modal */}
      {(creating || editing) && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
          <div className="w-full max-w-md rounded-xl border border-gray-200 bg-white p-6 shadow-lg dark:border-gray-800 dark:bg-gray-900">
            <h2 className="text-lg font-bold text-gray-900 dark:text-white">{editing ? 'Edit Role' : 'Create Role'}</h2>
            <form onSubmit={handleSubmit(onSubmit)} className="mt-4 space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">Name</label>
                <input {...register('name')} className="mt-1 block w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 focus:outline-none dark:border-gray-700 dark:bg-gray-800 dark:text-white" />
                {errors.name && <p className="mt-1 text-xs text-red-500">{errors.name.message}</p>}
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">Description</label>
                <input {...register('description')} className="mt-1 block w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 focus:outline-none dark:border-gray-700 dark:bg-gray-800 dark:text-white" />
                {errors.description && <p className="mt-1 text-xs text-red-500">{errors.description.message}</p>}
              </div>
              <div className="flex justify-end gap-2 pt-2">
                <button type="button" onClick={closeModal} className="rounded-lg px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-800">Cancel</button>
                <button type="submit" disabled={isSubmitting} className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-500 disabled:opacity-50">
                  {isSubmitting ? 'Saving...' : editing ? 'Save' : 'Create'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
