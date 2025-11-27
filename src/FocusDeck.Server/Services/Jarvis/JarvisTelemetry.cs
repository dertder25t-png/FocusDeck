using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

namespace FocusDeck.Server.Services.Jarvis;

internal static class JarvisTelemetry
{
    private const string MeterName = "FocusDeck.Jarvis";
    private const string MeterVersion = "1.0";
    private static readonly Meter Meter = new(MeterName, MeterVersion);

    private static readonly Counter<long> RunsStartedCounter = Meter.CreateCounter<long>("jarvis.runs.started");
    private static readonly Counter<long> RunsSucceededCounter = Meter.CreateCounter<long>("jarvis.runs.succeeded");
    private static readonly Counter<long> RunsFailedCounter = Meter.CreateCounter<long>("jarvis.runs.failed");
    private static readonly Histogram<double> RunDurationSecondsHistogram =
        Meter.CreateHistogram<double>("jarvis.runs.duration.seconds");

    public static void RecordRunStarted(Guid? tenantId, string workflowId)
        => RunsStartedCounter.Add(1, BuildTags(tenantId, workflowId, null));

    public static void RecordRunSucceeded(Guid? tenantId, string workflowId, double durationSeconds)
    {
        RunsSucceededCounter.Add(1, BuildTags(tenantId, workflowId, "Succeeded"));
        RunDurationSecondsHistogram.Record(durationSeconds, BuildTags(tenantId, workflowId, "Succeeded"));
    }

    public static void RecordRunFailed(Guid? tenantId, string workflowId, double durationSeconds, string reason)
    {
        RunsFailedCounter.Add(1, BuildTags(tenantId, workflowId, "Failed", reason));
        RunDurationSecondsHistogram.Record(durationSeconds, BuildTags(tenantId, workflowId, "Failed", reason));
    }

    private static KeyValuePair<string, object?>[] BuildTags(Guid? tenantId, string workflowId, string? status, string? reason = null)
    {
        var tags = new List<KeyValuePair<string, object?>>
        {
            new("tenant_id", tenantId?.ToString() ?? "unknown"),
            new("workflow_id", string.IsNullOrWhiteSpace(workflowId) ? "unknown" : workflowId)
        };

        if (!string.IsNullOrWhiteSpace(status))
        {
            tags.Add(new("status", status));
        }

        if (!string.IsNullOrWhiteSpace(reason))
        {
            tags.Add(new("reason", reason));
        }

        return tags.ToArray();
    }
}

