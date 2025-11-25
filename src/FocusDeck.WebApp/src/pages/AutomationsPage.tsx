import { useEffect, useState } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/Card';
import { apiFetch } from '../lib/api';

interface Automation {
  id: string;
  name: string;
  isEnabled: boolean;
  lastRunAt: string | null;
}

export function AutomationsPage() {
  const [automations, setAutomations] = useState<Automation[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchAutomations = async () => {
      try {
        const response = await apiFetch('/v1/automations');
        setAutomations(response);
      } catch (err) {
        setError('Failed to fetch automations');
        console.error(err);
      } finally {
        setLoading(false);
      }
    };

    fetchAutomations();
  }, []);

  if (loading) {
    return <div>Loading...</div>;
  }

  if (error) {
    return <div>{error}</div>;
  }

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle>Automations</CardTitle>
          <CardDescription>A card-based grid showing active rules.</CardDescription>
        </CardHeader>
      </Card>
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {automations.map((automation) => (
          <Card key={automation.id} className={automation.isEnabled ? 'border-green-500' : 'border-gray-500'}>
            <CardHeader>
              <CardTitle>{automation.name}</CardTitle>
            </CardHeader>
            <CardContent>
              <p>Status: {automation.isEnabled ? 'Active' : 'Paused'}</p>
              <p>Last Run: {automation.lastRunAt ? new Date(automation.lastRunAt).toLocaleString() : 'Never'}</p>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}
