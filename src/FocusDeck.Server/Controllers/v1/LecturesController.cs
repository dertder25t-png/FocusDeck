using Asp.Versioning;
using FocusDeck.Contracts.DTOs;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using FocusDeck.Server.Jobs;
using FocusDeck.Server.Services.Storage;
using FocusDeck.Server.Services.Writing;
using FocusDeck.SharedKernel;
using FocusDeck.SharedKernel.Tenancy;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FocusDeck.Server.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/lectures")]
[Authorize]
public class LecturesController : ControllerBase
{
    private readonly AutomationDbContext _context;
    private readonly IAssetStorage _assetStorage;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly LectureSynthesisService _synthesisService;
    private readonly ICurrentTenant _currentTenant;
    private readonly ILogger<LecturesController> _logger;

    public LecturesController(
        AutomationDbContext context,
        IAssetStorage assetStorage,
        IIdGenerator idGenerator,
        IClock clock,
        IBackgroundJobClient backgroundJobClient,
        LectureSynthesisService synthesisService,
        ICurrentTenant currentTenant,
        ILogger<LecturesController> logger)
    {
        _context = context;
        _assetStorage = assetStorage;
        _idGenerator = idGenerator;
        _clock = clock;
        _backgroundJobClient = backgroundJobClient;
        _synthesisService = synthesisService;
        _currentTenant = currentTenant;
        _logger = logger;
    }

    /// <summary>
    /// Create a new lecture
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(LectureDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LectureDto>> CreateLecture([FromBody] CreateLectureDto dto)
    {
        // Verify course exists
        var courseExists = await _context.Courses.AnyAsync(c => c.Id == dto.CourseId);
        if (!courseExists)
        {
            return BadRequest(new { message = "Course not found" });
        }

        var lecture = new Lecture
        {
            Id = _idGenerator.NewId().ToString(),
            CourseId = dto.CourseId,
            Title = dto.Title,
            Description = dto.Description,
            RecordedAt = dto.RecordedAt,
            CreatedAt = _clock.UtcNow,
            CreatedBy = User.Identity?.Name ?? "system",
            Status = LectureStatus.Created
        };

        _context.Lectures.Add(lecture);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created lecture {LectureId} for course {CourseId}", lecture.Id, lecture.CourseId);

        var lectureDto = MapToDto(lecture);
        return CreatedAtAction(nameof(GetLecture), new { id = lecture.Id }, lectureDto);
    }

    /// <summary>
    /// Upload audio file for a lecture
    /// </summary>
    [HttpPost("{id}/audio")]
    [RequestSizeLimit(52428800)] // 50MB limit for audio files
    [ProducesResponseType(typeof(UploadLectureAudioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UploadLectureAudioResponse>> UploadAudio(string id, [FromForm] IFormFile audio)
    {
        var lecture = await _context.Lectures.FindAsync(id);
        if (lecture == null)
        {
            return NotFound(new { message = "Lecture not found" });
        }

        if (audio == null || audio.Length == 0)
        {
            return BadRequest(new { message = "No audio file provided" });
        }

        // Validate audio content type
        var allowedTypes = new[] { "audio/wav", "audio/mpeg", "audio/mp3", "audio/mp4", "audio/x-m4a" };
        if (!allowedTypes.Contains(audio.ContentType.ToLower()))
        {
            return BadRequest(new { message = $"Invalid audio type. Allowed: {string.Join(", ", allowedTypes)}" });
        }

        try
        {
            // Store file
            using var stream = audio.OpenReadStream();
            var (generatedAssetId, storagePath) = await _assetStorage.StoreAsync(stream, audio.FileName, audio.ContentType);
            
            // Create asset record
            var asset = new Asset
            {
                Id = generatedAssetId,
                FileName = audio.FileName,
                ContentType = audio.ContentType,
                SizeInBytes = audio.Length,
                StoragePath = storagePath,
                UploadedAt = _clock.UtcNow,
                UploadedBy = User.Identity?.Name ?? "system",
                Description = $"Audio for lecture: {lecture.Title}"
            };

            _context.Assets.Add(asset);

            // Update lecture
            lecture.AudioAssetId = generatedAssetId;
            lecture.Status = LectureStatus.AudioUploaded;
            lecture.UpdatedAt = _clock.UtcNow;

            // Calculate duration if possible (for WAV files, we can estimate)
            if (audio.ContentType == "audio/wav" && audio.Length > 44)
            {
                lecture.DurationSeconds = EstimateWavDuration(audio.Length);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Uploaded audio asset {AssetId} for lecture {LectureId}", generatedAssetId, id);

            // TODO: Queue transcription job here
            // await _backgroundJobClient.Enqueue<ITranscribeLectureJob>(x => x.TranscribeAsync(id));

            return Ok(new UploadLectureAudioResponse(
                LectureId: id,
                AudioAssetId: generatedAssetId,
                Status: lecture.Status.ToString()
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading audio for lecture {LectureId}", id);
            return StatusCode(500, new { message = "Error uploading audio file" });
        }
    }

    /// <summary>
    /// Get lecture details
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(LectureDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LectureDto>> GetLecture(string id)
    {
        var lecture = await _context.Lectures
            .Include(l => l.Course)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lecture == null)
        {
            return NotFound(new { message = "Lecture not found" });
        }

        return Ok(MapToDto(lecture));
    }

    /// <summary>
    /// Get all lectures for a course
    /// </summary>
    [HttpGet("course/{courseId}")]
    [ProducesResponseType(typeof(List<LectureDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<LectureDto>>> GetLecturesByCourse(string courseId)
    {
        var lectures = await _context.Lectures
            .Where(l => l.CourseId == courseId)
            .OrderByDescending(l => l.RecordedAt)
            .ToListAsync();

        return Ok(lectures.Select(MapToDto).ToList());
    }

    /// <summary>
    /// Process lecture (transcribe and summarize)
    /// </summary>
    [HttpPost("{id}/process")]
    [ProducesResponseType(typeof(object), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessLecture(string id)
    {
        var lecture = await _context.Lectures.FindAsync(id);
        if (lecture == null)
        {
            return NotFound(new { message = "Lecture not found" });
        }

        if (string.IsNullOrEmpty(lecture.AudioAssetId))
        {
            return BadRequest(new { message = "Lecture does not have an audio file" });
        }

        if (lecture.Status == LectureStatus.Transcribing || lecture.Status == LectureStatus.Summarizing)
        {
            return BadRequest(new { message = $"Lecture is already being processed (status: {lecture.Status})" });
        }

        _logger.LogInformation("Enqueueing transcription job for lecture {LectureId}", id);

        // Enqueue transcription job
        var jobId = _backgroundJobClient.Enqueue<ITranscribeLectureJob>(
            x => x.TranscribeAsync(id, $"/assets/{lecture.AudioAssetId}", "en"));

        return Accepted(new
        {
            message = "Lecture processing started",
            lectureId = id,
            jobId = jobId,
            status = "Queued for transcription"
        });
    }

    /// <summary>
    /// Synthesize a note from a lecture transcript
    /// </summary>
    [HttpPost("synthesize")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SynthesizeLectureNote([FromBody] SynthesizeLectureDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Transcript))
        {
            return BadRequest(new { message = "Transcript cannot be empty" });
        }

        var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? throw new UnauthorizedAccessException());
        var tenantId = _currentTenant.TenantId ?? throw new InvalidOperationException("Tenant context required");

        var note = await _synthesisService.SynthesizeNoteAsync(
            dto.Transcript,
            dto.CourseId,
            dto.EventId,
            dto.Timestamp,
            userId,
            tenantId
        );

        return Created($"/v1/notes/{note.Id}", new { noteId = note.Id, title = note.Title });
    }

    private static LectureDto MapToDto(Lecture lecture)
    {
        return new LectureDto(
            Id: lecture.Id,
            CourseId: lecture.CourseId,
            Title: lecture.Title,
            Description: lecture.Description,
            RecordedAt: lecture.RecordedAt,
            CreatedAt: lecture.CreatedAt,
            CreatedBy: lecture.CreatedBy,
            AudioAssetId: lecture.AudioAssetId,
            Status: lecture.Status.ToString(),
            TranscriptionText: lecture.TranscriptionText,
            SummaryText: lecture.SummaryText,
            GeneratedNoteId: lecture.GeneratedNoteId,
            DurationSeconds: lecture.DurationSeconds
        );
    }

    private static int EstimateWavDuration(long fileSize)
    {
        // For 44.1kHz mono 16-bit PCM WAV: 88200 bytes per second (44100 samples * 2 bytes)
        // Subtract 44 bytes for WAV header
        const int bytesPerSecond = 88200;
        var audioBytes = fileSize - 44;
        return (int)(audioBytes / bytesPerSecond);
    }
}
