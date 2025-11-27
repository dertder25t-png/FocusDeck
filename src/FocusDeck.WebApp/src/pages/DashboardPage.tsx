import { WelcomeWidget } from '../components/Widgets/WelcomeWidget';
import { StatCardWidget } from '../components/Widgets/StatCardWidget';
import { RecentActivityWidget } from '../components/Widgets/RecentActivityWidget';
import { QuickActionsWidget } from '../components/Widgets/QuickActionsWidget';

export function DashboardPage() {
  return (
    <div className="space-y-6">
      <WelcomeWidget />
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <StatCardWidget label="Total Lectures" value="0" />
        <StatCardWidget label="Focus Time" value="0h" />
        <StatCardWidget label="Notes Verified" value="0" />
        <StatCardWidget label="Design Projects" value="0" />
      </div>
      <RecentActivityWidget />
      <QuickActionsWidget />
    </div>
  );
}
