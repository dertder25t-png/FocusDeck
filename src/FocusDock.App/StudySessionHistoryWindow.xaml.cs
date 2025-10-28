using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FocusDock.Core.Services;
using FocusDock.Data.Models;

namespace FocusDock.App
{
    public partial class StudySessionHistoryWindow : Window
    {
        private readonly StudyPlanService _studyPlanService;
        private List<StudySessionLog> _allSessions = new();

        public StudySessionHistoryWindow(StudyPlanService studyPlanService)
        {
            InitializeComponent();
            
            _studyPlanService = studyPlanService;
            
            // Set default date range (last 30 days)
            DtpToDate.SelectedDate = DateTime.Now;
            DtpFromDate.SelectedDate = DateTime.Now.AddDays(-30);
            
            BtnFilter.Click += (_, _) => RefreshSessions();
            BtnClose.Click += (_, _) => Close();
            BtnExport.Click += (_, _) => ExportSessions();
            BtnAnalytics.Click += (_, _) => ShowAnalytics();
            
            // Load initial data
            RefreshSessions();
        }

        private void ShowAnalytics()
        {
            var analyticsWindow = new ProductivityAnalyticsWindow(_studyPlanService) { Owner = this };
            analyticsWindow.ShowDialog();
        }

        private void RefreshSessions()
        {
            var fromDate = DtpFromDate.SelectedDate ?? DateTime.Now.AddDays(-30);
            var toDate = DtpToDate.SelectedDate ?? DateTime.Now;
            
            // Get all session logs from the service
            _allSessions = _studyPlanService.GetSessionLogs()
                .Where(s => (s.EndTime ?? DateTime.Now) >= fromDate && (s.EndTime ?? DateTime.Now) <= toDate.AddDays(1))
                .OrderByDescending(s => s.EndTime)
                .ToList();
            
            UpdateStatistics();
            DisplaySessions();
        }

        private void UpdateStatistics()
        {
            if (_allSessions.Count == 0)
            {
                TxtTotalSessions.Text = "0";
                TxtTotalHours.Text = "0.0h";
                TxtAvgEffectiveness.Text = "0.0/5.0";
                TxtTotalBreaks.Text = "0";
                return;
            }
            
            var totalMinutes = _allSessions.Sum(s => s.MinutesSpent);
            var totalHours = totalMinutes / 60.0;
            var avgRating = _allSessions
                .Where(s => s.EffectivenessRating.HasValue)
                .Average(s => (double)s.EffectivenessRating!.Value);
            var totalBreaks = _allSessions.Sum(s => s.BreaksTaken);
            
            TxtTotalSessions.Text = _allSessions.Count.ToString();
            TxtTotalHours.Text = $"{totalHours:F1}h";
            TxtAvgEffectiveness.Text = double.IsNaN(avgRating) ? "0.0/5.0" : $"{avgRating:F1}/5.0";
            TxtTotalBreaks.Text = totalBreaks.ToString();
        }

        private void DisplaySessions()
        {
            LstSessions.ItemsSource = _allSessions.Select(s => new SessionDisplayItem
            {
                Subject = s.Topic ?? "Untitled Session",
                DateTime = s.EndTime ?? DateTime.Now,
                DurationMinutes = s.MinutesSpent,
                EffectivenessRating = s.EffectivenessRating?.ToString() ?? "N/A",
                BreaksTaken = s.BreaksTaken
            }).ToList();
        }

        private void ExportSessions()
        {
            if (_allSessions.Count == 0)
            {
                System.Windows.MessageBox.Show("No sessions to export.", "Export");
                return;
            }
            
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Subject,Date,Duration (min),Effectiveness,Breaks,Notes");
            
            foreach (var session in _allSessions.OrderBy(s => s.EndTime))
            {
                csv.AppendLine($"\"{session.Topic}\",{session.EndTime:yyyy-MM-dd HH:mm},{session.MinutesSpent}," +
                    $"{session.EffectivenessRating?.ToString() ?? "N/A"},{session.BreaksTaken},\"{session.Notes ?? ""}\"");
            }
            
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var fileName = $"StudySessions_{DateTime.Now:yyyy-MM-dd_HHmmss}.csv";
            var filePath = System.IO.Path.Combine(documentsPath, fileName);
            
            try
            {
                System.IO.File.WriteAllText(filePath, csv.ToString());
                System.Windows.MessageBox.Show($"Sessions exported to:\n{filePath}", "Export Successful");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Export failed: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Helper class for data binding in ListBox
        /// </summary>
        public class SessionDisplayItem
        {
            public string Subject { get; set; } = string.Empty;
            public DateTime DateTime { get; set; }
            public int DurationMinutes { get; set; }
            public string EffectivenessRating { get; set; } = "N/A";
            public int BreaksTaken { get; set; }
        }
    }
}
