import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '../components/Card'
import { Input } from '../components/Input'
import { Button } from '../components/Button'
import { Badge } from '../components/Badge'
import { Link } from 'react-router-dom'
import { useState, useEffect } from 'react'
import { usePrivacySettings } from '../contexts/privacySettings'
import type { PrivacySetting } from '../types/privacy'

export function SettingsPage() {
  const [systemInfo, setSystemInfo] = useState<any>(null)
  const [activeTab, setActiveTab] = useState<'profile' | 'tenant' | 'integrations' | 'privacy' | 'system'>('profile')
  const { settings: privacySettings, loading: privacyLoading, updateSetting } = usePrivacySettings()
  const [pendingPrivacy, setPendingPrivacy] = useState<string[]>([])

  const toggleSetting = async (setting: PrivacySetting) => {
    if (pendingPrivacy.includes(setting.contextType)) {
      return
    }

    setPendingPrivacy((prev) => [...prev, setting.contextType])
    try {
      await updateSetting(setting.contextType, !setting.isEnabled)
    } catch (error) {
      console.error('Unable to update privacy setting', setting.contextType, error)
    } finally {
      setPendingPrivacy((prev) => prev.filter((ctx) => ctx !== setting.contextType))
    }
  }

  useEffect(() => {
    // Fetch system info for system tab
    fetch('/v1/system/info')
      .then(res => res.json())
      .then(data => setSystemInfo(data))
      .catch(err => console.error('Failed to fetch system info:', err))
  }, [])

  const tabs = [
    { id: 'profile' as const, label: 'Profile' },
    { id: 'tenant' as const, label: 'Tenant' },
    { id: 'integrations' as const, label: 'Integrations' },
    { id: 'privacy' as const, label: 'Privacy & Consent' },
    { id: 'system' as const, label: 'System' }
  ]

  return (
    <div className="space-y-6 max-w-6xl">
      <div>
        <h1 className="text-2xl font-semibold">Settings</h1>
        <p className="text-sm text-gray-400 mt-1">
          Manage your profile, organization, integrations, and system
        </p>
      </div>

      {/* Tab Navigation */}
      <div className="border-b border-gray-800">
        <div className="flex space-x-8">
          {tabs.map(tab => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={`pb-4 px-1 border-b-2 font-medium text-sm transition-colors ${
                activeTab === tab.id
                  ? 'border-primary text-primary'
                  : 'border-transparent text-gray-400 hover:text-gray-300'
              }`}
            >
              {tab.label}
            </button>
          ))}
        </div>
      </div>

      {/* Profile Tab */}
      {activeTab === 'profile' && (
        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Profile</CardTitle>
              <CardDescription>Update your personal information</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <label className="text-sm font-medium mb-2 block">Name</label>
                <Input placeholder="Your name" />
              </div>
              <div>
                <label className="text-sm font-medium mb-2 block">Email</label>
                <Input type="email" placeholder="your@email.com" />
              </div>
              <Button>Save Changes</Button>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Preferences</CardTitle>
              <CardDescription>Customize your experience</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center justify-between">
                <div>
                  <div className="font-medium">Theme</div>
                  <div className="text-sm text-gray-400">Dark mode (default)</div>
                </div>
                <Button variant="secondary" size="sm">
                  Change
                </Button>
              </div>
              <div className="flex items-center justify-between">
                <div>
                  <div className="font-medium">Notifications</div>
                  <div className="text-sm text-gray-400">Receive real-time updates</div>
                </div>
                <Button variant="secondary" size="sm">
                  Configure
                </Button>
              </div>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Tenant Tab */}
      {activeTab === 'tenant' && (
        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Tenants</CardTitle>
              <CardDescription>View and manage your tenants</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <Link to="/tenants">
                <Button>Manage Tenants</Button>
              </Link>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Team Members</CardTitle>
              <CardDescription>Manage members, roles, and invitations</CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              {/* Members Table Placeholder */}
              <div className="border border-gray-800 rounded-lg overflow-hidden">
                <table className="w-full">
                  <thead className="bg-gray-900">
                    <tr>
                      <th className="px-4 py-3 text-left text-xs font-medium text-gray-400 uppercase">User</th>
                      <th className="px-4 py-3 text-left text-xs font-medium text-gray-400 uppercase">Role</th>
                      <th className="px-4 py-3 text-left text-xs font-medium text-gray-400 uppercase">Joined</th>
                      <th className="px-4 py-3 text-right text-xs font-medium text-gray-400 uppercase">Actions</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-800">
                    <tr>
                      <td className="px-4 py-4">
                        <div>
                          <div className="font-medium">You</div>
                          <div className="text-sm text-gray-400">user@example.com</div>
                        </div>
                      </td>
                      <td className="px-4 py-4">
                        <Badge variant="success">Owner</Badge>
                      </td>
                      <td className="px-4 py-4 text-sm text-gray-400">Jan 15, 2024</td>
                      <td className="px-4 py-4 text-right">
                        <Button variant="ghost" size="sm" disabled>Remove</Button>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>

              {/* Pending Invites */}
              <div>
                <h3 className="font-medium mb-3">Pending Invites</h3>
                <div className="border border-gray-800 rounded-lg p-4 text-center text-sm text-gray-400">
                  No pending invitations
                </div>
              </div>

              <div className="flex justify-between">
                <Button>Send Invitation</Button>
                <Button variant="danger">Transfer Ownership</Button>
              </div>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Integrations Tab */}
      {activeTab === 'integrations' && (
        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Authentication</CardTitle>
              <CardDescription>Connected OAuth providers</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center justify-between">
                <div className="flex items-center space-x-3">
                  <div className="w-10 h-10 bg-white rounded-lg flex items-center justify-center text-2xl">
                    G
                  </div>
                  <div>
                    <div className="font-medium">Google</div>
                    <div className="text-sm text-gray-400">Connected</div>
                  </div>
                </div>
                <Badge variant="success">Active</Badge>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Learning Management Systems</CardTitle>
              <CardDescription>Connect to Canvas, Google Classroom, etc.</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center justify-between">
                <div>
                  <div className="font-medium">Canvas LMS</div>
                  <div className="text-sm text-gray-400">Auto-import assignments and deadlines</div>
                </div>
                <Button variant="secondary" size="sm">Connect</Button>
              </div>
              <div className="flex items-center justify-between">
                <div>
                  <div className="font-medium">Google Classroom</div>
                  <div className="text-sm text-gray-400">Sync courses and assignments</div>
                </div>
                <Button variant="secondary" size="sm">Connect</Button>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>AI Provider Settings</CardTitle>
              <CardDescription>Configure AI model endpoints (SaaS managed)</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <label className="text-sm font-medium mb-2 block">Whisper Model Path</label>
                <Input 
                  value="/models/whisper-large-v3" 
                  disabled 
                  className="bg-gray-900 text-gray-500"
                />
                <p className="text-xs text-gray-500 mt-1">Managed by platform (read-only)</p>
              </div>
              <div>
                <label className="text-sm font-medium mb-2 block">LLM API Key</label>
                <Input 
                  type="password" 
                  value="sk-•••••••••••••••••••" 
                  disabled
                  className="bg-gray-900 text-gray-500"
                />
                <p className="text-xs text-gray-500 mt-1">Managed by platform (masked)</p>
              </div>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Privacy Tab */}
      {activeTab === 'privacy' && (
        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Privacy & Consent</CardTitle>
              <CardDescription>Toggle which contextual sensors are allowed to capture data.</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              {privacyLoading ? (
                <div className="text-sm text-gray-400">Loading privacy controls…</div>
              ) : (
                <>
                  {privacySettings.map((setting) => (
                    <div
                      key={setting.contextType}
                      className="rounded-lg border border-gray-800 bg-gray-900/40 p-4 shadow-sm"
                    >
                      <div className="flex items-start justify-between gap-4">
                        <div>
                          <p className="font-medium text-white">{setting.displayName}</p>
                          <p className="text-xs text-gray-400">{setting.description}</p>
                          <div className="text-xs text-gray-500 mt-1">
                            Tier: <span className="text-primary">{setting.tier}</span> · Default:{' '}
                            {setting.defaultEnabled ? 'On' : 'Off'}
                          </div>
                        </div>

                        <Button
                          size="sm"
                          variant={setting.isEnabled ? 'secondary' : 'primary'}
                          onClick={() => toggleSetting(setting)}
                          disabled={pendingPrivacy.includes(setting.contextType)}
                          aria-pressed={setting.isEnabled}
                        >
                          {pendingPrivacy.includes(setting.contextType)
                            ? 'Updating…'
                            : setting.isEnabled
                            ? 'Enabled'
                            : 'Enable'}
                        </Button>
                      </div>
                    </div>
                  ))}
                  {!privacyLoading && privacySettings.length === 0 && (
                    <p className="text-sm text-gray-400">No privacy controls are available yet.</p>
                  )}
                </>
              )}
            </CardContent>
          </Card>
        </div>
      )}

      {/* System Tab */}
      {activeTab === 'system' && (
        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>System Information</CardTitle>
              <CardDescription>Server version, uptime, and diagnostics</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              {systemInfo ? (
                <>
                  <div className="grid grid-cols-2 gap-4">
                    <div>
                      <div className="text-sm text-gray-400">Version</div>
                      <div className="font-mono font-medium">{systemInfo.version}</div>
                    </div>
                    <div>
                      <div className="text-sm text-gray-400">Git SHA</div>
                      <div className="font-mono font-medium">{systemInfo.gitSha}</div>
                    </div>
                    <div>
                      <div className="text-sm text-gray-400">Environment</div>
                      <div className="font-medium">{systemInfo.environment}</div>
                    </div>
                    <div>
                      <div className="text-sm text-gray-400">Uptime</div>
                      <div className="font-medium">
                        {systemInfo.uptime.days}d {systemInfo.uptime.hours}h {systemInfo.uptime.minutes}m
                      </div>
                    </div>
                  </div>
                  
                  <div className="pt-4 border-t border-gray-800">
                    <h3 className="font-medium mb-3">Job Queue Status</h3>
                    <div className="grid grid-cols-4 gap-4">
                      <div>
                        <div className="text-sm text-gray-400">Enqueued</div>
                        <div className="text-2xl font-semibold">{systemInfo.queue.enqueued}</div>
                      </div>
                      <div>
                        <div className="text-sm text-gray-400">Processing</div>
                        <div className="text-2xl font-semibold text-warning">{systemInfo.queue.processing}</div>
                      </div>
                      <div>
                        <div className="text-sm text-gray-400">Scheduled</div>
                        <div className="text-2xl font-semibold text-info">{systemInfo.queue.scheduled}</div>
                      </div>
                      <div>
                        <div className="text-sm text-gray-400">Failed</div>
                        <div className="text-2xl font-semibold text-danger">{systemInfo.queue.failed}</div>
                      </div>
                    </div>
                  </div>
                </>
              ) : (
                <div className="text-center py-8 text-gray-400">Loading system information...</div>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Operations</CardTitle>
              <CardDescription>Monitoring and job management</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center justify-between">
                <div>
                  <div className="font-medium">Background Jobs</div>
                  <div className="text-sm text-gray-400">Monitor Hangfire job processing</div>
                </div>
                <Link to="/app/jobs">
                  <Button variant="secondary" size="sm">
                    View Dashboard
                  </Button>
                </Link>
              </div>
              <div className="flex items-center justify-between">
                <div>
                  <div className="font-medium">Hangfire Console</div>
                  <div className="text-sm text-gray-400">Full admin interface for job management</div>
                </div>
                <a href="/hangfire" target="_blank" rel="noopener noreferrer">
                  <Button variant="secondary" size="sm">
                    Open Console
                  </Button>
                </a>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Logs & Monitoring</CardTitle>
              <CardDescription>Access to application logs and telemetry</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center justify-between">
                <div>
                  <div className="font-medium">Application Logs</div>
                  <div className="text-sm text-gray-400">Serilog with correlation IDs</div>
                </div>
                <Badge>Enabled</Badge>
              </div>
              <div className="flex items-center justify-between">
                <div>
                  <div className="font-medium">OpenTelemetry</div>
                  <div className="text-sm text-gray-400">Distributed tracing and metrics</div>
                </div>
                <Badge>Active</Badge>
              </div>
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  )
}
