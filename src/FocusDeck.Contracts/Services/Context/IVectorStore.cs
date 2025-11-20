using System;
using System.Threading.Tasks;

namespace FocusDeck.Contracts.Services.Context
{
    public interface IVectorStore
    {
        Task UpsertAsync(Guid snapshotId, string text);
        Task<System.Collections.Generic.List<FocusDeck.Domain.Entities.Context.ContextSnapshot>> GetNearestNeighborsAsync(float[] queryVector, int limit = 5, double minRelevance = 0.7);
    }
}
