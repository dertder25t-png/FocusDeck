using Asp.Versioning;
using FocusDeck.Domain.Entities;
using FocusDeck.Server.Services.Browser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FocusDeck.Server.Controllers.v1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/browser")]
    [Authorize]
    public class BrowserController : ControllerBase
    {
        private readonly IBrowserContextService _browserService;
        private readonly FocusDeck.SharedKernel.Tenancy.ICurrentTenant _currentTenant;

        public BrowserController(IBrowserContextService browserService, FocusDeck.SharedKernel.Tenancy.ICurrentTenant currentTenant)
        {
            _browserService = browserService;
            _currentTenant = currentTenant;
        }

        [HttpPost("tabs/snapshot")]
        public async Task<IActionResult> SnapshotTabs([FromBody] List<TabSnapshotDto> tabs)
        {
            // In browser extension, we might send a device ID header
            var deviceId = Request.Headers["X-Device-ID"].FirstOrDefault() ?? "unknown-browser";

            if (!_currentTenant.HasTenant) return Unauthorized();

            var snapshots = tabs.Select(t => new TabSnapshot
            {
                Url = t.Url,
                Title = t.Title,
                IsActive = t.IsActive
            }).ToList();

            await _browserService.ProcessTabSnapshotAsync(deviceId, snapshots, _currentTenant.TenantId!.Value);
            return Ok();
        }

        [HttpPost("capture")]
        public async Task<IActionResult> CapturePage([FromBody] CapturePageDto request)
        {
            if (!_currentTenant.HasTenant) return Unauthorized();

            var item = new CapturedItem
            {
                Url = request.Url,
                Title = request.Title,
                Content = request.Content,
                Kind = request.Kind,
                ProjectId = request.ProjectId,
                TenantId = _currentTenant.TenantId!.Value
            };

            var id = await _browserService.CaptureItemAsync(item);
            return Ok(new { id });
        }
    }

    public class TabSnapshotDto
    {
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class CapturePageDto
    {
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; }
        public CapturedItemType Kind { get; set; }
        public Guid? ProjectId { get; set; }
    }
}
