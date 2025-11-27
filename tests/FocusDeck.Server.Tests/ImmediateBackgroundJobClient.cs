using System;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.DependencyInjection;

namespace FocusDeck.Server.Tests;

internal sealed class ImmediateBackgroundJobClient : IBackgroundJobClient
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ImmediateBackgroundJobClient(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public string Create(Job job, IState state)
    {
        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService(job.Type);
        var result = job.Method.Invoke(service, job.Args.ToArray());

        if (result is Task task)
        {
            task.GetAwaiter().GetResult();
        }

        return Guid.NewGuid().ToString();
    }

    public bool ChangeState(string jobId, IState state, string expectedState) => true;

    public bool ChangeState(string jobId, IState state, string[] expectedStates) => true;
}
