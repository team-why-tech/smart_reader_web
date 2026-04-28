import { lazy, Suspense } from 'react';
import { Route } from 'react-router';
import { AdminRoute } from '../components/AdminRoute';
import { LoadingSpinner } from '../components/LoadingSpinner';

const Roles = lazy(() => import('../pages/Roles').then((m) => ({ default: m.Roles })));
const AuditLogs = lazy(() => import('../pages/AuditLogs').then((m) => ({ default: m.AuditLogs })));

export const adminRoutes = (
  <Route element={<AdminRoute />}>
    <Route path="/roles" element={<Suspense fallback={<LoadingSpinner />}><Roles /></Suspense>} />
    <Route path="/audit-logs" element={<Suspense fallback={<LoadingSpinner />}><AuditLogs /></Suspense>} />
  </Route>
);
