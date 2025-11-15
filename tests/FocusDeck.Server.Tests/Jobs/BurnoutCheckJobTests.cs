using System;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Server.Jobs;
using FocusDeck.Server.Services.Burnout;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FocusDeck.Server.Tests.Jobs;

public class BurnoutCheckJobTests
{
    [Fact]
    public async Task ExecuteAsync_InvokesAnalysisService()
    {
        var invoked = false;
        var service = new TestBurnoutService(ct =>
        {
            invoked = true;
            return Task.CompletedTask;
        });

        var job = new BurnoutCheckJob(service, NullLogger<BurnoutCheckJob>.Instance);

        await job.ExecuteAsync(CancellationToken.None);

        Assert.True(invoked, "The burnout analysis service should be triggered by the job.");
    }

    private sealed class TestBurnoutService : IBurnoutAnalysisService
    {
        private readonly Func<CancellationToken, Task> _callback;

        public TestBurnoutService(Func<CancellationToken, Task> callback)
        {
            _callback = callback;
        }

        public Task AnalyzePatternsAsync(CancellationToken cancellationToken = default)
            => _callback(cancellationToken);
    }
}
