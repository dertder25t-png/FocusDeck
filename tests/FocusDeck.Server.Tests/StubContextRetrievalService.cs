using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Contracts.Services.Context;
using FocusDeck.Domain.Entities.Context;

namespace FocusDeck.Server.Tests
{
    public class StubContextRetrievalService : IContextRetrievalService
    {
        public Task<List<ContextSnapshot>> GetSimilarMomentsAsync(ContextSnapshot current)
        {
            return Task.FromResult(new List<ContextSnapshot>());
        }
    }
}
