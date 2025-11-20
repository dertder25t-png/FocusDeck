using System.Collections.Generic;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities.Context;

namespace FocusDeck.Contracts.Services.Context
{
    public interface IContextRetrievalService
    {
        /// <summary>
        /// Retrieves past snapshots similar to the current one.
        /// </summary>
        /// <param name="current">The current context snapshot.</param>
        /// <returns>A list of similar past snapshots.</returns>
        Task<List<ContextSnapshot>> GetSimilarMomentsAsync(ContextSnapshot current);
    }
}
