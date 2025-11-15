import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { PrivacySettingsProvider } from './contexts/privacySettings'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <PrivacySettingsProvider>
      <App />
    </PrivacySettingsProvider>
  </StrictMode>,
)
