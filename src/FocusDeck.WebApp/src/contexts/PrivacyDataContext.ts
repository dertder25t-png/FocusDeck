import { createContext } from 'react';

interface PrivacyDataContextType {
  data: Record<string, string>;
}

export const PrivacyDataContext = createContext<PrivacyDataContextType | null>(null);
