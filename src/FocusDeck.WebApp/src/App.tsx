import { ToastViewport } from './components/Toast';
import { ToastProvider } from './contexts/ToastContext';
import { SignalRProvider } from './contexts/signalR';
import { PrivacyDataProvider } from './contexts/PrivacyDataProvider';
import { WindowManagerProvider } from './contexts/WindowManagerContext';
import { DesktopLayout } from './components/OS/DesktopLayout';
import { AppShell } from './components/AppShell';
import { useIsMobile } from './hooks/useIsMobile';
import { BrowserRouter, Route, Routes, Navigate, useLocation } from 'react-router-dom';
import { SignInPage } from './pages/Auth/SignInPage';
import { getAuthToken } from './lib/utils';
import { useState, useEffect } from 'react';

// Simple Auth Guard
const ProtectedRoute = ({ children }: { children: React.ReactElement }) => {
    const location = useLocation();
    const [isAuthenticated, setIsAuthenticated] = useState<boolean | null>(null);

    useEffect(() => {
        getAuthToken()
            .then(() => setIsAuthenticated(true))
            .catch(() => setIsAuthenticated(false));
    }, [location]);

    if (isAuthenticated === null) return <div className="h-screen w-full flex items-center justify-center bg-gray-100 dark:bg-gray-900"><i className="fa-solid fa-circle-notch fa-spin text-4xl text-blue-600"></i></div>;

    if (!isAuthenticated) {
        return <Navigate to="/login" state={{ from: location }} replace />;
    }

    return children;
};

function App() {
  const isMobile = useIsMobile();

  return (
    <ToastProvider>
      <SignalRProvider>
        <PrivacyDataProvider>
          <BrowserRouter>
            <WindowManagerProvider>
               <Routes>
                   <Route path="/login" element={<SignInPage />} />
                   <Route path="/*" element={
                       <ProtectedRoute>
                           {isMobile ? <AppShell /> : <DesktopLayout />}
                       </ProtectedRoute>
                   } />
               </Routes>
              <ToastViewport />
            </WindowManagerProvider>
          </BrowserRouter>
        </PrivacyDataProvider>
      </SignalRProvider>
    </ToastProvider>
  );
}

export default App;
