using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FocusDeck.Server.Controllers.V1
{
    [ApiController]
    [Route("v1/browser")]
    [Authorize]
    public class BrowserController : ControllerBase
    {
        private readonly AutomationDbContext _context;

        public BrowserController(AutomationDbContext context)
        {
            _context = context;
        }

        [HttpPost("snapshot")]
        public async Task<IActionResult> SnapshotTabs([FromBody] TabSnapshotRequest request)
        {
            // In a real scenario, we would store this as a "Session" or update the project state
            // For now, we will just log it or save as CapturedItems if user wants

            if (request.ProjectId.HasValue && request.Tabs != null)
            {
                foreach (var tab in request.Tabs)
                {
                    var item = new CapturedItem
                    {
                        Id = Guid.NewGuid(),
                        Url = tab.Url,
                        Title = tab.Title,
                        Kind = CapturedItemType.Page,
                        ProjectId = request.ProjectId,
                        CapturedAt = DateTime.UtcNow
                    };
                    _context.CapturedItems.Add(item);
                }
                await _context.SaveChangesAsync();
            }

            return Ok();
        }

        [HttpPost("capture")]
        public async Task<IActionResult> CapturePage([FromBody] CaptureRequest request)
        {
            var item = new CapturedItem
            {
                Id = Guid.NewGuid(),
                Url = request.Url,
                Title = request.Title,
                Content = request.Content,
                Kind = ParseKind(request.Kind),
                ProjectId = request.ProjectId,
                CapturedAt = DateTime.UtcNow
            };

            _context.CapturedItems.Add(item);
            await _context.SaveChangesAsync();

            return Ok(new { id = item.Id });
        }

        private CapturedItemType ParseKind(string kind)
        {
            return kind?.ToLower() switch
            {
                "ai_chat" => CapturedItemType.AiChat,
                "code_snippet" => CapturedItemType.CodeSnippet,
                "research_article" => CapturedItemType.ResearchArticle,
                _ => CapturedItemType.Page
            };
        }
    }

    public class TabSnapshotRequest
    {
        public List<TabInfo> Tabs { get; set; } = new();
        public Guid? ProjectId { get; set; }
    }

    public class TabInfo
    {
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }

    public class CaptureRequest
    {
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; }
        public string Kind { get; set; } = "page";
        public Guid? ProjectId { get; set; }
    }
}
