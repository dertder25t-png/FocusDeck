using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Persistence;
using FocusDeck.Server.Services.Jarvis;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FocusDeck.Server.Tests;

public class JarvisWorkflowRegistryTests
{
    [Fact]
    public async Task ListWorkflowsAsync_EmptyDirectory_ReturnsEmptyList()
    {
        using var temp = new TempWorkdir();
        var workflowsRoot = Path.Combine(temp.Root, "bmad", "jarvis", "workflows");
        Directory.CreateDirectory(workflowsRoot);

        var registry = CreateRegistry(temp.Root);

        var result = await registry.ListWorkflowsAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ListWorkflowsAsync_MultipleWorkflows_ParsesMetadata()
    {
        using var temp = new TempWorkdir();
        var workflowsRoot = Path.Combine(temp.Root, "bmad", "jarvis", "workflows");
        Directory.CreateDirectory(workflowsRoot);

        var wf1Dir = Path.Combine(workflowsRoot, "wf1");
        var wf2Dir = Path.Combine(workflowsRoot, "wf2");
        Directory.CreateDirectory(wf1Dir);
        Directory.CreateDirectory(wf2Dir);

        await File.WriteAllTextAsync(
            Path.Combine(wf1Dir, "workflow.yaml"),
            """
            # Workflow 1
            name: jarvis-phase-1-activity-detection
            description: Activity detection foundation.
            """);

        await File.WriteAllTextAsync(
            Path.Combine(wf2Dir, "workflow.yaml"),
            """
            # Workflow 2
            name: jarvis-phase-2-burnout-detection
            description: Burnout detection and prevention.
            """);

        var registry = CreateRegistry(temp.Root);

        var result = await registry.ListWorkflowsAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, w => w.Id == "jarvis-phase-1-activity-detection" && w.Name == "jarvis-phase-1-activity-detection");
        Assert.Contains(result, w => w.Id == "jarvis-phase-2-burnout-detection" && w.Name == "jarvis-phase-2-burnout-detection");
    }

    [Fact]
    public async Task ListWorkflowsAsync_BadMetadataFile_IsSkippedNotFatal()
    {
        using var temp = new TempWorkdir();
        var workflowsRoot = Path.Combine(temp.Root, "bmad", "jarvis", "workflows");
        Directory.CreateDirectory(workflowsRoot);

        var goodDir = Path.Combine(workflowsRoot, "good");
        var badDir = Path.Combine(workflowsRoot, "bad");
        Directory.CreateDirectory(goodDir);
        Directory.CreateDirectory(badDir);

        await File.WriteAllTextAsync(
            Path.Combine(goodDir, "workflow.yaml"),
            """
            name: jarvis-phase-3-notification-management
            description: Notification management workflow.
            """);

        // Missing name: should be treated as bad metadata and skipped
        await File.WriteAllTextAsync(
            Path.Combine(badDir, "workflow.yaml"),
            """
            # no name field here
            description: This file is intentionally invalid.
            """);

        var registry = CreateRegistry(temp.Root);

        var result = await registry.ListWorkflowsAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("jarvis-phase-3-notification-management", result[0].Id);
    }

    private static JarvisWorkflowRegistry CreateRegistry(string contentRoot)
    {
        var options = new DbContextOptionsBuilder<AutomationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new AutomationDbContext(options);
        var jobs = new FakeBackgroundJobClient();
        var http = new HttpContextAccessor();
        var env = new TestHostEnvironment(contentRoot);
        var logger = NullLogger<JarvisWorkflowRegistry>.Instance;

        return new JarvisWorkflowRegistry(db, jobs, http, env, logger);
    }

    private sealed class TempWorkdir : IDisposable
    {
        public string Root { get; }

        public TempWorkdir()
        {
            Root = Path.Combine(Path.GetTempPath(), "jarvis-registry-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Root))
                {
                    Directory.Delete(Root, recursive: true);
                }
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string contentRootPath)
        {
            ContentRootPath = contentRootPath;
            EnvironmentName = Environments.Development;
            ApplicationName = "FocusDeck.Server.Tests";
            ContentRootFileProvider = new NullFileProvider();
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; }
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; }
    }

    private sealed class FakeBackgroundJobClient : IBackgroundJobClient
    {
        public string? Create(Job job, IState state) => Guid.NewGuid().ToString();

        public bool ChangeState(string jobId, IState state, string? expectedState) => true;
    }
}
