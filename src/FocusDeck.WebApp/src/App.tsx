import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { LoginPage } from './pages/Auth/LoginPage';
import { RegisterPage } from './pages/Auth/RegisterPage';
import { ToastViewport } from './components/Toast';
import { ToastProvider } from './contexts/ToastContext';
import { DashboardPage } from './pages/DashboardPage';
import { LecturesPage } from './pages/LecturesPage';
import { FocusPage } from './pages/FocusPage';
import { NotesPage } from './pages/NotesPage';
import { DesignPage } from './pages/DesignPage';
import { AnalyticsPage } from './pages/AnalyticsPage';
import { JarvisPage } from './pages/JarvisPage';
import { SettingsPage } from './pages/SettingsPage';
import PrivacyDashboardPage from './pages/PrivacyDashboardPage';
import { TenantsPage } from './pages/TenantsPage';
import { JobsPage } from './pages/JobsPage';
import { AutomationsPage } from './pages/AutomationsPage';
import { AutomationProposalsPage } from './pages/AutomationProposalsPage';
import { DevicesPage } from './pages/DevicesPage';
import { PairingPage } from './pages/Auth/PairingPage';
import { MorningBriefingPage } from './pages/MorningBriefingPage';
import ProvisioningPage from './pages/ProvisioningPage';
import { ProtectedRoute } from './pages/Auth/ProtectedRoute';
import { SignalRProvider } from './contexts/signalR';
import { AppShell } from './components/AppShell';
import { KanbanPage } from './pages/KanbanPage';
import { CustomizationPage } from './pages/CustomizationPage';
import { PageBuilder } from './pages/PageBuilder';
import { WidgetBuilder } from './pages/WidgetBuilder';
import { PrivacyDataProvider } from './contexts/PrivacyDataProvider';

function App() {
  return (
    <ToastProvider>
      <SignalRProvider>
        <BrowserRouter>
          <Routes>
            {/* Public Routes */}
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />

            {/* Protected Routes - All require authentication */}
            <Route element={<ProtectedRoute />}>
              <Route element={<AppShell />}>
                {/* Dashboard */}
                <Route index element={<DashboardPage />} />
                <Route path="morning" element={<MorningBriefingPage />} />

                {/* Main Features */}
                <Route path="lectures" element={<LecturesPage />} />
                <Route path="focus" element={<FocusPage />} />
                <Route path="notes" element={<NotesPage />} />
                <Route path="design" element={<DesignPage />} />
                <Route path="analytics" element={<AnalyticsPage />} />
                <Route path="jarvis" element={<JarvisPage />} />
                <Route path="automations" element={<AutomationsPage />} />
                <Route path="automations/proposals" element={<AutomationProposalsPage />} />
                <Route path="projects/:projectId/board" element={<KanbanPage />} />
                <Route path="customize" element={<CustomizationPage />} />
                <Route path="customize/pages/new" element={<PageBuilder />} />
                <Route path="customize/widgets/new" element={<WidgetBuilder />} />

                {/* Settings & Management */}
                <Route path="settings" element={<SettingsPage />} />
                <Route path="settings/privacy" element={<PrivacyDataProvider><PrivacyDashboardPage /></PrivacyDataProvider>} />
                <Route path="devices" element={<DevicesPage />} />
                <Route path="pairing" element={<PairingPage />} />
                <Route path="provisioning" element={<ProvisioningPage />} />

                {/* Admin Routes */}
                <Route path="tenants" element={<TenantsPage />} />
                <Route path="jobs" element={<JobsPage />} />
              </Route>
            </Route>

            {/* Fallback: Redirect unknown paths to home */}
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
          <ToastViewport />
        </BrowserRouter>
      </SignalRProvider>
    </ToastProvider>
  );
}

export default App;
