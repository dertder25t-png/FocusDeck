import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { WorkspaceProvider } from './contexts/WorkspaceProvider'
import { PrivacySettingsProvider } from './contexts/privacySettings'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'

const queryClient = new QueryClient()

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <PrivacySettingsProvider>
        <WorkspaceProvider>
          <App />
        </WorkspaceProvider>
      </PrivacySettingsProvider>
    </QueryClientProvider>
  </StrictMode>,
)
