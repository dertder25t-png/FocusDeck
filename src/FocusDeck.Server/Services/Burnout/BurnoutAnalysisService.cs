using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Services.Burnout;

public sealed class BurnoutAnalysisService : IBurnoutAnalysisService
{
    private const int LookbackDays = 7;
    private static readonly TimeSpan SessionGapThreshold = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan MinSessionDuration = TimeSpan.FromMinutes(5);
    private const double SleepGapHours = 4;
    private const double MaxSleepHours = 12;

    private readonly AutomationDbContext _db;
    private readonly ILogger<BurnoutAnalysisService> _logger;

    public BurnoutAnalysisService(AutomationDbContext db, ILogger<BurnoutAnalysisService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task AnalyzePatternsAsync(CancellationToken cancellationToken = default)
    {
        var lookbackWindow = DateTime.UtcNow.AddDays(-LookbackDays);

        var signals = await _db.ActivitySignals
            .AsNoTracking()
            .Where(signal => signal.CapturedAtUtc >= lookbackWindow &&
                             signal.TenantId != Guid.Empty &&
                             !string.IsNullOrWhiteSpace(signal.UserId))
            .OrderBy(signal => signal.TenantId)
            .ThenBy(signal => signal.UserId)
            .ThenBy(signal => signal.CapturedAtUtc)
            .ToListAsync(cancellationToken);

        if (signals.Count == 0)
        {
            _logger.LogInformation("Burnout analysis found no activity signals in the last {Days} days.", LookbackDays);
            return;
        }

        var metricsToPersist = new List<StudentWellnessMetrics>();

        foreach (var grouping in signals.GroupBy(signal => new { signal.TenantId, signal.UserId }))
        {
            var result = AnalyzeUserSignals(grouping.ToList());
            if (result == null)
            {
                continue;
            }

            metricsToPersist.Add(new StudentWellnessMetrics
            {
                TenantId = grouping.Key.TenantId,
                UserId = grouping.Key.UserId,
                CapturedAtUtc = DateTime.UtcNow,
                HoursWorked = result.Value.AverageHours,
                BreakFrequency = result.Value.BreakFrequency,
                QualityScore = result.Value.QualityScore,
                SleepHours = result.Value.SleepHours,
                IsUnsustainable = result.Value.IsUnsustainable,
                Notes = result.Value.Notes
            });
        }

        if (metricsToPersist.Count == 0)
        {
            _logger.LogInformation("Burnout analysis did not generate metrics for any user.");
            return;
        }

        _db.StudentWellnessMetrics.AddRange(metricsToPersist);
        await _db.SaveChangesAsync(cancellationToken);

        foreach (var metric in metricsToPersist)
        {
            if (metric.IsUnsustainable)
            {
                _logger.LogWarning(
                    "Unsustainable pattern detected for user {UserId} (tenant {TenantId}): {Notes}",
                    metric.UserId,
                    metric.TenantId,
                    metric.Notes);
            }
            else
            {
                _logger.LogInformation(
                    "Burnout metrics persisted for user {UserId} (tenant {TenantId}): {HoursWorked:F1}h worked, break frequency {BreakFrequency:P0}, quality {QualityScore:P0}, sleep {SleepHours:F1}h.",
                    metric.UserId,
                    metric.TenantId,
                    metric.HoursWorked,
                    metric.BreakFrequency,
                    metric.QualityScore,
                    metric.SleepHours);
            }
        }
    }

    private static AnalysisResult? AnalyzeUserSignals(IReadOnlyList<ActivitySignal> signals)
    {
        if (signals.Count == 0)
        {
            return null;
        }

        var sessions = BuildWorkSessions(signals);
        if (sessions.Count == 0)
        {
            return null;
        }

        var dailyHours = sessions
            .GroupBy(session => session.Start.Date)
            .ToDictionary(g => g.Key, g => g.Sum(session => session.Duration.TotalHours));

        var breakFrequencies = ComputeBreakFrequenciesByDay(sessions);
        var consecutiveLongDays = CountConsecutiveLongDays(dailyHours);
        var breakDropDetected = DetectBreakDrop(breakFrequencies);
        var dailyAverageHours = dailyHours.Values.Count > 0 ? dailyHours.Values.Average() : 0.0;
        var averageBreakFrequency = breakFrequencies.Count > 0 ? breakFrequencies.Average() : 0.0;
        var stdDevMinutes = CalculateStandardDeviation(sessions.Select(s => s.Duration.TotalMinutes));

        var qualityScore = Math.Clamp(1 - Math.Min(stdDevMinutes / 60.0, 1.0), 0.0, 1.0);
        if (breakDropDetected)
        {
            qualityScore = Math.Max(0.0, qualityScore - 0.1);
        }

        var sleepHours = CalculateSleepHours(sessions);
        var unsustainable = consecutiveLongDays >= 3 || breakDropDetected;
        var notes = BuildNotes(consecutiveLongDays, breakDropDetected, dailyAverageHours, averageBreakFrequency);

        return new AnalysisResult(
            AverageHours: dailyAverageHours,
            BreakFrequency: averageBreakFrequency,
            QualityScore: qualityScore,
            SleepHours: sleepHours,
            IsUnsustainable: unsustainable,
            Notes: notes);
    }

    private static List<WorkSession> BuildWorkSessions(IReadOnlyList<ActivitySignal> signals)
    {
        var sessions = new List<WorkSession>();
        DateTime? windowStart = null;
        DateTime? windowEnd = null;

        foreach (var signal in signals)
        {
            if (windowStart == null)
            {
                windowStart = signal.CapturedAtUtc;
                windowEnd = signal.CapturedAtUtc;
                continue;
            }

            var gap = signal.CapturedAtUtc - windowEnd!.Value;
            if (gap > SessionGapThreshold)
            {
                sessions.Add(CreateSession(windowStart.Value, windowEnd.Value));
                windowStart = signal.CapturedAtUtc;
            }

            windowEnd = signal.CapturedAtUtc;
        }

        if (windowStart.HasValue && windowEnd.HasValue)
        {
            sessions.Add(CreateSession(windowStart.Value, windowEnd.Value));
        }

        return sessions;
    }

    private static WorkSession CreateSession(DateTime start, DateTime end)
    {
        var duration = end - start;
        if (duration < MinSessionDuration)
        {
            end = start + MinSessionDuration;
            duration = MinSessionDuration;
        }

        return new WorkSession(start, end, duration);
    }

    private static List<double> ComputeBreakFrequenciesByDay(IReadOnlyList<WorkSession> sessions)
    {
        var frequencies = new List<double>();

        foreach (var group in sessions.GroupBy(session => session.Start.Date).OrderBy(group => group.Key))
        {
            var sessionCount = group.Count();
            if (sessionCount <= 1)
            {
                frequencies.Add(0);
                continue;
            }

            frequencies.Add((sessionCount - 1) / (double)sessionCount);
        }

        return frequencies;
    }

    private static bool DetectBreakDrop(IReadOnlyList<double> frequencies)
    {
        if (frequencies.Count < 2)
        {
            return false;
        }

        var last = frequencies.Last();
        var earlierAverage = frequencies.Take(frequencies.Count - 1).Average();
        return earlierAverage > 0 && last < earlierAverage * 0.5;
    }

    private static int CountConsecutiveLongDays(Dictionary<DateTime, double> dailyHours)
    {
        var orderedDays = dailyHours.OrderBy(pair => pair.Key).ToList();
        if (orderedDays.Count == 0)
        {
            return 0;
        }

        var longest = 0;
        var current = 0;
        DateTime? previousDate = null;

        foreach (var (date, hours) in orderedDays)
        {
            var isLong = hours >= 12;
            if (previousDate.HasValue && (date - previousDate.Value).Days == 1)
            {
                current = isLong ? current + 1 : 0;
            }
            else
            {
                current = isLong ? 1 : 0;
            }

            longest = Math.Max(longest, current);
            previousDate = date;
        }

        return longest;
    }

    private static double CalculateSleepHours(IReadOnlyList<WorkSession> sessions)
    {
        var gaps = new List<double>();
        for (var i = 1; i < sessions.Count; i++)
        {
            var gapHours = (sessions[i].Start - sessions[i - 1].End).TotalHours;
            if (gapHours >= SleepGapHours)
            {
                gaps.Add(Math.Min(gapHours, MaxSleepHours));
            }
        }

        return gaps.Count == 0 ? 7.0 : Math.Clamp(gaps.Average(), 4.0, MaxSleepHours);
    }

    private static double CalculateStandardDeviation(IEnumerable<double> values)
    {
        var array = values.Where(value => !double.IsNaN(value)).ToArray();
        if (array.Length <= 1)
        {
            return 0.0;
        }

        var average = array.Average();
        var sumOfSquaredDifferences = array.Sum(value => (value - average) * (value - average));
        return Math.Sqrt(sumOfSquaredDifferences / array.Length);
    }

    private static string BuildNotes(int consecutiveLongDays, bool breakDrop, double averageHours, double breakFrequency)
        => $"ConsecutiveLongDays={consecutiveLongDays};BreakDrop={breakDrop};AvgHours={averageHours:F1};BreakFreq={breakFrequency:P0}";

    private readonly record struct WorkSession(DateTime Start, DateTime End, TimeSpan Duration);
    private readonly record struct AnalysisResult(
        double AverageHours,
        double BreakFrequency,
        double QualityScore,
        double SleepHours,
        bool IsUnsustainable,
        string Notes);
}
