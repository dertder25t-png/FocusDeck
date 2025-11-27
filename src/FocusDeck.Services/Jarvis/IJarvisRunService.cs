using System;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities.Jarvis;

namespace FocusDeck.Services.Jarvis
{
    public interface IJarvisRunService
    {
        Task<JarvisRun> StartRunAsync(string userId, string entryPoint, string? inputPayloadJson, CancellationToken ct);
        Task AppendStepAsync(Guid runId, JarvisRunStep step, CancellationToken ct);
        Task CompleteRunAsync(Guid runId, JarvisRunStatus finalStatus, CancellationToken ct);
    }
}
