import { ToastViewport } from './components/Toast';
import { ToastProvider } from './contexts/ToastContext';
import { SignalRProvider } from './contexts/signalR';
import { PrivacyDataProvider } from './contexts/PrivacyDataProvider';
import { FocusProvider } from './contexts/FocusContext';
import { WindowManagerProvider } from './contexts/WindowManagerContext';
import { DesktopLayout } from './components/OS/DesktopLayout';
import { AppShell } from './components/AppShell';
import { useIsMobile } from './hooks/useIsMobile';
import { BrowserRouter, Route, Routes } from 'react-router-dom';
import { SignInPage } from './pages/Auth/SignInPage';
import { RegisterPage } from './pages/Auth/RegisterPage';
import { ProtectedRoute } from './pages/Auth/ProtectedRoute';

// Pages
import { DashboardPage } from './pages/DashboardPage';
import { NotesPage } from './pages/NotesPage';
import { KanbanPage } from './pages/KanbanPage';
import { AnalyticsPage } from './pages/AnalyticsPage';
import { FlashcardsApp } from './apps/FlashcardsApp'; // Using App directly for page if needed

function MainLayout() {
  const isMobile = useIsMobile();

  if (isMobile) {
    return (
      <Routes>
        <Route element={<AppShell />}>
          <Route index element={<DashboardPage />} />
          <Route path="dashboard" element={<DashboardPage />} />
          <Route path="notes" element={<NotesPage />} />
          <Route path="kanban" element={<KanbanPage />} />
          <Route path="analytics" element={<AnalyticsPage />} />
          <Route path="flashcards" element={<FlashcardsApp />} />
          {/* Add other mobile routes here */}
        </Route>
      </Routes>
    );
  }

  return <DesktopLayout />;
}

function App() {
  return (
    <ToastProvider>
      <SignalRProvider>
        <PrivacyDataProvider>
          <FocusProvider>
            <BrowserRouter>
              <WindowManagerProvider>
                <Routes>
                   <Route path="/login" element={<SignInPage />} />
                   <Route path="/register" element={<RegisterPage />} />
                   
                   {/* Use robust ProtectedRoute with token validation */}
                   <Route element={<ProtectedRoute />}>
                       <Route path="/*" element={<MainLayout />} />
                   </Route>
                </Routes>
                <ToastViewport />
              </WindowManagerProvider>
            </BrowserRouter>
          </FocusProvider>
        </PrivacyDataProvider>
      </SignalRProvider>
    </ToastProvider>
  );
}

export default App;
