import { useQuery } from '@tanstack/react-query';
import { analytics } from '../services/api';

export function useAnalytics(days: number, courseId: string | null) {
  const overviewQuery = useQuery({
    queryKey: ['analytics', 'overview', days, courseId],
    queryFn: () => analytics.getOverview(days, courseId),
    staleTime: 5 * 60 * 1000
  });

  const focusQuery = useQuery({
    queryKey: ['analytics', 'focus', days, courseId],
    queryFn: () => analytics.getFocusMinutes(days, courseId),
    staleTime: 5 * 60 * 1000
  });

  const lecturesQuery = useQuery({
    queryKey: ['analytics', 'lectures', days, courseId],
    queryFn: () => analytics.getLecturesTimeline(days, courseId),
    staleTime: 5 * 60 * 1000
  });

  const suggestionsQuery = useQuery({
    queryKey: ['analytics', 'suggestions', days, courseId],
    queryFn: () => analytics.getSuggestionsAccepted(days, courseId),
    staleTime: 5 * 60 * 1000
  });

  const coursesQuery = useQuery({
    queryKey: ['analytics', 'courses'],
    queryFn: analytics.getCourses,
    staleTime: 60 * 60 * 1000 // Cache courses for 1 hour
  });

  const isLoading = overviewQuery.isLoading || focusQuery.isLoading || lecturesQuery.isLoading || suggestionsQuery.isLoading || coursesQuery.isLoading;
  const isError = overviewQuery.isError || focusQuery.isError || lecturesQuery.isError || suggestionsQuery.isError || coursesQuery.isError;

  return {
    overview: overviewQuery.data,
    focus: focusQuery.data?.series || [], // API returns { series: [...] }
    lectures: lecturesQuery.data?.series || [],
    suggestions: suggestionsQuery.data?.series || [],
    courses: coursesQuery.data || [],
    isLoading,
    isError
  };
}
