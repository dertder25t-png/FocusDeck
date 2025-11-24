using System;
using System.Threading.Tasks;
using Asp.Versioning;
using FocusDeck.Server.Services.Context;
using FocusDeck.SharedKernel.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FocusDeck.Server.Controllers.v1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/ambient")]
    [Authorize]
    public class AmbientController : ControllerBase
    {
        private readonly AmbientService _ambientService;
        private readonly ICurrentTenant _currentTenant;

        public AmbientController(AmbientService ambientService, ICurrentTenant currentTenant)
        {
            _ambientService = ambientService;
            _currentTenant = currentTenant;
        }

        [HttpGet("briefing")]
        public async Task<ActionResult<MorningBriefingDto>> GetBriefing([FromQuery] int offset = 0)
        {
            if (!_currentTenant.HasTenant) return Unauthorized();

            var briefing = await _ambientService.GetMorningBriefingAsync(_currentTenant.TenantId!.Value, offset);
            return Ok(briefing);
        }
    }
}
