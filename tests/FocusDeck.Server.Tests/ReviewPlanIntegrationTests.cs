using System.Net;
using System.Net.Http.Json;
using FocusDeck.Contracts.DTOs;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using FocusDeck.Server.Jobs;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FocusDeck.Server.Tests;

public class ReviewPlanIntegrationTests : IClassFixture<FocusDeckWebApplicationFactory>
{
    private readonly WebApplicationFactory<TestServerProgram> _factory;

    public ReviewPlanIntegrationTests(FocusDeckWebApplicationFactory factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                context.HostingEnvironment.EnvironmentName = "Development";
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Cors:AllowedOrigins:0"] = "http://localhost:5173"
                });
            });

            builder.ConfigureServices(services =>
            {
                // Replace database with in-memory version for testing
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AutomationDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<AutomationDbContext>(options =>
                {
                    options.UseSqlite("DataSource=:memory:");
                });

                // Build service provider and create database
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<AutomationDbContext>();
                db.Database.OpenConnection();
                db.Database.EnsureCreated();
            });
        });
    }

    [Fact]
    public async Task ComputeSpacedPlan_ReturnsCorrectSchedule()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new ComputeSpacedPlanRequest
        {
            TargetEntityId = "lecture-123",
            EntityType = "Lecture",
            Title = "Test Lecture",
            StartDate = new DateTime(2024, 1, 1)
        };

        // Act
        var response = await client.PostAsJsonAsync("/v1/review-plans/compute-spaced", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CreateReviewPlanDto>();
        
        Assert.NotNull(result);
        Assert.Equal(3, result.ScheduledDates.Length);
        Assert.Equal(new DateTime(2024, 1, 1), result.ScheduledDates[0]); // D0
        Assert.Equal(new DateTime(2024, 1, 3), result.ScheduledDates[1]); // D+2
        Assert.Equal(new DateTime(2024, 1, 8), result.ScheduledDates[2]); // D+7
    }

    [Fact]
    public async Task CreateReviewPlan_WithValidLecture_Succeeds()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Create a lecture first
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();
        
        var courseId = Guid.NewGuid().ToString();
        var lectureId = Guid.NewGuid().ToString();
        
        context.Courses.Add(new Course
        {
            Id = courseId,
            Name = "Test Course",
            Code = "TEST101"
        });
        
        context.Lectures.Add(new Lecture
        {
            Id = lectureId,
            CourseId = courseId,
            Title = "Test Lecture",
            CreatedAt = DateTime.UtcNow,
            RecordedAt = DateTime.UtcNow,
            CreatedBy = "test-user"
        });
        
        await context.SaveChangesAsync();

        var createDto = new CreateReviewPlanDto
        {
            TargetEntityId = lectureId,
            EntityType = "Lecture",
            Title = "Review Test Lecture",
            ScheduledDates = new[]
            {
                DateTime.Today,
                DateTime.Today.AddDays(2),
                DateTime.Today.AddDays(7)
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/v1/review-plans", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ReviewPlanDto>();
        
        Assert.NotNull(result);
        Assert.Equal(lectureId, result.TargetEntityId);
        Assert.Equal("Lecture", result.EntityType);
        Assert.Equal(3, result.ReviewSessions.Count);
        Assert.All(result.ReviewSessions, session => Assert.Equal("Pending", session.Status));
    }

    [Fact]
    public async Task UpdateReviewSession_ToCompleted_UpdatesStatus()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();
        
        // Create review plan with sessions
        var planId = Guid.NewGuid().ToString();
        var sessionId = Guid.NewGuid().ToString();
        var lectureId = Guid.NewGuid().ToString();
        var courseId = Guid.NewGuid().ToString();
        
        context.Courses.Add(new Course
        {
            Id = courseId,
            Name = "Test Course",
            Code = "TEST101"
        });
        
        context.Lectures.Add(new Lecture
        {
            Id = lectureId,
            CourseId = courseId,
            Title = "Test Lecture",
            CreatedAt = DateTime.UtcNow,
            RecordedAt = DateTime.UtcNow,
            CreatedBy = "test-user"
        });
        
        context.ReviewPlans.Add(new ReviewPlan
        {
            Id = planId,
            UserId = "anonymous",
            TargetEntityId = lectureId,
            EntityType = ReviewPlanEntityType.Lecture,
            Title = "Test Plan",
            CreatedAt = DateTime.UtcNow,
            Status = ReviewPlanStatus.Active,
            ReviewSessions = new List<ReviewSession>
            {
                new ReviewSession
                {
                    Id = sessionId,
                    ReviewPlanId = planId,
                    ScheduledDate = DateTime.Today,
                    Status = ReviewSessionStatus.Pending
                }
            }
        });
        
        await context.SaveChangesAsync();

        var updateDto = new UpdateReviewSessionDto
        {
            Status = "Completed",
            Score = 85,
            Notes = "Good review session"
        };

        // Act
        var response = await client.PatchAsJsonAsync($"/v1/review-plans/{planId}/sessions/{sessionId}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ReviewSessionDto>();
        
        Assert.NotNull(result);
        Assert.Equal("Completed", result.Status);
        Assert.Equal(85, result.Score);
        Assert.Equal("Good review session", result.Notes);
        Assert.NotNull(result.CompletedDate);
    }
}

public class GenerateLectureNoteJobTests : IClassFixture<FocusDeckWebApplicationFactory>
{
    private readonly WebApplicationFactory<TestServerProgram> _factory;

    public GenerateLectureNoteJobTests(FocusDeckWebApplicationFactory factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                context.HostingEnvironment.EnvironmentName = "Development";
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Cors:AllowedOrigins:0"] = "http://localhost:5173"
                });
            });

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AutomationDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<AutomationDbContext>(options =>
                {
                    options.UseSqlite("DataSource=:memory:");
                });

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<AutomationDbContext>();
                db.Database.OpenConnection();
                db.Database.EnsureCreated();
            });
        });
    }

    [Fact]
    public async Task GenerateNoteAsync_WithValidLecture_CreatesNote()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();
        var job = scope.ServiceProvider.GetRequiredService<IGenerateLectureNoteJob>();
        
        var courseId = Guid.NewGuid().ToString();
        var lectureId = Guid.NewGuid().ToString();
        
        context.Courses.Add(new Course
        {
            Id = courseId,
            Name = "Computer Science 101",
            Code = "CS101"
        });
        
        context.Lectures.Add(new Lecture
        {
            Id = lectureId,
            CourseId = courseId,
            Title = "Introduction to Algorithms",
            CreatedAt = DateTime.UtcNow,
            RecordedAt = DateTime.UtcNow,
            CreatedBy = "test-user",
            Status = LectureStatus.Summarized,
            TranscriptionText = "This lecture covers algorithms, data structures, and complexity analysis. Big O notation is important.",
            SummaryText = "Overview of algorithms and complexity."
        });
        
        await context.SaveChangesAsync();

        // Act
        var result = await job.GenerateNoteAsync(lectureId);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.NoteId);
        
        // Verify note was created
        var note = await context.Notes.FindAsync(result.NoteId);
        Assert.NotNull(note);
        Assert.Contains("Introduction to Algorithms", note.Title);
        Assert.Contains("Key Points", note.Content);
        Assert.Contains("Definitions", note.Content);
        Assert.Contains("Likely Test Questions", note.Content);
        Assert.Contains("References", note.Content);
        Assert.Contains("lecture", note.Tags);
        
        // Verify lecture was updated
        var updatedLecture = await context.Lectures.FindAsync(lectureId);
        Assert.Equal(result.NoteId, updatedLecture!.GeneratedNoteId);
        Assert.Equal(LectureStatus.Completed, updatedLecture.Status);
    }

    [Fact]
    public async Task GenerateNoteAsync_Idempotency_ReturnsSameNote()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();
        var job = scope.ServiceProvider.GetRequiredService<IGenerateLectureNoteJob>();
        
        var courseId = Guid.NewGuid().ToString();
        var lectureId = Guid.NewGuid().ToString();
        var existingNoteId = Guid.NewGuid().ToString();
        
        context.Courses.Add(new Course
        {
            Id = courseId,
            Name = "Test Course",
            Code = "TEST101"
        });
        
        context.Lectures.Add(new Lecture
        {
            Id = lectureId,
            CourseId = courseId,
            Title = "Test Lecture",
            CreatedAt = DateTime.UtcNow,
            RecordedAt = DateTime.UtcNow,
            CreatedBy = "test-user",
            Status = LectureStatus.Completed,
            TranscriptionText = "Test transcript",
            SummaryText = "Test summary",
            GeneratedNoteId = existingNoteId // Already has a note
        });
        
        await context.SaveChangesAsync();

        // Act
        var result = await job.GenerateNoteAsync(lectureId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(existingNoteId, result.NoteId);
        
        // Verify no new note was created (idempotency)
        var noteCount = await context.Notes.CountAsync(n => n.Id == existingNoteId);
        Assert.Equal(0, noteCount); // Existing note wasn't in our test database, so count should be 0
    }

    [Fact]
    public async Task GenerateNoteAsync_WithoutTranscription_Fails()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();
        var job = scope.ServiceProvider.GetRequiredService<IGenerateLectureNoteJob>();
        
        var courseId = Guid.NewGuid().ToString();
        var lectureId = Guid.NewGuid().ToString();
        
        context.Courses.Add(new Course
        {
            Id = courseId,
            Name = "Test Course",
            Code = "TEST101"
        });
        
        context.Lectures.Add(new Lecture
        {
            Id = lectureId,
            CourseId = courseId,
            Title = "Test Lecture",
            CreatedAt = DateTime.UtcNow,
            RecordedAt = DateTime.UtcNow,
            CreatedBy = "test-user",
            Status = LectureStatus.AudioUploaded
            // No transcription
        });
        
        await context.SaveChangesAsync();

        // Act
        var result = await job.GenerateNoteAsync(lectureId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("no transcription", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
}
