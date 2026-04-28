import { lazy, Suspense } from 'react';
import { Route } from 'react-router';
import { LoadingSpinner } from '../components/LoadingSpinner';

const Login = lazy(() => import('../pages/Login').then((m) => ({ default: m.Login })));
const Register = lazy(() => import('../pages/Register').then((m) => ({ default: m.Register })));

export const publicRoutes = (
  <>
    <Route path="/login" element={<Suspense fallback={<LoadingSpinner />}><Login /></Suspense>} />
    <Route path="/register" element={<Suspense fallback={<LoadingSpinner />}><Register /></Suspense>} />
  </>
);
