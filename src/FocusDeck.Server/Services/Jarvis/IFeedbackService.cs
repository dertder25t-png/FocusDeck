using System.Threading.Tasks;
using FocusDeck.Contracts.DTOs;

namespace FocusDeck.Server.Services.Jarvis;

/// <summary>
/// Defines the contract for a service that processes user feedback on suggestions.
/// </summary>
public interface IFeedbackService
{
    /// <summary>
    /// Processes feedback submitted by the user.
    /// </summary>
    /// <param name="request">The feedback request from the user.</param>
    Task ProcessFeedbackAsync(FeedbackRequestDto request);
}
