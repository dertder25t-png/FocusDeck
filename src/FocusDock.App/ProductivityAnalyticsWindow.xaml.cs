using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FocusDock.Core.Services;

namespace FocusDock.App
{
    public partial class ProductivityAnalyticsWindow : Window
    {
        private readonly StudyPlanService _studyPlanService;

        public ProductivityAnalyticsWindow(StudyPlanService studyPlanService)
        {
            InitializeComponent();
            _studyPlanService = studyPlanService;
            BtnClose.Click += (_, _) => Close();
            
            LoadAnalytics();
        }

        private void LoadAnalytics()
        {
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            var sessions = _studyPlanService.GetSessionLogs()
                .Where(s => s.EndTime >= thirtyDaysAgo)
                .OrderBy(s => s.EndTime)
                .ToList();

            TxtDateRange.Text = $"Showing last 30 days ({thirtyDaysAgo:M/d/yyyy} - {DateTime.Now:M/d/yyyy})";

            if (sessions.Count == 0)
            {
                TxtTotalTime.Text = "0h";
                TxtAvgDaily.Text = "0.0h";
                TxtAvgEffectiveness.Text = "0.0/5";
                TxtTopSubject.Text = "No data";
                TxtDailyBreakdown.Text = "No study sessions recorded.";
                return;
            }

            // Calculate totals
            var totalMinutes = sessions.Sum(s => s.MinutesSpent);
            var totalHours = totalMinutes / 60.0;
            var uniqueDays = sessions.Select(s => s.EndTime?.Date).Distinct().Count();
            var avgDailyHours = uniqueDays > 0 ? totalHours / uniqueDays : 0;
            
            var effectiveRatings = sessions.Where(s => s.EffectivenessRating.HasValue).ToList();
            var avgEffectiveness = effectiveRatings.Count > 0 
                ? effectiveRatings.Average(s => s.EffectivenessRating!.Value) 
                : 0;
            
            TxtTotalTime.Text = $"{totalHours:F1}h";
            TxtAvgDaily.Text = $"{avgDailyHours:F1}h";
            TxtAvgEffectiveness.Text = $"{avgEffectiveness:F1}/5";

            // Top subject
            var topSubject = sessions
                .GroupBy(s => s.Topic ?? "Unknown")
                .OrderByDescending(g => g.Sum(s => s.MinutesSpent))
                .FirstOrDefault();
            
            if (topSubject != null)
            {
                var topMinutes = topSubject.Sum(s => s.MinutesSpent);
                TxtTopSubject.Text = $"{topSubject.Key} ({topMinutes}m)";
            }

            // Daily breakdown
            var dailyStats = sessions
                .GroupBy(s => s.EndTime?.Date)
                .OrderBy(g => g.Key)
                .Select(g => new { Date = g.Key, Minutes = g.Sum(s => s.MinutesSpent) });
            
            var dailyText = string.Join("\n", dailyStats.Select(d => 
                $"{d.Date:M/d}: {d.Minutes / 60.0:F1}h ({d.Minutes}m)"));
            TxtDailyBreakdown.Text = dailyText;

            // Subject breakdown (pie chart in text form)
            var subjectStats = sessions
                .GroupBy(s => s.Topic ?? "Untitled")
                .OrderByDescending(g => g.Sum(s => s.MinutesSpent))
                .Select(g => new { Subject = g.Key, Minutes = g.Sum(s => s.MinutesSpent), Sessions = g.Count() });
            
            LstSubjects.Items.Clear();
            foreach (var subject in subjectStats)
            {
                var percent = (subject.Minutes / (double)totalMinutes * 100);
                var item = new TextBlock
                {
                    Text = $"  {subject.Subject}: {subject.Minutes}m ({percent:F0}%) • {subject.Sessions} sessions",
                    Padding = new System.Windows.Thickness(8, 4, 0, 0),
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(204, 204, 204))
                };
                LstSubjects.Items.Add(item);
            }

            // Effectiveness trend
            if (effectiveRatings.Count > 0)
            {
                var weeklyEffectiveness = sessions
                    .Where(s => s.EffectivenessRating.HasValue)
                    .GroupBy(s => GetWeekNumber(s.EndTime ?? DateTime.Now))
                    .OrderBy(g => g.Key)
                    .Select(g => new { Week = g.Key, AvgRating = g.Average(s => s.EffectivenessRating!.Value) });
                
                var trendText = string.Join(" → ", weeklyEffectiveness.Select(w => $"Week {w.Week}: {w.AvgRating:F1}★"));
                TxtEffectivenessTrend.Text = trendText;
            }
            else
            {
                TxtEffectivenessTrend.Text = "No effectiveness ratings recorded yet.";
            }

            // Session statistics
            var longestSession = sessions.MaxBy(s => s.MinutesSpent);
            var avgSessionMinutes = sessions.Average(s => s.MinutesSpent);
            var totalBreaks = sessions.Sum(s => s.BreaksTaken);
            var avgBreaksPerSession = sessions.Average(s => s.BreaksTaken);

            var statsText = $"Total Sessions: {sessions.Count}\n" +
                $"Longest Session: {longestSession!.MinutesSpent}m\n" +
                $"Avg Session: {avgSessionMinutes:F0}m\n" +
                $"Total Breaks: {totalBreaks}\n" +
                $"Avg Breaks/Session: {avgBreaksPerSession:F1}";
            
            TxtSessionStats.Text = statsText;
        }

        private int GetWeekNumber(DateTime date)
        {
            var janFirst = new DateTime(date.Year, 1, 1);
            return (date - janFirst).Days / 7 + 1;
        }
    }
}
