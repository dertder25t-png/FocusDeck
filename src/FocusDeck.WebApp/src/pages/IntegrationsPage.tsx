import { useState, useEffect } from 'react';
import { Button } from '../components/Button';
import { Input } from '../components/Input';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/Card';
import { Badge } from '../components/Badge';
import { apiFetch } from '../services/api';

export function IntegrationsPage() {
  const [googleConnected, setGoogleConnected] = useState(false);

  const [canvasConnected, setCanvasConnected] = useState(false);
  const [canvasDomain, setCanvasDomain] = useState('');
  const [canvasKey, setCanvasKey] = useState('');
  const [showCanvasForm, setShowCanvasForm] = useState(false);

  const [spotifyConnected, setSpotifyConnected] = useState(false);
  const [spotifyKey, setSpotifyKey] = useState('');
  const [showSpotifyForm, setShowSpotifyForm] = useState(false);

  const [whisperPath] = useState('/models/ggml-base.en.bin');
  const [aiProvider, setAiProvider] = useState<'openai' | 'anthropic' | 'none'>('none');
  const [apiKey, setApiKey] = useState('');
  const [showApiKey, setShowApiKey] = useState(false);

  useEffect(() => {
    // Fetch initial status
    apiFetch('/v1/integrations').then(async (res) => {
        if (res.ok) {
            const data = await res.json();
            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            const canvas = data.find((s: any) => s.serviceType === 'Canvas');
            if (canvas && canvas.isConfigured) {
                setCanvasConnected(true);
                try {
                    const meta = JSON.parse(canvas.metadataJson);
                    setCanvasDomain(meta.domain || '');
                } catch { /* empty */ }
            }

            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            const spotify = data.find((s: any) => s.serviceType === 'Spotify');
            if (spotify && spotify.isConfigured) {
                setSpotifyConnected(true);
            }
        }
    });
  }, []);

  const handleConnectGoogle = () => {
    // OAuth flow would happen here
    window.location.href = '/v1/auth/google-oauth-start';
  };

  const handleSaveCanvas = async () => {
    if (!canvasDomain || !canvasKey) {
        alert("Please enter both domain and API key.");
        return;
    }

    try {
        const res = await apiFetch('/v1/integrations', {
            method: 'POST',
            body: JSON.stringify({
                serviceType: 'Canvas',
                accessToken: canvasKey,
                metadataJson: JSON.stringify({ domain: canvasDomain })
            })
        });

        if (res.ok) {
            setCanvasConnected(true);
            setShowCanvasForm(false);
            setCanvasKey('');
            alert("Canvas connected successfully!");
        } else {
            alert("Failed to connect Canvas. Check server logs.");
        }
    } catch (e) {
        console.error(e);
        alert("Error connecting Canvas.");
    }
  };

  const handleSaveSpotify = async () => {
      if (!spotifyKey) {
          alert("Please enter your Spotify Access Token.");
          return;
      }

      try {
          const res = await apiFetch('/v1/integrations', {
              method: 'POST',
              body: JSON.stringify({
                  serviceType: 'Spotify',
                  accessToken: spotifyKey
              })
          });

          if (res.ok) {
              setSpotifyConnected(true);
              setShowSpotifyForm(false);
              setSpotifyKey('');
              alert("Spotify connected successfully!");
          } else {
              alert("Failed to connect Spotify.");
          }
      } catch (e) {
          console.error(e);
          alert("Error connecting Spotify.");
      }
  };

  const handleDisconnectCanvas = async () => {
      try {
        const res = await apiFetch('/v1/integrations');
        if (res.ok) {
            const data = await res.json();
            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            const canvas = data.find((s: any) => s.serviceType === 'Canvas');
            if (canvas) {
                await apiFetch(`/v1/integrations/${canvas.id}`, { method: 'DELETE' });
                setCanvasConnected(false);
                setCanvasDomain('');
            }
        }
      } catch (e) {
          console.error(e);
      }
  };

  const handleDisconnectSpotify = async () => {
      try {
        const res = await apiFetch('/v1/integrations');
        if (res.ok) {
            const data = await res.json();
            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            const spotify = data.find((s: any) => s.serviceType === 'Spotify');
            if (spotify) {
                await apiFetch(`/v1/integrations/${spotify.id}`, { method: 'DELETE' });
                setSpotifyConnected(false);
            }
        }
      } catch (e) {
          console.error(e);
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
              <p className="text-gray-300">Connected to: {canvasDomain}</p>
              <Button variant="danger" onClick={handleDisconnectCanvas}>
                Disconnect
              </Button>
            </div>
          ) : (
            <div className="space-y-4">
                {!showCanvasForm ? (
                    <Button onClick={() => setShowCanvasForm(true)}>Connect Canvas</Button>
                ) : (
                    <div className="bg-gray-800/50 p-4 rounded-lg border border-gray-700 space-y-4">
                        <div>
                            <label className="block text-sm font-medium text-gray-300 mb-1">Canvas Domain</label>
                            <Input
                                placeholder="canvas.instructure.com"
                                value={canvasDomain}
                                onChange={(e) => setCanvasDomain(e.target.value)}
                            />
                            <p className="text-xs text-gray-500 mt-1">The URL you use to access Canvas.</p>
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-gray-300 mb-1">API Key</label>
                            <Input
                                type="password"
                                placeholder="Canvas Access Token"
                                value={canvasKey}
                                onChange={(e) => setCanvasKey(e.target.value)}
                            />
                            <div className="text-xs text-gray-400 mt-2 space-y-1">
                                <p>To get your API key:</p>
                                <ol className="list-decimal pl-4 space-y-1">
                                    <li>Log in to Canvas and go to <strong>Account &gt; Settings</strong>.</li>
                                    <li>Scroll down to <strong>Approved Integrations</strong>.</li>
                                    <li>Click <strong>+ New Access Token</strong>.</li>
                                    <li>Enter a purpose (e.g., "FocusDeck") and click <strong>Generate Token</strong>.</li>
                                    <li>Copy the token immediately and paste it above.</li>
                                </ol>
                            </div>
                        </div>
                        <div className="flex gap-2 pt-2">
                            <Button onClick={handleSaveCanvas}>Save & Connect</Button>
                            <Button variant="ghost" onClick={() => setShowCanvasForm(false)}>Cancel</Button>
                        </div>
                    </div>
                )}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Spotify Integration */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle>Spotify</CardTitle>
              <CardDescription>Control music playback</CardDescription>
            </div>
            {spotifyConnected && <Badge variant="success">Connected</Badge>}
          </div>
        </CardHeader>
        <CardContent>
            {spotifyConnected ? (
                <div className="space-y-4">
                    <p className="text-gray-300">Spotify connected</p>
                    <Button variant="danger" onClick={handleDisconnectSpotify}>
                        Disconnect
                    </Button>
                </div>
            ) : (
                <div className="space-y-4">
                    {!showSpotifyForm ? (
                        <Button onClick={() => setShowSpotifyForm(true)}>Connect Spotify</Button>
                    ) : (
                        <div className="bg-gray-800/50 p-4 rounded-lg border border-gray-700 space-y-4">
                            <div>
                                <label className="block text-sm font-medium text-gray-300 mb-1">Spotify Access Token</label>
                                <Input
                                    type="password"
                                    placeholder="Spotify Access Token"
                                    value={spotifyKey}
                                    onChange={(e) => setSpotifyKey(e.target.value)}
                                />
                                <p className="text-xs text-gray-500 mt-1">
                                    Enter a valid Spotify Access Token (Note: Tokens expire after 1 hour).
                                </p>
                            </div>
                            <div className="flex gap-2 pt-2">
                                <Button onClick={handleSaveSpotify}>Save & Connect</Button>
                                <Button variant="ghost" onClick={() => setShowSpotifyForm(false)}>Cancel</Button>
                            </div>
                        </div>
                    )}
                </div>
            )}
        </CardContent>
      </Card>

      {/* Home Assistant Integration */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle>Home Assistant</CardTitle>
              <CardDescription>Control smart home devices</CardDescription>
            </div>
            <Button variant="outline" onClick={() => alert('Home Assistant config dialog not yet implemented')}>Configure</Button>
          </div>
        </CardHeader>
      </Card>

      {/* Philips Hue Integration */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle>Philips Hue</CardTitle>
              <CardDescription>Control smart lights</CardDescription>
            </div>
            <Button variant="outline" onClick={() => alert('Hue config dialog not yet implemented')}>Configure</Button>
          </div>
        </CardHeader>
      </Card>

      {/* Slack Integration */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle>Slack</CardTitle>
              <CardDescription>Send messages and update status</CardDescription>
            </div>
            <Button variant="outline" onClick={() => alert('Slack OAuth not yet implemented')}>Connect Slack</Button>
          </div>
        </CardHeader>
      </Card>

      {/* Discord Integration */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle>Discord</CardTitle>
              <CardDescription>Send messages via Webhook</CardDescription>
            </div>
            <Button variant="outline" onClick={() => alert('Discord Webhook config dialog not yet implemented')}>Configure</Button>
          </div>
        </CardHeader>
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
                // eslint-disable-next-line @typescript-eslint/no-explicit-any
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
