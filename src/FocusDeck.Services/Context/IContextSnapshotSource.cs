using System;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities.Context;

namespace FocusDeck.Services.Context
{
    /// <summary>
    /// Defines the contract for a source of context snapshots.
    /// </summary>
    public interface IContextSnapshotSource
    {
        /// <summary>
        /// Gets the name of the source.
        /// </summary>
        string SourceName { get; }

        /// <summary>
        /// Captures a slice of context from the source.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The captured context slice, or null if no data is available.</returns>
        Task<ContextSlice?> CaptureAsync(Guid userId, CancellationToken ct);
    }
}
