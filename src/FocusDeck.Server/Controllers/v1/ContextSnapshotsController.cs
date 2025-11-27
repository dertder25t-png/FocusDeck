using Asp.Versioning;
using FocusDeck.Contracts.DTOs;
using FocusDeck.Server.Services.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FocusDeck.Server.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/jarvis/snapshots")]
[Authorize]
public class ContextSnapshotsController : ControllerBase
{
    private readonly ISnapshotIngestService _ingestService;

    public ContextSnapshotsController(ISnapshotIngestService ingestService)
    {
        _ingestService = ingestService;
    }

    [HttpPost]
    public async Task<IActionResult> PostSnapshot([FromBody] ContextSnapshotDto dto, CancellationToken cancellationToken)
    {
        if (dto == null)
        {
            return BadRequest(new { error = "Snapshot payload is required." });
        }

        await _ingestService.IngestSnapshotAsync(dto, cancellationToken);

        return Accepted();
    }
}
