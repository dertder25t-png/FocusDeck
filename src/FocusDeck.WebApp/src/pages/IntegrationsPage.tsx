import { useState } from 'react';
import { Button } from '../components/Button';
import { Input } from '../components/Input';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/Card';
import { Badge } from '../components/Badge';

export function IntegrationsPage() {
  const [googleConnected, setGoogleConnected] = useState(false);
  const [canvasConnected, setCanvasConnected] = useState(false);
  const [whisperPath] = useState('/models/ggml-base.en.bin');
  const [aiProvider, setAiProvider] = useState<'openai' | 'anthropic' | 'none'>('none');
  const [apiKey, setApiKey] = useState('');
  const [showApiKey, setShowApiKey] = useState(false);

  const handleConnectGoogle = () => {
    // OAuth flow would happen here
    window.location.href = '/v1/auth/google-oauth-start';
  };

  const handleConnectCanvas = () => {
    // Canvas OAuth flow
    const canvasUrl = prompt('Enter your Canvas instance URL (e.g., canvas.university.edu)');
    if (canvasUrl) {
      window.location.href = `/v1/integrations/canvas/oauth?instance=${canvasUrl}`;
    }
  };

  const handleSaveAiKey = async () => {
    if (!apiKey) return;
    
    try {
      await fetch('/v1/integrations/ai-provider', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${localStorage.getItem('token')}`
        },
        body: JSON.stringify({ provider: aiProvider, apiKey })
      });
      
      setApiKey('');
      setShowApiKey(false);
      alert('AI provider key saved successfully');
    } catch (err) {
      console.error('Failed to save AI key:', err);
    }
  };

  const maskKey = (key: string) => {
    if (!key || key.length < 8) return '••••••••';
    return `${key.substring(0, 4)}${'•'.repeat(key.length - 8)}${key.substring(key.length - 4)}`;
  };

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-3xl font-semibold text-white">Integrations</h1>
        <p className="text-gray-400 mt-2">Connect external services and configure AI providers</p>
      </div>

      {/* Google Integration */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle>Google Calendar</CardTitle>
              <CardDescription>Sync events and assignments from Google Calendar</CardDescription>
            </div>
            {googleConnected && <Badge variant="success">Connected</Badge>}
          </div>
        </CardHeader>
        <CardContent>
          {googleConnected ? (
            <div className="space-y-4">
              <p className="text-gray-300">Connected as: user@gmail.com</p>
              <Button variant="danger" onClick={() => setGoogleConnected(false)}>
                Disconnect
              </Button>
            </div>
          ) : (
            <Button onClick={handleConnectGoogle}>Connect Google</Button>
          )}
        </CardContent>
      </Card>

      {/* Canvas Integration */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle>Canvas LMS</CardTitle>
              <CardDescription>Import assignments and due dates from Canvas</CardDescription>
            </div>
            {canvasConnected && <Badge variant="success">Connected</Badge>}
          </div>
        </CardHeader>
        <CardContent>
          {canvasConnected ? (
            <div className="space-y-4">
              <p className="text-gray-300">Connected to: canvas.university.edu</p>
              <Button variant="danger" onClick={() => setCanvasConnected(false)}>
                Disconnect
              </Button>
            </div>
          ) : (
            <Button onClick={handleConnectCanvas}>Connect Canvas</Button>
          )}
        </CardContent>
      </Card>

      {/* Whisper Model */}
      <Card>
        <CardHeader>
          <CardTitle>Whisper Transcription Model</CardTitle>
          <CardDescription>Local AI model path (read-only in SaaS)</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="p-3 bg-gray-800 border border-gray-700 rounded-lg text-gray-300 font-mono text-sm">
            {whisperPath}
          </div>
          <p className="text-gray-500 text-xs mt-2">
            This path is managed by the server administrator
          </p>
        </CardContent>
      </Card>

      {/* AI Provider Keys */}
      <Card>
        <CardHeader>
          <CardTitle>AI Provider Configuration</CardTitle>
          <CardDescription>Configure API keys for summarization and suggestions</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            <div>
              <label className="text-sm text-gray-300 mb-2 block">Provider</label>
              <select
                className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-primary"
                value={aiProvider}
                onChange={(e) => setAiProvider(e.target.value as any)}
              >
                <option value="none">None (Mock responses)</option>
                <option value="openai">OpenAI (GPT-4)</option>
                <option value="anthropic">Anthropic (Claude)</option>
              </select>
            </div>
            
            {aiProvider !== 'none' && (
              <div>
                <label className="text-sm text-gray-300 mb-2 block">API Key</label>
                <div className="flex gap-2">
                  <Input
                    type={showApiKey ? 'text' : 'password'}
                    placeholder="sk-..."
                    value={apiKey}
                    onChange={(e) => setApiKey(e.target.value)}
                    className="flex-1"
                  />
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => setShowApiKey(!showApiKey)}
                  >
                    {showApiKey ? 'Hide' : 'Show'}
                  </Button>
                </div>
                <p className="text-gray-500 text-xs mt-2">
                  {apiKey ? `Key: ${maskKey(apiKey)}` : 'Enter your API key'}
                </p>
              </div>
            )}
            
            <Button onClick={handleSaveAiKey} disabled={aiProvider === 'none' || !apiKey}>
              Save Configuration
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
