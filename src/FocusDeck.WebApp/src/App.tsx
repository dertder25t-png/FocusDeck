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

function App() {
  const isMobile = useIsMobile();

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
                       <Route path="/*" element={isMobile ? <AppShell /> : <DesktopLayout />} />
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
