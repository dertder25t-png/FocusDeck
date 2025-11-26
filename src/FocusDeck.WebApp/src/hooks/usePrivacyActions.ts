import { useState } from 'react';
import { apiFetch } from '../lib/utils';
import { useToast } from './useToast';

export const usePrivacyActions = () => {
  const [isExporting, setIsExporting] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [isConfirming, setIsConfirming] = useState(false);
  const { addToast } = useToast();

  const exportAllData = async () => {
    setIsExporting(true);
    try {
      const response = await apiFetch('/v1/privacy/export', { method: 'POST' });
      if (response.ok) {
        addToast({ title: 'Export Initiated', description: 'Your data export is being prepared.', variant: 'success' });
      } else {
        addToast({ title: 'Export Failed', description: 'Could not initiate the data export.', variant: 'error' });
      }
    } catch (error) {
      console.error('Error exporting data:', error);
      addToast({ title: 'Error', description: 'An unexpected error occurred while exporting data.', variant: 'error' });
    } finally {
      setIsExporting(false);
    }
  };

  const deleteAllData = async () => {
    setIsDeleting(true);
    try {
      const response = await apiFetch('/v1/privacy/delete-all', { method: 'POST' });
      if (response.ok) {
        addToast({ title: 'Data Deleted', description: 'All your data has been successfully deleted.', variant: 'success' });
      } else {
        addToast({ title: 'Deletion Failed', description: 'Could not delete your data.', variant: 'error' });
    }
    } catch (error) {
      console.error('Error deleting data:', error);
      addToast({ title: 'Error', description: 'An unexpected error occurred while deleting data.', variant: 'error' });
    } finally {
      setIsDeleting(false);
      setIsConfirming(false);
    }
  };

  return {
    exportAllData,
    deleteAllData: () => setIsConfirming(true),
    isExporting,
    isDeleting,
    isConfirming,
    setIsConfirming,
    confirmDelete: deleteAllData,
  };
};
