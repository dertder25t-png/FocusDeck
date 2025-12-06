using System;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Server.Services.TextGeneration;
using FocusDeck.SharedKernel.Tenancy;

namespace FocusDeck.Server.Tests.Helpers;

/// <summary>
/// Mock implementation of ICurrentTenant for testing purposes.
/// Provides a simple tenant context that satisfies DbContext query filters.
/// </summary>
public sealed class MockCurrentTenant : ICurrentTenant
{
    private Guid? _tenantId;

    public MockCurrentTenant(Guid tenantId)
    {
        _tenantId = tenantId;
    }

    public Guid? TenantId => _tenantId;
    public bool HasTenant => _tenantId.HasValue;

    public void SetTenant(Guid tenantId)
    {
        _tenantId = tenantId;
    }
}

/// <summary>
/// Mock implementation of ITextGen for testing purposes.
/// Returns configurable JSON responses for LLM text generation.
/// </summary>
public sealed class MockTextGen : ITextGen
{
    private readonly Guid? _matchProjectId;
    private readonly double _confidence;
    private readonly string _reason;

    public MockTextGen(Guid? matchProjectId = null, double confidence = 0.9, string reason = "Test match")
    {
        _matchProjectId = matchProjectId;
        _confidence = confidence;
        _reason = reason;
    }

    public Task<string> GenerateAsync(string prompt, int maxTokens = 500, double temperature = 0.7, CancellationToken cancellationToken = default)
    {
        var response = _matchProjectId.HasValue
            ? $"{{\"projectId\": \"{_matchProjectId}\", \"confidence\": {_confidence}, \"reason\": \"{_reason}\"}}"
            : "{\"projectId\": null, \"confidence\": 0.0, \"reason\": \"No match found\"}";
        return Task.FromResult(response);
    }
}
