using System;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Server.Services.Burnout;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Jobs;

public sealed class BurnoutCheckJob
{
    private readonly IBurnoutAnalysisService _analysisService;
    private readonly ILogger<BurnoutCheckJob> _logger;

    public BurnoutCheckJob(IBurnoutAnalysisService analysisService, ILogger<BurnoutCheckJob> logger)
    {
        _analysisService = analysisService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("BurnoutCheckJob starting");
        try
        {
            await _analysisService.AnalyzePatternsAsync(cancellationToken);
            _logger.LogInformation("BurnoutCheckJob completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BurnoutCheckJob failed");
            throw;
        }
    }
}
