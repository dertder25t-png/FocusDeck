import React from 'react';
import { usePrivacySettings } from '../hooks/usePrivacySettings';
import { usePrivacyData } from '../hooks/usePrivacyData';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '../components/Card';
import { Button } from '../components/Button';

const PrivacyDashboardPage: React.FC = () => {
  const { settings, loading, updateSetting } = usePrivacySettings();
  const { data } = usePrivacyData();

  return (
    <div className="p-6 max-w-4xl mx-auto">
      <div className="mb-8">
        <h1 className="text-3xl font-bold">Privacy Dashboard</h1>
        <p className="mt-2 text-gray-400">
          This page provides a live preview of the data being collected by each sensor when enabled.
        </p>
      </div>

      <div className="space-y-6">
        {loading ? (
          <div className="text-center py-12 text-gray-400">Loading privacy settings...</div>
        ) : (
          settings.map((setting) => (
            <Card key={setting.contextType} className="bg-surface-100 border border-gray-800">
              <CardHeader>
                <div className="flex items-center justify-between">
                  <div>
                    <CardTitle>{setting.displayName}</CardTitle>
                    <CardDescription className="mt-1">{setting.description}</CardDescription>
                  </div>
                  <Button
                    size="sm"
                    variant={setting.isEnabled ? 'secondary' : 'primary'}
                    onClick={() => updateSetting(setting.contextType, !setting.isEnabled)}
                  >
                    {setting.isEnabled ? 'Disable' : 'Enable'}
                  </Button>
                </div>
              </CardHeader>
              <CardContent>
                <div className="mt-4 p-4 rounded-lg bg-gray-900/50 border border-gray-700/50">
                  <h4 className="text-sm font-semibold mb-2 text-gray-300">Live Data Preview</h4>
                  <div className="text-sm text-gray-400 font-mono min-h-[2.5rem] flex items-center p-2 bg-black/20 rounded">
                    {data[setting.contextType] || 'Waiting for data...'}
                  </div>
                </div>
              </CardContent>
            </Card>
          ))
        )}
      </div>
    </div>
  );
};

export default PrivacyDashboardPage;
