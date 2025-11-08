using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Services.Integrations;

public class CanvasSyncService : BackgroundService
{
    private readonly ILogger<CanvasSyncService> _logger;
    private readonly CanvasService _canvasService;
    private readonly ICanvasCache _cache;
    private readonly IConfiguration _configuration;

    public CanvasSyncService(ILogger<CanvasSyncService> logger, CanvasService canvasService, ICanvasCache cache, IConfiguration configuration)
    {
        _logger = logger;
        _canvasService = canvasService;
        _cache = cache;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var enable = _configuration.GetValue<bool>("Canvas:EnableGlobalSync");
                if (enable)
                {
                    var domain = _configuration["CANVAS_DOMAIN"]; // optional env/global config
                    var token = _configuration["CANVAS_TOKEN"];   // optional env/global config
                    if (!string.IsNullOrWhiteSpace(domain) && !string.IsNullOrWhiteSpace(token))
                    {
                        var items = await _canvasService.GetUpcomingAssignments(domain!, token!);
                        var ttl = TimeSpan.FromMinutes(_configuration.GetValue<int>("Canvas:CacheTtlMinutes", 20));
                        _cache.SetAssignments(items, ttl);
                        _logger.LogInformation("Canvas sync cached {Count} items", items.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Canvas sync failed");
            }

            try
            {
                var delayMin = Math.Max(1, _configuration.GetValue<int>("Canvas:SyncMinutes", 15));
                await Task.Delay(TimeSpan.FromMinutes(delayMin), stoppingToken);
            }
            catch (TaskCanceledException) { }
        }
    }
}
