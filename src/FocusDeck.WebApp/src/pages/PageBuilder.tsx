import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/Card';

export function PageBuilder() {
  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle>Page Builder</CardTitle>
          <CardDescription>
            This is where you will build your custom pages. Functionality coming soon!
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="text-center text-gray-500 py-12 border border-dashed border-gray-700 rounded-lg">
            <p className="text-lg">🏗️</p>
            <p>Page Builder Interface Coming Soon</p>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
