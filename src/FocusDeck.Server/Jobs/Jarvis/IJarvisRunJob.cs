using System;
using System.Threading.Tasks;

namespace FocusDeck.Server.Jobs.Jarvis
{
    public interface IJarvisRunJob
    {
        Task ExecuteAsync(Guid runId);
    }
}
