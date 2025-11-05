import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '../components/Card';
import { Button } from '../components/Button';
import { Badge } from '../components/Badge';

export default function BillingPage() {
  return (
    <div>
      <h1 className="text-2xl font-semibold mb-6">Billing & Plans</h1>

      <div className="space-y-6">
        {/* Current Plan */}
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <div>
                <CardTitle>Current Plan</CardTitle>
                <CardDescription>You are currently on the Free plan</CardDescription>
              </div>
              <Badge variant="info">Free</Badge>
            </div>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              <div>
                <div className="text-sm text-gray-400 mb-2">Features included:</div>
                <ul className="space-y-1 text-sm">
                  <li className="flex items-center gap-2">
                    <span className="text-green-500">✓</span>
                    <span>Unlimited lectures</span>
                  </li>
                  <li className="flex items-center gap-2">
                    <span className="text-green-500">✓</span>
                    <span>Focus sessions</span>
                  </li>
                  <li className="flex items-center gap-2">
                    <span className="text-green-500">✓</span>
                    <span>Basic AI suggestions</span>
                  </li>
                  <li className="flex items-center gap-2">
                    <span className="text-gray-500">✗</span>
                    <span className="text-gray-500">Advanced analytics</span>
                  </li>
                  <li className="flex items-center gap-2">
                    <span className="text-gray-500">✗</span>
                    <span className="text-gray-500">Team collaboration</span>
                  </li>
                </ul>
              </div>
              <div>
                <Button>Upgrade Plan</Button>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Usage */}
        <Card>
          <CardHeader>
            <CardTitle>Usage This Month</CardTitle>
            <CardDescription>Current usage against plan limits</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              <div>
                <div className="flex items-center justify-between mb-2">
                  <span className="text-sm">Lecture Processing</span>
                  <span className="text-sm text-gray-400">8 / 10</span>
                </div>
                <div className="w-full bg-gray-700 rounded-full h-2">
                  <div className="bg-primary h-2 rounded-full" style={{ width: '80%' }}></div>
                </div>
              </div>
              <div>
                <div className="flex items-center justify-between mb-2">
                  <span className="text-sm">AI Verifications</span>
                  <span className="text-sm text-gray-400">23 / 50</span>
                </div>
                <div className="w-full bg-gray-700 rounded-full h-2">
                  <div className="bg-primary h-2 rounded-full" style={{ width: '46%' }}></div>
                </div>
              </div>
              <div>
                <div className="flex items-center justify-between mb-2">
                  <span className="text-sm">Design Ideas</span>
                  <span className="text-sm text-gray-400">145 / 200</span>
                </div>
                <div className="w-full bg-gray-700 rounded-full h-2">
                  <div className="bg-primary h-2 rounded-full" style={{ width: '72.5%' }}></div>
                </div>
              </div>
              <div>
                <div className="flex items-center justify-between mb-2">
                  <span className="text-sm">Storage</span>
                  <span className="text-sm text-gray-400">1.2 GB / 5 GB</span>
                </div>
                <div className="w-full bg-gray-700 rounded-full h-2">
                  <div className="bg-primary h-2 rounded-full" style={{ width: '24%' }}></div>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Billing Management */}
        <Card>
          <CardHeader>
            <CardTitle>Billing Management</CardTitle>
            <CardDescription>Manage payment methods and invoices</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              <Button variant="secondary" className="w-full justify-center">
                Manage Billing (Stripe Portal)
              </Button>
              <p className="text-sm text-gray-400 text-center">
                You'll be redirected to our secure payment partner
              </p>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
