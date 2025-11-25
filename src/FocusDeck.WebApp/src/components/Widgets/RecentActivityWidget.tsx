import { Card, CardContent, CardHeader, CardTitle } from '../Card';

export function RecentActivityWidget() {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Recent Activity</CardTitle>
      </CardHeader>
      <CardContent>
        <div className="text-center text-gray-400 py-8">
          No recent activity yet. Start by creating your first lecture or focus session!
        </div>
      </CardContent>
    </Card>
  );
}
