import { useState, useEffect } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/Card';
import { Badge } from '../components/Badge';
import { Button } from '../components/Button';

interface SystemInfo {
  version: string;
  gitSha: string;
  uptime: {
    days: number;
    hours: number;
    minutes: number;
    seconds: number;
    totalSeconds: number;
  };
  queue: {
    enqueued: number;
    scheduled: number;
    processing: number;
    failed: number;
  };
  environment: string;
  serverTime: string;
}

export function SystemOpsPage() {
  const [systemInfo, setSystemInfo] = useState<SystemInfo | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadSystemInfo();
    const interval = setInterval(loadSystemInfo, 30000); // Refresh every 30s
    return () => clearInterval(interval);
  }, []);

  const loadSystemInfo = async () => {
    try {
      const res = await fetch('/v1/system/info');
      if (res.ok) {
        const data = await res.json();
        setSystemInfo(data);
      }
    } catch (err) {
      console.error('Failed to load system info:', err);
    } finally {
      setLoading(false);
    }
  };

  const formatUptime = (uptime: SystemInfo['uptime']) => {
    const parts = [];
    if (uptime.days > 0) parts.push(`${uptime.days}d`);
    if (uptime.hours > 0) parts.push(`${uptime.hours}h`);
    if (uptime.minutes > 0) parts.push(`${uptime.minutes}m`);
    if (parts.length === 0) parts.push(`${uptime.seconds}s`);
    return parts.join(' ');
  };

  if (loading || !systemInfo) {
    return (
      <div className="flex items-center justify-center h-96">
        <p className="text-gray-400">Loading system information...</p>
      </div>
    );
  }

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-3xl font-semibold text-white">System Operations</h1>
        <p className="text-gray-400 mt-2">Monitor server health and background jobs</p>
      </div>

      {/* System Info Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <Card>
          <CardHeader>
            <CardDescription>Version</CardDescription>
            <CardTitle className="text-2xl">{systemInfo.version}</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-gray-400 text-sm">Build: {systemInfo.gitSha}</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardDescription>Environment</CardDescription>
            <CardTitle className="text-2xl">
              <Badge variant={systemInfo.environment === 'Production' ? 'success' : 'warning'}>
                {systemInfo.environment}
              </Badge>
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-gray-400 text-sm">
              {new Date(systemInfo.serverTime).toLocaleString()}
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardDescription>Uptime</CardDescription>
            <CardTitle className="text-2xl">{formatUptime(systemInfo.uptime)}</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-gray-400 text-sm">
              {systemInfo.uptime.totalSeconds.toLocaleString()} seconds
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardDescription>Queue Depth</CardDescription>
            <CardTitle className="text-2xl">{systemInfo.queue.enqueued}</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-gray-400 text-sm">
              {systemInfo.queue.processing} processing
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Job Queue Status */}
      <Card>
        <CardHeader>
          <CardTitle>Background Job Queue</CardTitle>
          <CardDescription>Hangfire job status and metrics</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div>
              <p className="text-gray-400 text-sm">Enqueued</p>
              <p className="text-white text-2xl font-semibold mt-1">
                {systemInfo.queue.enqueued}
              </p>
            </div>
            <div>
              <p className="text-gray-400 text-sm">Scheduled</p>
              <p className="text-white text-2xl font-semibold mt-1">
                {systemInfo.queue.scheduled}
              </p>
            </div>
            <div>
              <p className="text-gray-400 text-sm">Processing</p>
              <p className="text-primary text-2xl font-semibold mt-1">
                {systemInfo.queue.processing}
              </p>
            </div>
            <div>
              <p className="text-gray-400 text-sm">Failed</p>
              <p className="text-red-400 text-2xl font-semibold mt-1">
                {systemInfo.queue.failed}
              </p>
            </div>
          </div>
          <div className="mt-6">
            <Button onClick={() => window.open('/hangfire', '_blank')}>
              Open Hangfire Dashboard
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Quick Actions */}
      <Card>
        <CardHeader>
          <CardTitle>Quick Actions</CardTitle>
          <CardDescription>Common administrative tasks</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex flex-wrap gap-3">
            <Button variant="ghost" onClick={() => window.location.href = '/app/jobs'}>
              View Job History
            </Button>
            <Button variant="ghost" onClick={loadSystemInfo}>
              Refresh Metrics
            </Button>
            <Button variant="ghost" onClick={() => window.open('/health', '_blank')}>
              Health Check
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Resource Links */}
      <Card>
        <CardHeader>
          <CardTitle>Resources</CardTitle>
          <CardDescription>External monitoring and tools</CardDescription>
        </CardHeader>
        <CardContent>
          <ul className="space-y-2">
            <li>
              <a href="/swagger" target="_blank" className="text-primary hover:underline">
                API Documentation (Swagger)
              </a>
            </li>
            <li>
              <a href="/hangfire" target="_blank" className="text-primary hover:underline">
                Background Jobs (Hangfire Dashboard)
              </a>
            </li>
            <li>
              <a href="/health" target="_blank" className="text-primary hover:underline">
                Health Checks Endpoint
              </a>
            </li>
            <li>
              <a
                href="https://github.com/dertder25t-png/FocusDeck"
                target="_blank"
                rel="noopener noreferrer"
                className="text-primary hover:underline"
              >
                GitHub Repository
              </a>
            </li>
          </ul>
        </CardContent>
      </Card>
    </div>
  );
}
