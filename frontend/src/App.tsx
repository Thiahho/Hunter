import { Navigate, Route, Routes } from 'react-router';
import { LoginPage } from './features/auth/LoginPage';
import { DashboardPage } from './features/dashboard/DashboardPage';
import { ProspectsListPage } from './features/prospects/ProspectsListPage';
import { ProspectDetailPage } from './features/prospects/ProspectDetailPage';
import { ProspectCreatePage } from './features/prospects/ProspectCreatePage';
import { ProspectSearchPage } from './features/prospects/ProspectSearchPage';
import { LeadsKanbanPage } from './features/leads/LeadsKanbanPage';
import { LeadDetailPage } from './features/leads/LeadDetailPage';
import { UsersPage } from './features/users/UsersPage';
import { MessagesPage } from './features/messages/MessagesPage';
import { TemplatesPage } from './features/templates/TemplatesPage';
import { ProfilePage } from './features/profile/ProfilePage';
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
        path="/app/prospects/new"
        element={
          <ProtectedRoute>
            <Layout>
              <ProspectCreatePage />
            </Layout>
          </ProtectedRoute>
        }
      />

      <Route
        path="/app/prospects/search"
        element={
          <ProtectedRoute>
            <Layout>
              <ProspectSearchPage />
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

      <Route
        path="/app/users"
        element={
          <ProtectedRoute allowedRoles={['OWNER', 'ADMIN']}>
            <Layout>
              <UsersPage />
            </Layout>
          </ProtectedRoute>
        }
      />

      <Route
        path="/app/profile"
        element={
          <ProtectedRoute>
            <Layout>
              <ProfilePage />
            </Layout>
          </ProtectedRoute>
        }
      />

      <Route
        path="/app/messages"
        element={
          <ProtectedRoute>
            <Layout>
              <MessagesPage />
            </Layout>
          </ProtectedRoute>
        }
      />

      <Route
        path="/app/templates"
        element={
          <ProtectedRoute>
            <Layout>
              <TemplatesPage />
            </Layout>
          </ProtectedRoute>
        }
      />

      <Route path="/" element={<Navigate to="/app/dashboard" replace />} />
      <Route path="*" element={<Navigate to="/app/dashboard" replace />} />
    </Routes>
  );
}
