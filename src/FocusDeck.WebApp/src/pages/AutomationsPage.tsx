import { useEffect, useState } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/Card';
import { Button } from '../components/Button';
import { Badge } from '../components/Badge';
import { Link } from 'react-router-dom';
import { AutomationEditor } from './AutomationEditor';
import { AutomationHistory } from './AutomationHistory';
import { apiFetch } from '../lib/api';

interface Automation {
  id: string;
  name: string;
  description: string | null;
  isEnabled: boolean;
  lastRunAt: string | null;
  yamlDefinition: string;
}

export function AutomationsPage() {
  const [automations, setAutomations] = useState<Automation[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showEditor, setShowEditor] = useState(false);
  const [showHistory, setShowHistory] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [historyId, setHistoryId] = useState<string | null>(null);

  useEffect(() => {
    fetchAutomations();
  }, []);

  const fetchAutomations = async () => {
    try {
      const data = await apiFetch('/v1/automations');
      setAutomations(data);
    } catch (error) {
      setError('Failed to fetch automations');
      console.error('Failed to fetch automations', error);
    } finally {
      setLoading(false);
    }
  };

  const toggleAutomation = async (id: string) => {
    try {
      const data = await apiFetch(`/v1/automations/${id}/toggle`, { method: 'PATCH' });
      setAutomations(prev => prev.map(a =>
        a.id === id ? { ...a, isEnabled: data.isEnabled } : a
      ));
    } catch (error) {
      console.error('Failed to toggle automation', error);
    }
  };

  const deleteAutomation = async (id: string) => {
    if (!confirm('Are you sure you want to delete this automation?')) return;

    try {
      await apiFetch(`/v1/automations/${id}`, { method: 'DELETE' });
      setAutomations(prev => prev.filter(a => a.id !== id));
    } catch (error) {
      console.error('Failed to delete automation', error);
    }
  };

  const openEditor = (id?: string) => {
    setEditingId(id || null);
    setShowEditor(true);
  };

  const closeEditor = () => {
    setShowEditor(false);
    setEditingId(null);
  };

  const openHistory = (id: string) => {
    setHistoryId(id);
    setShowHistory(true);
  };

  const closeHistory = () => {
    setShowHistory(false);
    setHistoryId(null);
  };

  const handleSaved = () => {
    closeEditor();
    fetchAutomations();
  };

  if (loading) return <div className="p-8 text-center text-gray-400">Loading automations...</div>;
  if (error) return <div className="p-8 text-center text-red-400">{error}</div>;

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-2xl font-semibold">Active Automations</h1>
          <p className="text-sm text-gray-400 mt-1">
            Manage your running automation rules.
          </p>
        </div>
        <div className="flex gap-3">
          <Button onClick={() => openEditor()}>Create New</Button>
          <Link to="/automations/proposals">
            <Button variant="secondary">View Proposals</Button>
          </Link>
        </div>
      </div>

      {automations.length === 0 ? (
        <div className="text-center py-12 border border-dashed border-gray-700 rounded-lg bg-surface-100/50">
          <p className="text-gray-400 mb-4">No active automations.</p>
          <div className="flex justify-center gap-3">
            <Button onClick={() => openEditor()}>Create Manually</Button>
            <Link to="/automations/proposals">
              <Button variant="secondary">Check Proposals</Button>
            </Link>
          </div>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {automations.map(automation => (
            <Card key={automation.id} className={`overflow-hidden border ${automation.isEnabled ? 'border-green-500/50' : 'border-gray-700'}`}>
              <CardHeader>
                <div className="flex items-center justify-between">
                  <CardTitle className="cursor-pointer hover:text-primary" onClick={() => openEditor(automation.id)}>
                    {automation.name}
                  </CardTitle>
                  <label className="flex items-center cursor-pointer" title="Toggle Automation">
                    <div className="relative">
                      <input
                        type="checkbox"
                        className="sr-only"
                        checked={automation.isEnabled}
                        onChange={() => toggleAutomation(automation.id)}
                      />
                      <div className={`block w-10 h-6 rounded-full transition-colors ${automation.isEnabled ? 'bg-primary' : 'bg-gray-700'}`}></div>
                      <div className={`dot absolute left-1 top-1 bg-white w-4 h-4 rounded-full transition-transform ${automation.isEnabled ? 'transform translate-x-4' : ''}`}></div>
                    </div>
                  </label>
                </div>
                <CardDescription>{automation.description || "No description"}</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="flex items-center justify-between text-xs text-gray-500">
                  <span>
                    Last run: {automation.lastRunAt
                      ? new Date(automation.lastRunAt).toLocaleString()
                      : 'Never'}
                  </span>
                  <div className="flex items-center gap-2">
                    <Button variant="ghost" size="sm" onClick={() => openHistory(automation.id)}>History</Button>
                    <Button variant="ghost" size="sm" onClick={() => openEditor(automation.id)}>Edit</Button>
                    <Button variant="ghost" size="sm" className="text-red-400 hover:text-red-300" onClick={() => deleteAutomation(automation.id)}>Delete</Button>
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {showEditor && (
        <AutomationEditor
          automationId={editingId}
          onClose={closeEditor}
          onSaved={handleSaved}
        />
      )}

      {showHistory && historyId && (
        <AutomationHistory
          automationId={historyId}
          onClose={closeHistory}
        />
      )}
    </div>
  )
}
