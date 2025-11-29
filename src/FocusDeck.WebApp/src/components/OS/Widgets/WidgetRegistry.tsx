import React from 'react';
import { 
  FocusTimerWidget, 
  TaskListWidget, 
  CalendarWidget, 
  WeatherWidget, 
  SpotifyWidget, 
  HabitTrackerWidget, 
  QuickNoteWidget, 
  CourseProgressWidget, 
  QuoteWidget, 
  RecentFilesWidget 
} from './Widgets';

export type WidgetType = 
  | 'focus-timer'
  | 'tasks'
  | 'calendar'
  | 'weather'
  | 'spotify'
  | 'habits'
  | 'quick-note'
  | 'course-progress'
  | 'quote'
  | 'recent-files';

export interface WidgetDefinition {
  id: string;
  type: WidgetType;
  title: string;
  w: number; // Grid width (1-4)
  h: number; // Grid height (1-4)
  data?: any; // Static or connected data
}

export const WIDGET_REGISTRY: Record<WidgetType, { title: string, defaultW: number, defaultH: number, component: React.FC<any> }> = {
  'focus-timer': { title: 'Focus Timer', defaultW: 2, defaultH: 1, component: FocusTimerWidget },
  'tasks': { title: 'Tasks', defaultW: 1, defaultH: 2, component: TaskListWidget },
  'calendar': { title: 'Calendar', defaultW: 1, defaultH: 2, component: CalendarWidget },
  'weather': { title: 'Weather', defaultW: 1, defaultH: 1, component: WeatherWidget },
  'spotify': { title: 'Spotify', defaultW: 2, defaultH: 1, component: SpotifyWidget },
  'habits': { title: 'Habits', defaultW: 2, defaultH: 1, component: HabitTrackerWidget },
  'quick-note': { title: 'Quick Note', defaultW: 1, defaultH: 1, component: QuickNoteWidget },
  'course-progress': { title: 'Course Progress', defaultW: 1, defaultH: 1, component: CourseProgressWidget },
  'quote': { title: 'Quote', defaultW: 2, defaultH: 1, component: QuoteWidget },
  'recent-files': { title: 'Recent Files', defaultW: 1, defaultH: 1, component: RecentFilesWidget },
};
