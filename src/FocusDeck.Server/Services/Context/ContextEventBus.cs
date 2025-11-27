using System;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities.Context;

namespace FocusDeck.Server.Services.Context
{
    public interface IContextEventBus
    {
        event Func<ContextSnapshot, Task> OnContextSnapshotCreated;
        Task PublishAsync(ContextSnapshot snapshot);
    }

    public class ContextEventBus : IContextEventBus
    {
        public event Func<ContextSnapshot, Task> OnContextSnapshotCreated = null!;

        public async Task PublishAsync(ContextSnapshot snapshot)
        {
            if (OnContextSnapshotCreated != null)
            {
                var handlers = OnContextSnapshotCreated.GetInvocationList();
                foreach (Func<ContextSnapshot, Task> handler in handlers)
                {
                    try
                    {
                        await handler(snapshot);
                    }
                    catch
                    {
                        // Log error but don't stop other handlers
                    }
                }
            }
        }
    }
}
