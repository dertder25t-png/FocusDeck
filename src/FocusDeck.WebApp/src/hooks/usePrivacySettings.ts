import { useContext } from 'react';
import { PrivacySettingsContext } from '../contexts/privacySettings';

export function usePrivacySettings() {
  const context = useContext(PrivacySettingsContext);
  if (!context) {
    throw new Error('usePrivacySettings must be used inside PrivacySettingsProvider');
  }
  return context;
}
