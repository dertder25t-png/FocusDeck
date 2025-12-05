import { WelcomeWidget } from '../components/Widgets/WelcomeWidget';
import { StatCardWidget } from '../components/Widgets/StatCardWidget';
import { RecentActivityWidget } from '../components/Widgets/RecentActivityWidget';
import { QuickActionsWidget } from '../components/Widgets/QuickActionsWidget';
import { useDashboard } from '../hooks/useDashboard';

export function DashboardPage() {
  const { data: dashboard, isLoading } = useDashboard();
  const stats = dashboard?.stats || { lectures: 0, focusTime: 0, notes: 0, projects: 0 };

  return (
    <div className="space-y-6">
      <WelcomeWidget />
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <StatCardWidget label="Total Lectures" value={isLoading ? '-' : stats.lectures.toString()} />
        <StatCardWidget label="Focus Time" value={isLoading ? '-' : `${stats.focusTime}h`} />
        <StatCardWidget label="Notes Verified" value={isLoading ? '-' : stats.notes.toString()} />
        <StatCardWidget label="Projects" value={isLoading ? '-' : stats.projects.toString()} />
      </div>
      <RecentActivityWidget />
      <QuickActionsWidget />
    </div>
  );
}
