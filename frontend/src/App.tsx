import { Navigate, Route, Routes } from 'react-router';
import { LoginPage } from './features/auth/LoginPage';
import { DashboardPage } from './features/dashboard/DashboardPage';
import { ProspectsListPage } from './features/prospects/ProspectsListPage';
import { ProspectDetailPage } from './features/prospects/ProspectDetailPage';
import { LeadsKanbanPage } from './features/leads/LeadsKanbanPage';
import { LeadDetailPage } from './features/leads/LeadDetailPage';
import { ProtectedRoute } from './components/ProtectedRoute';
import { Layout } from './components/Layout';

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      <Route
        path="/app/dashboard"
        element={
          <ProtectedRoute>
            <Layout>
              <DashboardPage />
            </Layout>
          </ProtectedRoute>
        }
      />

      <Route
        path="/app/prospects"
        element={
          <ProtectedRoute>
            <Layout>
              <ProspectsListPage />
            </Layout>
          </ProtectedRoute>
        }
      />

      <Route
        path="/app/prospects/:id"
        element={
          <ProtectedRoute>
            <Layout>
              <ProspectDetailPage />
            </Layout>
          </ProtectedRoute>
        }
      />

      <Route
        path="/app/leads"
        element={
          <ProtectedRoute>
            <Layout>
              <LeadsKanbanPage />
            </Layout>
          </ProtectedRoute>
        }
      />

      <Route
        path="/app/leads/:id"
        element={
          <ProtectedRoute>
            <Layout>
              <LeadDetailPage />
            </Layout>
          </ProtectedRoute>
        }
      />

      <Route path="/" element={<Navigate to="/app/dashboard" replace />} />
      <Route path="*" element={<Navigate to="/app/dashboard" replace />} />
    </Routes>
  );
}
