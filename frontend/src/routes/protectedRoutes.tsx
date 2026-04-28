import { lazy, Suspense } from 'react';
import { Route } from 'react-router';
import { ProtectedRoute } from '../components/ProtectedRoute';
import { Layout } from '../components/Layout';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { adminRoutes } from './adminRoutes';

const Dashboard = lazy(() => import('../pages/Dashboard').then((m) => ({ default: m.Dashboard })));
const Users = lazy(() => import('../pages/Users').then((m) => ({ default: m.Users })));

export const protectedRoutes = (
  <Route element={<ProtectedRoute />}>
    <Route element={<Layout />}>
      <Route path="/" element={<Suspense fallback={<LoadingSpinner />}><Dashboard /></Suspense>} />
      <Route path="/users" element={<Suspense fallback={<LoadingSpinner />}><Users /></Suspense>} />
      {adminRoutes}
    </Route>
  </Route>
);
