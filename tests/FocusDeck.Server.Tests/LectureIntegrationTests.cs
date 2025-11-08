using FocusDeck.Contracts.DTOs;
using FocusDeck.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace FocusDeck.Server.Tests;

public class LectureIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public LectureIntegrationTests(WebApplicationFactory<Program> factory)
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
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateLecture_ReturnsCreated()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // First create a course
        var courseDto = new CreateCourseDto(
            Name: "Computer Science 101",
            Code: "CS101",
            Description: "Intro to CS",
            Instructor: "Dr. Smith"
        );

        var courseResponse = await _client.PostAsJsonAsync("/v1/courses", courseDto);
        courseResponse.EnsureSuccessStatusCode();
        var course = await courseResponse.Content.ReadFromJsonAsync<CourseDto>();

        var lectureDto = new CreateLectureDto(
            CourseId: course!.Id,
            Title: "Introduction to Programming",
            Description: "First lecture covering basics",
            RecordedAt: DateTime.UtcNow
        );

        // Act
        var response = await _client.PostAsJsonAsync("/v1/lectures", lectureDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var lecture = await response.Content.ReadFromJsonAsync<LectureDto>();
        Assert.NotNull(lecture);
        Assert.Equal(lectureDto.Title, lecture.Title);
        Assert.Equal(course.Id, lecture.CourseId);
        Assert.Equal("Created", lecture.Status);
    }

    [Fact]
    public async Task UploadLectureAudio_WithWavFile_ReturnsSuccess()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create course and lecture
        var course = await CreateTestCourseAsync();
        var lecture = await CreateTestLectureAsync(course.Id);

        // Create a tiny WAV file for testing (44.1kHz mono 16-bit PCM)
        var wavData = GenerateTinyWavFile();
        
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(wavData);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        content.Add(fileContent, "audio", "test-lecture.wav");

        // Act
        var response = await _client.PostAsync($"/v1/lectures/{lecture.Id}/audio", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<UploadLectureAudioResponse>();
        Assert.NotNull(result);
        Assert.Equal(lecture.Id, result.LectureId);
        Assert.NotNull(result.AudioAssetId);
        Assert.Equal("AudioUploaded", result.Status);
    }

    [Fact]
    public async Task UploadLectureAudio_RoundTrip_SuccessfullyUploadsAndDownloads()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var course = await CreateTestCourseAsync();
        var lecture = await CreateTestLectureAsync(course.Id);
        var wavData = GenerateTinyWavFile();
        
        using var uploadContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(wavData);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        uploadContent.Add(fileContent, "audio", "test.wav");

        // Act - Upload
        var uploadResponse = await _client.PostAsync($"/v1/lectures/{lecture.Id}/audio", uploadContent);
        uploadResponse.EnsureSuccessStatusCode();
        var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<UploadLectureAudioResponse>();

        // Act - Download
        var downloadResponse = await _client.GetAsync($"/v1/assets/{uploadResult!.AudioAssetId}");
        downloadResponse.EnsureSuccessStatusCode();
        var downloadedData = await downloadResponse.Content.ReadAsByteArrayAsync();

        // Assert
        Assert.Equal(wavData.Length, downloadedData.Length);
        Assert.Equal(wavData, downloadedData);
    }

    [Fact]
    public async Task GetLecture_ReturnsLectureDetails()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var course = await CreateTestCourseAsync();
        var lecture = await CreateTestLectureAsync(course.Id);

        // Act
        var response = await _client.GetAsync($"/v1/lectures/{lecture.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LectureDto>();
        Assert.NotNull(result);
        Assert.Equal(lecture.Id, result.Id);
        Assert.Equal(lecture.Title, result.Title);
    }

    private async Task<string> GetAuthTokenAsync()
    {
        // Mock auth - in real tests, implement proper authentication
        return "test-token";
    }

    private async Task<CourseDto> CreateTestCourseAsync()
    {
        var courseDto = new CreateCourseDto(
            Name: "Test Course",
            Code: "TEST101",
            Description: "Test Description",
            Instructor: "Test Instructor"
        );

        var response = await _client.PostAsJsonAsync("/v1/courses", courseDto);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CourseDto>())!;
    }

    private async Task<LectureDto> CreateTestLectureAsync(string courseId)
    {
        var lectureDto = new CreateLectureDto(
            CourseId: courseId,
            Title: "Test Lecture",
            Description: "Test Description",
            RecordedAt: DateTime.UtcNow
        );

        var response = await _client.PostAsJsonAsync("/v1/lectures", lectureDto);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LectureDto>())!;
    }

    private static byte[] GenerateTinyWavFile()
    {
        // Generate a minimal valid WAV file (44.1kHz, mono, 16-bit PCM)
        // WAV file structure: RIFF header (12 bytes) + fmt chunk (24 bytes) + data chunk (8 bytes + audio data)
        
        const int sampleRate = 44100;
        const short numChannels = 1;
        const short bitsPerSample = 16;
        const int durationMs = 100; // 100ms of audio
        
        var numSamples = sampleRate * durationMs / 1000;
        var audioDataSize = numSamples * numChannels * (bitsPerSample / 8);
        
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        
        // RIFF header
        writer.Write(new[] { 'R', 'I', 'F', 'F' });
        writer.Write(36 + audioDataSize); // File size - 8
        writer.Write(new[] { 'W', 'A', 'V', 'E' });
        
        // fmt chunk
        writer.Write(new[] { 'f', 'm', 't', ' ' });
        writer.Write(16); // fmt chunk size
        writer.Write((short)1); // PCM format
        writer.Write(numChannels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * numChannels * (bitsPerSample / 8)); // Byte rate
        writer.Write((short)(numChannels * (bitsPerSample / 8))); // Block align
        writer.Write(bitsPerSample);
        
        // data chunk
        writer.Write(new[] { 'd', 'a', 't', 'a' });
        writer.Write(audioDataSize);
        
        // Generate simple sine wave for audio data
        for (int i = 0; i < numSamples; i++)
        {
            var sample = (short)(Math.Sin(2.0 * Math.PI * 440.0 * i / sampleRate) * 16000);
            writer.Write(sample);
        }
        
        return ms.ToArray();
    }

    [Fact]
    public async Task ProcessLecture_WithAudio_StartsTranscription()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create course, lecture, and upload audio
        var course = await CreateTestCourseAsync();
        var lecture = await CreateTestLectureAsync(course.Id);
        
        var wavData = GenerateTinyWavFile();
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(wavData);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        content.Add(fileContent, "audio", "test-lecture.wav");
        
        var uploadResponse = await _client.PostAsync($"/v1/lectures/{lecture.Id}/audio", content);
        uploadResponse.EnsureSuccessStatusCode();

        // Act
        var processResponse = await _client.PostAsync($"/v1/lectures/{lecture.Id}/process", null);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, processResponse.StatusCode);
        var result = await processResponse.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ProcessLecture_WithoutAudio_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create course and lecture without audio
        var course = await CreateTestCourseAsync();
        var lecture = await CreateTestLectureAsync(course.Id);

        // Act
        var processResponse = await _client.PostAsync($"/v1/lectures/{lecture.Id}/process", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, processResponse.StatusCode);
    }

    [Fact]
    public async Task TranscriptionJob_ProducesNonEmptyTranscript()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create course, lecture, and upload audio
        var course = await CreateTestCourseAsync();
        var lecture = await CreateTestLectureAsync(course.Id);
        
        var wavData = GenerateTinyWavFile();
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(wavData);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        content.Add(fileContent, "audio", "test-lecture.wav");
        
        var uploadResponse = await _client.PostAsync($"/v1/lectures/{lecture.Id}/audio", content);
        uploadResponse.EnsureSuccessStatusCode();

        // Act - trigger processing
        var processResponse = await _client.PostAsync($"/v1/lectures/{lecture.Id}/process", null);
        processResponse.EnsureSuccessStatusCode();

        // Wait for processing (stub implementation should be fast)
        await Task.Delay(1000);

        // Assert - check lecture has transcription
        var lectureResponse = await _client.GetAsync($"/v1/lectures/{lecture.Id}");
        lectureResponse.EnsureSuccessStatusCode();
        var updatedLecture = await lectureResponse.Content.ReadFromJsonAsync<LectureDto>();
        
        Assert.NotNull(updatedLecture);
        Assert.NotNull(updatedLecture.TranscriptionText);
        Assert.NotEmpty(updatedLecture.TranscriptionText);
        Assert.True(updatedLecture.TranscriptionText.Length > 10, "Transcription should contain meaningful text");
    }

    [Fact]
    public async Task LectureProcessing_ChainsToSummarization()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create course, lecture, and upload audio
        var course = await CreateTestCourseAsync();
        var lecture = await CreateTestLectureAsync(course.Id);
        
        var wavData = GenerateTinyWavFile();
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(wavData);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        content.Add(fileContent, "audio", "test-lecture.wav");
        
        var uploadResponse = await _client.PostAsync($"/v1/lectures/{lecture.Id}/audio", content);
        uploadResponse.EnsureSuccessStatusCode();

        // Act - trigger processing
        var processResponse = await _client.PostAsync($"/v1/lectures/{lecture.Id}/process", null);
        processResponse.EnsureSuccessStatusCode();

        // Wait for both transcription and summarization (stub implementations should be fast)
        await Task.Delay(2000);

        // Assert - check lecture has both transcription and summary
        var lectureResponse = await _client.GetAsync($"/v1/lectures/{lecture.Id}");
        lectureResponse.EnsureSuccessStatusCode();
        var updatedLecture = await lectureResponse.Content.ReadFromJsonAsync<LectureDto>();
        
        Assert.NotNull(updatedLecture);
        Assert.NotNull(updatedLecture.TranscriptionText);
        Assert.NotEmpty(updatedLecture.TranscriptionText);
        Assert.NotNull(updatedLecture.SummaryText);
        Assert.NotEmpty(updatedLecture.SummaryText);
        Assert.Equal("Summarized", updatedLecture.Status);
    }
}
