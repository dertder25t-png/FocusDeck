using System;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities.Context;

namespace FocusDeck.Contracts.Repositories
{
    /// <summary>
    /// Defines the contract for a repository that manages context snapshots.
    /// </summary>
    public interface IContextSnapshotRepository
    {
        /// <summary>
        /// Adds a new context snapshot to the repository.
        /// </summary>
        /// <param name="snapshot">The snapshot to add.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task AddAsync(ContextSnapshot snapshot, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a context snapshot by its ID.
        /// </summary>
        /// <param name="id">The ID of the snapshot to retrieve.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The context snapshot, or null if not found.</returns>
        Task<ContextSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the latest context snapshot for a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The latest context snapshot, or null if not found.</returns>
        Task<ContextSnapshot?> GetLatestForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
