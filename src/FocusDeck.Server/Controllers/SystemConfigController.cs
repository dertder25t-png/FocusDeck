using System;
using System.Linq;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FocusDeck.SharedKernel.Tenancy;

namespace FocusDeck.Server.Controllers
{
    [ApiController]
    [Route("v1/system/config")]
    [Authorize] // Require auth, maybe restricting to Admin later
    public class SystemConfigController : ControllerBase
    {
        private readonly AutomationDbContext _dbContext;
        private readonly ICurrentTenant _currentTenant;

        public SystemConfigController(AutomationDbContext dbContext, ICurrentTenant currentTenant)
        {
            _dbContext = dbContext;
            _currentTenant = currentTenant;
        }

        [HttpGet("gemini")]
        public async Task<IActionResult> GetGeminiConfig()
        {
            var config = await _dbContext.ServiceConfigurations
                .FirstOrDefaultAsync(c => c.ServiceName == "Gemini");

            if (config == null)
            {
                return Ok(new { hasKey = false });
            }

            return Ok(new { hasKey = !string.IsNullOrEmpty(config.ApiKey) });
        }

        [HttpPost("gemini")]
        public async Task<IActionResult> SetGeminiConfig([FromBody] GeminiConfigDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ApiKey))
            {
                return BadRequest("API Key is required.");
            }

            var config = await _dbContext.ServiceConfigurations
                .FirstOrDefaultAsync(c => c.ServiceName == "Gemini");

            if (config == null)
            {
                config = new ServiceConfiguration
                {
                    Id = Guid.NewGuid(),
                    ServiceName = "Gemini",
                    ApiKey = dto.ApiKey,
                    TenantId = _currentTenant.TenantId ?? Guid.Empty
                };
                _dbContext.ServiceConfigurations.Add(config);
            }
            else
            {
                config.ApiKey = dto.ApiKey;
                config.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();

            return Ok(new { success = true });
        }

        public class GeminiConfigDto
        {
            public string ApiKey { get; set; } = string.Empty;
        }
    }
}
