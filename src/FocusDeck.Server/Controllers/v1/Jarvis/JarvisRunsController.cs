using System;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using FocusDeck.Contracts.DTOs.Jarvis;
using FocusDeck.Services.Jarvis;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FocusDeck.Server.Controllers.v1.Jarvis
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/jarvis/runs")]
    [Authorize]
    public class JarvisRunsController : ControllerBase
    {
        private readonly IJarvisRunService _runService;
        private readonly IBackgroundJobClient _jobClient;

        public JarvisRunsController(IJarvisRunService runService, IBackgroundJobClient jobClient)
        {
            _runService = runService;
            _jobClient = jobClient;
        }

        [HttpPost]
        public Task<ActionResult<JarvisRunDto>> CreateRun([FromBody] CreateJarvisRunRequestDto request, CancellationToken ct)
        {
            // TODO:
            // - Get userId from claims
            // - Call StartRunAsync
            // - Enqueue IJarvisRunJob.ExecuteAsync(run.Id)
            // - Return 202 or 201 with JarvisRunDto
            throw new NotImplementedException();
        }

        [HttpGet("{id:guid}")]
        public Task<ActionResult<JarvisRunDetailsDto>> GetRun(Guid id, CancellationToken ct)
        {
            // TODO: load run + steps, map to DTO
            throw new NotImplementedException();
        }
    }
}
