using System;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Contracts.Repositories;
using FocusDeck.Services.Context;
using Microsoft.AspNetCore.Mvc;

namespace FocusDeck.Server.Controllers.V1.Context
{
    [ApiController]
    [Route("api/v1/context")]
    public class ContextController : ControllerBase
    {
        private readonly IContextSnapshotService _snapshotService;
        private readonly IContextSnapshotRepository _snapshotRepository;

        public ContextController(IContextSnapshotService snapshotService, IContextSnapshotRepository snapshotRepository)
        {
            _snapshotService = snapshotService;
            _snapshotRepository = snapshotRepository;
        }

        [HttpPost("snapshots")]
        public async Task<IActionResult> CaptureSnapshot(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var snapshot = await _snapshotService.CaptureNowAsync(userId, cancellationToken);
            return Ok(snapshot);
        }

        [HttpGet("snapshots/latest")]
        public async Task<IActionResult> GetLatestSnapshot(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var snapshot = await _snapshotRepository.GetLatestForUserAsync(userId, cancellationToken);
            if (snapshot == null)
            {
                return NotFound();
            }
            return Ok(snapshot);
        }

        [HttpGet("snapshots/{id}")]
        public async Task<IActionResult> GetSnapshot(Guid id, CancellationToken cancellationToken)
        {
            var snapshot = await _snapshotRepository.GetByIdAsync(id, cancellationToken);
            if (snapshot == null)
            {
                return NotFound();
            }
            return Ok(snapshot);
        }

        private Guid GetCurrentUserId()
        {
            // TODO: Get the current user ID from the HttpContext.
            return Guid.NewGuid();
        }
    }
}
