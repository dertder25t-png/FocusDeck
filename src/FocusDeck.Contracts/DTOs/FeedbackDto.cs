using System;

namespace FocusDeck.Contracts.DTOs;

/// <summary>
/// Represents the request payload for submitting feedback on a suggestion.
/// </summary>
/// <param name="SnapshotId">The ID of the snapshot that the feedback is associated with.</param>
/// <param name="Reward">A value indicating the user's feedback (e.g., 1.0 for positive, -1.0 for negative).</param>
public record FeedbackRequestDto(Guid SnapshotId, double Reward);
