import { useEffect, useState } from 'react';
import * as auditApi from '../api/auditLogs';
import type { AuditLogDto } from '../types';
import toast from 'react-hot-toast';
import { LoadingSpinner } from '../components/LoadingSpinner';

export function AuditLogs() {
  const [logs, setLogs] = useState<AuditLogDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [filterUserId, setFilterUserId] = useState('');

  useEffect(() => {
    loadLogs();
  }, []);

  async function loadLogs() {
    setLoading(true);
    try {
      const res = await auditApi.getAuditLogs();
      if (res.data.success) setLogs(res.data.data ?? []);
    } catch {
      toast.error('Failed to load audit logs');
    } finally {
      setLoading(false);
    }
  }

  async function filterByUser() {
    const userId = parseInt(filterUserId);
    if (isNaN(userId)) {
      loadLogs();
      return;
    }
    setLoading(true);
    try {
      const res = await auditApi.getAuditLogsByUser(userId);
      if (res.data.success) setLogs(res.data.data ?? []);
    } catch {
      toast.error('Failed to filter logs');
    } finally {
      setLoading(false);
    }
  }

  if (loading) return <LoadingSpinner />;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Audit Logs</h1>
        <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">{logs.length} log entries</p>
      </div>

      <div className="flex items-center gap-2">
        <input
          type="number"
          placeholder="Filter by User ID"
          value={filterUserId}
          onChange={(e) => setFilterUserId(e.target.value)}
          className="w-48 rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 focus:outline-none dark:border-gray-700 dark:bg-gray-800 dark:text-white"
        />
        <button onClick={filterByUser} className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-500">Filter</button>
        <button
          onClick={() => { setFilterUserId(''); loadLogs(); }}
          className="rounded-lg px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-800"
        >
          Clear
        </button>
      </div>

      <div className="overflow-x-auto rounded-xl border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900">
        <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-800">
          <thead className="bg-gray-50 dark:bg-gray-800/50">
            <tr>
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-gray-500 dark:text-gray-400">ID</th>
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-gray-500 dark:text-gray-400">Action</th>
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-gray-500 dark:text-gray-400">Entity</th>
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-gray-500 dark:text-gray-400">Entity ID</th>
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-gray-500 dark:text-gray-400">User ID</th>
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-gray-500 dark:text-gray-400">Timestamp</th>
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-gray-500 dark:text-gray-400">Details</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200 dark:divide-gray-800">
            {logs.map((log) => (
              <tr key={log.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                <td className="whitespace-nowrap px-4 py-3 text-sm text-gray-500 dark:text-gray-400">{log.id}</td>
                <td className="whitespace-nowrap px-4 py-3">
                  <span className={`inline-flex rounded-full px-2 py-0.5 text-xs font-medium ${
                    log.action.toLowerCase().includes('create') ? 'bg-green-100 text-green-700 dark:bg-green-900/40 dark:text-green-300' :
                    log.action.toLowerCase().includes('delete') ? 'bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-300' :
                    log.action.toLowerCase().includes('update') ? 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/40 dark:text-yellow-300' :
                    'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300'
                  }`}>
                    {log.action}
                  </span>
                </td>
                <td className="whitespace-nowrap px-4 py-3 text-sm text-gray-900 dark:text-white">{log.entityName}</td>
                <td className="whitespace-nowrap px-4 py-3 text-sm text-gray-500 dark:text-gray-400">{log.entityId ?? '—'}</td>
                <td className="whitespace-nowrap px-4 py-3 text-sm text-gray-500 dark:text-gray-400">{log.userId ?? '—'}</td>
                <td className="whitespace-nowrap px-4 py-3 text-sm text-gray-500 dark:text-gray-400">
                  {new Date(log.timestamp).toLocaleString()}
                </td>
                <td className="max-w-xs truncate px-4 py-3 text-sm text-gray-500 dark:text-gray-400" title={log.details ?? ''}>
                  {log.details ?? '—'}
                </td>
              </tr>
            ))}
            {logs.length === 0 && (
              <tr>
                <td colSpan={7} className="px-4 py-8 text-center text-sm text-gray-500 dark:text-gray-400">No audit logs found</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
