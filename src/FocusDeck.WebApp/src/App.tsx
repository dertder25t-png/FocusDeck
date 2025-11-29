import { ToastViewport } from './components/Toast';
import { ToastProvider } from './contexts/ToastContext';
import { SignalRProvider } from './contexts/signalR';
import { PrivacyDataProvider } from './contexts/PrivacyDataProvider';
import { WindowManagerProvider } from './contexts/WindowManagerContext';
import { DesktopLayout } from './components/OS/DesktopLayout';
import { AppShell } from './components/AppShell';
import { useIsMobile } from './hooks/useIsMobile';
import { BrowserRouter, Route, Routes } from 'react-router-dom';

function App() {
  const isMobile = useIsMobile();

  return (
    <ToastProvider>
      <SignalRProvider>
        <PrivacyDataProvider>
          <BrowserRouter>
            <WindowManagerProvider>
              {isMobile ? (
                  <Routes>
                    <Route path="/*" element={<AppShell />} />
                  </Routes>
              ) : (
                <DesktopLayout />
              )}
              <ToastViewport />
            </WindowManagerProvider>
          </BrowserRouter>
        </PrivacyDataProvider>
      </SignalRProvider>
    </ToastProvider>
  );
}

export default App;
