import { useQuery } from '@tanstack/react-query';
import { dashboard } from '../services/api';

export interface DashboardTask {
  id: string;
  title: string;
  isCompleted: boolean;
}

export interface DashboardEvent {
  title: string;
  startTime: string; // ISO string
  color?: string;
}

export interface DashboardActivity {
  id: string;
  type: string;
  title: string;
  timestamp: string;
  details: string;
}

export interface DashboardHabit {
  id: string;
  title: string;
  icon: string;
  isCompleted: boolean;
}

export interface DashboardCourseProgress {
  code: string;
  progressPercent: number;
}

export interface DashboardRecentFile {
  title: string;
  type: string;
  timestamp: string;
}

export interface DashboardSummary {
  stats: {
    lectures: number;
    focusTime: number;
    notes: number;
    projects: number;
  };
  tasks: DashboardTask[];
  events: DashboardEvent[];
  activity: DashboardActivity[];
  habits: DashboardHabit[];
  courseProgress: DashboardCourseProgress[];
  recentFiles: DashboardRecentFile[];
}

export function useDashboard() {
  return useQuery<DashboardSummary>({
    queryKey: ['dashboard-summary'],
    queryFn: () => dashboard.getSummary(),
    // Refresh every 5 minutes or on window focus
    staleTime: 5 * 60 * 1000,
  });
}
