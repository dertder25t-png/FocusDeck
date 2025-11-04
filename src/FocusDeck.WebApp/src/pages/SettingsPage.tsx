import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '../components/Card'
import { Input } from '../components/Input'
import { Button } from '../components/Button'
import { Link } from 'react-router-dom'

export function SettingsPage() {
  return (
    <div className="space-y-6 max-w-4xl">
      <div>
        <h1 className="text-2xl font-semibold">Settings</h1>
        <p className="text-sm text-gray-400 mt-1">
          Manage your profile, organization, and preferences
        </p>
      </div>

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
          <CardTitle>Organization</CardTitle>
          <CardDescription>Manage your organization settings</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <div className="font-medium">My Organizations</div>
              <div className="text-sm text-gray-400">View and manage your organizations</div>
            </div>
            <Link to="/app/organizations">
              <Button variant="secondary" size="sm">
                Manage
              </Button>
            </Link>
          </div>
          <div className="flex items-center justify-between">
            <div>
              <div className="font-medium">Team Members</div>
              <div className="text-sm text-gray-400">Invite and manage team members</div>
            </div>
            <Button variant="secondary" size="sm" disabled>
              View Members
            </Button>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Operations</CardTitle>
          <CardDescription>Monitor system operations and jobs</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <div className="font-medium">Background Jobs</div>
              <div className="text-sm text-gray-400">Monitor and manage background job processing</div>
            </div>
            <Link to="/app/jobs">
              <Button variant="secondary" size="sm">
                View Jobs
              </Button>
            </Link>
          </div>
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
  )
}
