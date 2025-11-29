import { ToastViewport } from './components/Toast';
import { ToastProvider } from './contexts/ToastContext';
import { SignalRProvider } from './contexts/signalR';
import { PrivacyDataProvider } from './contexts/PrivacyDataProvider';
import { WindowManagerProvider } from './contexts/WindowManagerContext';
import { DesktopLayout } from './components/OS/DesktopLayout';

function App() {
  return (
    <ToastProvider>
      <SignalRProvider>
        <PrivacyDataProvider>
          <WindowManagerProvider>
            <DesktopLayout />
            <ToastViewport />
          </WindowManagerProvider>
        </PrivacyDataProvider>
      </SignalRProvider>
    </ToastProvider>
  );
}

export default App;
