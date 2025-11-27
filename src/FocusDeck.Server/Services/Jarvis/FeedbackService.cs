using System.Threading.Tasks;
using FocusDeck.Contracts.DTOs;
using FocusDeck.Server.Jobs;
using FocusDeck.Services.Context;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Services.Jarvis;

/// <summary>
/// A service that processes user feedback on suggestions, stores it,
/// and triggers re-vectorization of the associated snapshot.
/// </summary>
public class FeedbackService : IFeedbackService
{
    private readonly ILogger<FeedbackService> _logger;
    private readonly IBackgroundJobClient _jobClient;
    // In a real implementation, you would inject the DbContext.
    // private readonly AutomationDbContext _dbContext;

    public FeedbackService(ILogger<FeedbackService> logger, IBackgroundJobClient jobClient /*, AutomationDbContext dbContext */)
    {
        _logger = logger;
        _jobClient = jobClient;
        // _dbContext = dbContext;
    }

    /// <summary>
    /// Processes and stores user feedback.
    /// </summary>
    /// <param name="request">The feedback data from the user.</param>
    public async Task ProcessFeedbackAsync(FeedbackRequestDto request)
    {
        _logger.LogInformation("Processing feedback for Snapshot ID: {SnapshotId} with Reward: {Reward}", request.SnapshotId, request.Reward);

        // STEP 1: Store the feedback signal in the database.
        // This requires a new `FeedbackSignal` entity and a `FeedbackSignals` table.
        // See `docs/FeedbackLoop-Implementation-Notes.md` for the table schema.
        /*
        var feedbackSignal = new FeedbackSignal
        {
            Id = Guid.NewGuid(),
            SnapshotId = request.SnapshotId,
            Reward = request.Reward,
            Timestamp = DateTime.UtcNow
        };
        _dbContext.FeedbackSignals.Add(feedbackSignal);
        await _dbContext.SaveChangesAsync();
        */

        // STEP 2: Trigger a re-vectorization of the original snapshot.
        // By re-processing the snapshot with the new feedback, the system can learn.
        // The vector could be updated with a decayed weighting to emphasize recent feedback.
        _jobClient.Enqueue<IVectorizeSnapshotJob>(job => job.Execute(request.SnapshotId, CancellationToken.None));

        _logger.LogInformation("Enqueued re-vectorization job for snapshot {SnapshotId} due to new feedback.", request.SnapshotId);

        // Placeholder to indicate completion.
        await Task.CompletedTask;
    }
}
