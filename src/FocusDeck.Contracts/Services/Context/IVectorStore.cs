using System;
using System.Threading.Tasks;

namespace FocusDeck.Contracts.Services.Context
{
    public interface IVectorStore
    {
        Task UpsertAsync(Guid snapshotId, string text);
    }
}
