using System;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities.Context;

namespace FocusDeck.Services.Context
{
    /// <summary>
    /// Defines the contract for a service that coordinates the capture and processing of context snapshots.
    /// </summary>
    public interface IContextSnapshotService
    {
        /// <summary>
        /// Captures a new context snapshot for the specified user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The captured context snapshot.</returns>
        Task<ContextSnapshot> CaptureNowAsync(Guid userId, CancellationToken ct);
    }
}
