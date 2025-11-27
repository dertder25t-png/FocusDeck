import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/Card';

export function WidgetBuilder() {
  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle>Widget Builder</CardTitle>
          <CardDescription>
            This is where you will build your custom widgets. Functionality coming soon!
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="text-center text-gray-500 py-12 border border-dashed border-gray-700 rounded-lg">
            <p className="text-lg">🏗️</p>
            <p>Widget Builder Interface Coming Soon</p>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
