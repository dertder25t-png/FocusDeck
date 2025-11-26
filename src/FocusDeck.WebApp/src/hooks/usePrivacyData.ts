import { useContext } from 'react';
import { PrivacyDataContext } from '../contexts/PrivacyDataContext';

export function usePrivacyData() {
  const context = useContext(PrivacyDataContext);
  if (!context) {
    throw new Error('usePrivacyData must be used within a PrivacyDataProvider');
  }
  return context;
}
