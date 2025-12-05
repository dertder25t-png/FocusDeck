import { Card, CardContent, CardHeader, CardTitle } from '../Card';

interface ActivityItem {
  id: string;
  type: string;
  title: string;
  timestamp: string;
  details: string;
}

interface RecentActivityWidgetProps {
  activity?: ActivityItem[];
}

export function RecentActivityWidget({ activity = [] }: RecentActivityWidgetProps) {
  return (
    <Card className="col-span-1 md:col-span-2 lg:col-span-3">
      <CardHeader>
        <CardTitle>Recent Activity</CardTitle>
      </CardHeader>
      <CardContent>
        {activity.length === 0 ? (
           <div className="text-center text-gray-400 py-8">
             No recent activity yet. Start by creating your first lecture or focus session!
           </div>
        ) : (
           <div className="space-y-4">
             {activity.map(item => (
               <div key={item.id} className="flex items-start gap-4 p-3 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors">
                  <div className={`mt-1 w-8 h-8 rounded-full flex items-center justify-center ${
                      item.type === 'lecture' ? 'bg-purple-100 text-purple-600' :
                      item.type === 'note' ? 'bg-yellow-100 text-yellow-600' :
                      'bg-blue-100 text-blue-600'
                  }`}>
                      <i className={`fa-solid ${
                          item.type === 'lecture' ? 'fa-microphone' :
                          item.type === 'note' ? 'fa-note-sticky' :
                          'fa-layer-group'
                      }`}></i>
                  </div>
                  <div className="flex-1">
                      <div className="font-medium">{item.title}</div>
                      <div className="text-sm text-gray-500">{item.details}</div>
                  </div>
                  <div className="text-xs text-gray-400 whitespace-nowrap">
                      {new Date(item.timestamp).toLocaleDateString()}
                  </div>
               </div>
             ))}
           </div>
        )}
      </CardContent>
    </Card>
  );
}
