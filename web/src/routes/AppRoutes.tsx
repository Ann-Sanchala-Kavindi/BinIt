import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthLayout } from '../layouts/AuthLayout';
import { AppLayout } from '../layouts/AppLayout';
import { LoginPage } from '../features/auth/pages/LoginPage';
import { HomePage } from '../pages/HomePage';
import { UnauthorizedPage } from '../pages/UnauthorizedPage';
import { NotFoundPage } from '../pages/NotFoundPage';
import { ProtectedRoute } from './ProtectedRoute';
import { RoleRoute } from './RoleRoute';
import { useAuthStore } from '../store/authStore';
import { getDefaultRouteForRole } from './routeUtils';
import { LoadingSpinner } from '../components/ui/LoadingSpinner';

import { DashboardLayout } from '../layouts/DashboardLayout';
import { OfficerDashboardPage } from '../features/officer/pages/OfficerDashboardPage';
import { ManagerDashboardPage } from '../features/manager/pages/ManagerDashboardPage';
import { UserManagementPage } from '../features/manager/pages/UserManagementPage';
import { ChangePasswordPage } from '../features/auth/pages/ChangePasswordPage';
import { managerNavItems } from '../components/layout/navConfig';
import { ComingSoonPage } from '../pages/ComingSoonPage';
import { WasteReportsPage } from '../features/reporting/pages/WasteReportsPage';
import { WasteReportDetailPage } from '../features/reporting/pages/WasteReportDetailPage';

/**
 * Renders auth pages or redirects already-authenticated users to their role default dashboard.
 * Holds session restoration state to prevent intermediate redirect flashes.
 */
const PublicAuthRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { isAuthenticated, isLoading, user } = useAuthStore();

  if (isLoading && isAuthenticated) {
    return (
      <div
        className="py-12 flex flex-col items-center justify-center text-slate-500 gap-3"
        role="status"
        aria-label="Verifying session"
      >
        <LoadingSpinner size="md" label="Verifying session" />
        <p className="text-sm text-slate-500">Verifying session...</p>
      </div>
    );
  }

  if (isAuthenticated) {
    const destination = user?.mustChangePassword
      ? '/account/change-password'
      : getDefaultRouteForRole(user?.role);
    return <Navigate to={destination} replace />;
  }

  return <>{children}</>;
};

/**
 * Root route handler (/).
 * If the authenticated role has a dedicated dashboard (e.g. WasteOfficer),
 * redirect directly to that dashboard. Otherwise, render HomePage fallback.
 */
const RootRoute: React.FC = () => {
  const { user } = useAuthStore();
  if (user?.mustChangePassword) {
    return <Navigate to="/account/change-password" replace />;
  }
  const defaultRoute = getDefaultRouteForRole(user?.role);

  if (defaultRoute !== '/') {
    return <Navigate to={defaultRoute} replace />;
  }

  return <HomePage />;
};

const defaultQueryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
      refetchOnWindowFocus: false,
    },
  },
});

export const AppRoutes: React.FC = () => {
  return (
    <QueryClientProvider client={defaultQueryClient}>
      <Routes>
      {/* Public / Auth routes */}
      <Route element={<AuthLayout />}>
        <Route
          path="/login"
          element={
            <PublicAuthRoute>
              <LoginPage />
            </PublicAuthRoute>
          }
        />
        <Route path="/register" element={<Navigate to="/login" replace />} />
      </Route>

      {/* WasteOfficer protected routes wrapped in DashboardLayout */}
      <Route element={<ProtectedRoute />}>
        <Route element={<RoleRoute allowedRoles={['WasteOfficer']} />}>
          <Route element={<DashboardLayout />}>
            <Route path="/officer/dashboard" element={<OfficerDashboardPage />} />
            <Route path="/officer/waste-reports" element={<WasteReportsPage />} />
            <Route path="/officer/waste-reports/:id" element={<WasteReportDetailPage />} />
            <Route path="/officer/reports" element={<Navigate to="/officer/waste-reports" replace />} />
            <Route path="/officer/reports/:id" element={<WasteReportDetailPage />} />
            <Route
              path="/officer/bins"
              element={
                <ComingSoonPage
                  title="Bin Management"
                  description="Manage waste bins, locations, and operational status. This module is currently under development."
                  backTo="/officer/dashboard"
                />
              }
            />
            <Route
              path="/officer/schedules"
              element={
                <ComingSoonPage
                  title="Collection Schedules"
                  description="Create and manage scheduled waste collections. This module is currently under development."
                  backTo="/officer/dashboard"
                />
              }
            />
            <Route
              path="/officer/tasks"
              element={
                <ComingSoonPage
                  title="Collection Tasks"
                  description="Track and manage operational collection tasks. This module is currently under development."
                  backTo="/officer/dashboard"
                />
              }
            />
            <Route
              path="/officer/complaints"
              element={
                <ComingSoonPage
                  title="Complaints"
                  description="Review and manage citizen service complaints. This module is currently under development."
                  backTo="/officer/dashboard"
                />
              }
            />
          </Route>
        </Route>
      </Route>

      {/* MunicipalManager protected routes wrapped in DashboardLayout */}
      <Route element={<ProtectedRoute />}>
        <Route element={<RoleRoute allowedRoles={['MunicipalManager']} />}>
          <Route element={<DashboardLayout navItems={managerNavItems} title="Municipal Manager Dashboard" />}>
            <Route path="/manager/dashboard" element={<ManagerDashboardPage />} />
            <Route path="/manager/reports" element={<WasteReportsPage />} />
            <Route path="/manager/reports/:id" element={<WasteReportDetailPage />} />
            <Route path="/manager/waste-reports" element={<Navigate to="/manager/reports" replace />} />
            <Route path="/manager/waste-reports/:id" element={<WasteReportDetailPage />} />
            <Route path="/manager/users" element={<UserManagementPage />} />
            <Route
              path="/manager/ai-approvals"
              element={
                <ComingSoonPage
                  title="AI Approvals"
                  description="Review and adjudicate multi-agent AI workflow proposals and recommendations. This module is currently under development."
                  backTo="/manager/dashboard"
                />
              }
            />
            <Route
              path="/manager/fleet"
              element={
                <ComingSoonPage
                  title="Fleet & Routes"
                  description="Management visibility into collection vehicles, driver assignments, and active routes. This module is currently under development."
                  backTo="/manager/dashboard"
                />
              }
            />
            <Route
              path="/manager/operations"
              element={
                <ComingSoonPage
                  title="Operations"
                  description="Higher-level operational oversight, field incident tracking, and active collections. This module is currently under development."
                  backTo="/manager/dashboard"
                />
              }
            />
            <Route
              path="/manager/analytics"
              element={
                <ComingSoonPage
                  title="Analytics"
                  description="Aggregated municipal operational metrics, service performance, and reporting. This module is currently under development."
                  backTo="/manager/dashboard"
                />
              }
            />
            <Route
              path="/manager/audit"
              element={
                <ComingSoonPage
                  title="Audit Logs"
                  description="Auditable activity history for AI recommendations, approvals, and executive decisions. This module is currently under development."
                  backTo="/manager/dashboard"
                />
              }
            />
          </Route>
        </Route>
      </Route>

      {/* Shared Change Password route */}
      <Route element={<ProtectedRoute />}>
        <Route element={<DashboardLayout title="Change Password" />}>
          <Route path="/account/change-password" element={<ChangePasswordPage />} />
        </Route>
      </Route>

      {/* Protected routes wrapped in AppLayout */}
      <Route element={<ProtectedRoute />}>
        <Route element={<AppLayout />}>
          <Route path="/" element={<RootRoute />} />

          {/* Verification role routes */}
          <Route
            element={
              <RoleRoute allowedRoles={['MunicipalManager']} />
            }
          >
            <Route
              path="/manager-test"
              element={
                <div className="p-6 bg-white rounded-xl border border-slate-200 shadow-sm text-slate-800 font-medium">
                  Municipal Manager Exclusive Area
                </div>
              }
            />
          </Route>
        </Route>
      </Route>

      {/* Error / info routes */}
      <Route path="/unauthorized" element={<UnauthorizedPage />} />
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
    </QueryClientProvider>
  );
};
