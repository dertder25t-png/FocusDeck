namespace FocusDock.Core.Services;

using FocusDock.Data;
using FocusDock.Data.Models;

public class StudyPlanService
{
    public event EventHandler<StudyPlan>? PlanCreated;
    public event EventHandler<StudySessionLog>? SessionStarted;
    public event EventHandler<StudySessionLog>? SessionEnded;

    private List<StudyPlan> _plans;
    private List<StudySessionLog> _sessionLogs;
    private StudySessionLog? _activeSession;

    public StudyPlanService()
    {
        _plans = TodoStore.LoadPlans();
        _sessionLogs = TodoStore.LoadSessionLogs();
    }

    public StudyPlan CreatePlanFromAssignments(
        string title,
        List<CanvasAssignment> assignments,
        DateTime targetDate)
    {
        var plan = new StudyPlan
        {
            Title = title,
            Description = $"Study plan for {assignments.Count} assignments",
            StartDate = DateTime.Now,
            TargetCompleteDate = targetDate,
            RelatedItems = assignments.Select(a => a.Id).ToList(),
        };

        // Generate recommended study sessions (simplified for now)
        var daysUntilDue = (int)(targetDate - DateTime.Now).TotalDays;
        var hoursNeeded = CalculateStudyHoursNeeded(assignments);
        plan.EstimatedHours = hoursNeeded;

        // Distribute study sessions across available days (2-3 hours per day)
        GenerateStudySessions(plan, daysUntilDue, hoursNeeded);

        _plans.Add(plan);
        TodoStore.SavePlans(_plans);
        PlanCreated?.Invoke(this, plan);

        return plan;
    }

    private double CalculateStudyHoursNeeded(List<CanvasAssignment> assignments)
    {
        // Simple heuristic: 30 min per assignment, plus points-based scaling
        double hours = assignments.Count * 0.5;

        foreach (var a in assignments)
        {
            if (a.PointsPossible.HasValue && a.PointsPossible > 100)
            {
                hours += (a.PointsPossible.Value - 100) / 100; // Extra hour per 100 points
            }
        }

        return Math.Max(2, Math.Min(20, hours)); // Clamp 2-20 hours
    }

    private void GenerateStudySessions(StudyPlan plan, int daysAvailable, double hoursNeeded)
    {
        var sessions = new List<StudySession>();
        var hoursPerDay = Math.Min(3, hoursNeeded / Math.Max(1, daysAvailable));
        var startDate = DateTime.Now.Date.AddDays(1); // Start tomorrow

        // Generate ~1 session per day
        for (int day = 0; day < daysAvailable; day++)
        {
            if (hoursNeeded <= 0) break;

            var sessionDate = startDate.AddDays(day);
            var sessionHours = Math.Min(hoursPerDay, hoursNeeded);
            var durationMinutes = (int)(sessionHours * 60);

            // Schedule in afternoon (2 PM start)
            var startTime = sessionDate.AddHours(14);

            sessions.Add(new StudySession
            {
                StartTime = startTime,
                EndTime = startTime.AddMinutes(durationMinutes),
                Topic = "Review & Practice",
                Technique = "Pomodoro",
                BreakMinutes = 5
            });

            hoursNeeded -= sessionHours;
        }

        plan.Sessions = sessions;
    }

    public List<StudySession> GetUpcomingSessions()
    {
        return _plans
            .SelectMany(p => p.Sessions)
            .Where(s => s.StartTime > DateTime.Now)
            .OrderBy(s => s.StartTime)
            .ToList();
    }

    public StudySession? GetCurrentSession()
    {
        return _plans
            .SelectMany(p => p.Sessions)
            .FirstOrDefault(s => s.StartTime <= DateTime.Now && DateTime.Now < s.EndTime);
    }

    public void StartSession(string topic)
    {
        if (_activeSession?.IsActive() ?? false)
        {
            EndSession(); // End previous session first
        }

        _activeSession = new StudySessionLog
        {
            StartTime = DateTime.Now,
            Topic = topic
        };

        SessionStarted?.Invoke(this, _activeSession);
    }

    public void EndSession(int? effectivenessRating = null, string notes = "")
    {
        if (_activeSession == null) return;

        _activeSession.EndTime = DateTime.Now;
        _activeSession.MinutesSpent = _activeSession.ElapseMinutes();
        _activeSession.EffectivenessRating = effectivenessRating;
        _activeSession.Notes = notes;

        _sessionLogs.Add(_activeSession);
        TodoStore.SaveSessionLogs(_sessionLogs);

        SessionEnded?.Invoke(this, _activeSession);
        _activeSession = null;
    }

    public List<StudySessionLog> GetSessionHistory(int daysBack = 7)
    {
        var cutoff = DateTime.Now.AddDays(-daysBack);
        return _sessionLogs
            .Where(log => log.StartTime >= cutoff)
            .OrderByDescending(log => log.StartTime)
            .ToList();
    }

    public int GetTotalMinutesStudied(int daysBack = 7)
    {
        return GetSessionHistory(daysBack).Sum(log => log.MinutesSpent);
    }

    public double GetAverageEffectiveness(int daysBack = 7)
    {
        var sessions = GetSessionHistory(daysBack)
            .Where(log => log.EffectivenessRating.HasValue)
            .ToList();

        if (sessions.Count == 0) return 0;

        return sessions.Average(log => log.EffectivenessRating!.Value);
    }

    public string GetProductivitySummary(int daysBack = 7)
    {
        var sessions = GetSessionHistory(daysBack);
        var minutesSpent = sessions.Sum(s => s.MinutesSpent);
        var hoursSpent = minutesSpent / 60.0;
        var avgEffectiveness = GetAverageEffectiveness(daysBack);

        return $"{sessions.Count} sessions • {hoursSpent:F1}h spent • {avgEffectiveness:F1}/5 effectiveness";
    }

    public List<StudyPlan> GetAllPlans() => _plans.ToList();

    public StudyPlan? GetPlan(string id) => _plans.FirstOrDefault(p => p.Id == id);

    public void DeletePlan(string id)
    {
        _plans.RemoveAll(p => p.Id == id);
        TodoStore.SavePlans(_plans);
    }

    /// <summary>
    /// Log the end of a study session with effectiveness rating
    /// </summary>
    public void EndSession(StudySession session)
    {
        session.EndTime = DateTime.Now;

        var log = new StudySessionLog
        {
            Id = session.Id,
            StartTime = session.StartTime,
            EndTime = session.EndTime,
            Topic = session.Subject,
            MinutesSpent = session.DurationMinutes,
            EffectivenessRating = session.EffectivenessRating,
            Notes = session.Notes
        };

        _sessionLogs.Add(log);
        TodoStore.SaveSessionLogs(_sessionLogs);
        _activeSession = null;
        SessionEnded?.Invoke(this, log);
    }

    public StudySessionLog? GetActiveSession() => _activeSession;

    /// <summary>
    /// Get all study session logs
    /// </summary>
    public List<StudySessionLog> GetSessionLogs() => _sessionLogs.ToList();
}
