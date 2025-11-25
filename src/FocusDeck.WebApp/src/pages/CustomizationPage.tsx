import { Link } from 'react-router-dom';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/Card';
import { Button } from '../components/Button';

export function CustomizationPage() {
  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-3xl font-bold">Customization Hub</h1>
        <p className="text-gray-400 mt-2">
          Build your own pages and widgets to create a personalized workspace.
        </p>
      </div>

      {/* Custom Pages Section */}
      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <div>
            <CardTitle>Custom Pages</CardTitle>
            <CardDescription>Manage your personalized pages.</CardDescription>
          </div>
          <Link to="/customize/pages/new">
            <Button>Create New Page</Button>
          </Link>
        </CardHeader>
        <CardContent>
          <div className="text-center text-gray-500 py-12 border border-dashed border-gray-700 rounded-lg">
            <p>You haven't created any custom pages yet.</p>
            <p className="text-sm mt-1">Click "Create New Page" to get started.</p>
          </div>
        </CardContent>
      </Card>

      {/* Custom Widgets Section */}
      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <div>
            <CardTitle>Custom Widgets</CardTitle>
            <CardDescription>Manage your reusable widgets and data sources.</CardDescription>
          </div>
          <Link to="/customize/widgets/new">
            <Button>Create New Widget</Button>
          </Link>
        </CardHeader>
        <CardContent>
          <div className="text-center text-gray-500 py-12 border border-dashed border-gray-700 rounded-lg">
            <p>You haven't created any custom widgets yet.</p>
            <p className="text-sm mt-1">Click "Create New Widget" to get started.</p>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
