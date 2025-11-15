using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using FocusDeck.Contracts.DTOs;
using FocusDeck.Persistence;
using FocusDeck.Server.Services.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FocusDeck.Server.Tests;

public class AssetIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _testStorageRoot;

    public AssetIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _testStorageRoot = Path.Combine(Path.GetTempPath(), $"focusdeck_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testStorageRoot);

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Set environment to Development for tests
                context.HostingEnvironment.EnvironmentName = "Development";
                
                // Override configuration for tests
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Storage:Root"] = _testStorageRoot,
                    ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:",
                    ["Jwt:Key"] = "test-key-for-testing-purposes-min-32-chars-long",
                    ["Jwt:Issuer"] = "test-issuer",
                    ["Jwt:Audience"] = "test-audience",
                    ["Cors:AllowedOrigins:0"] = "http://localhost:5173"
                });
            });
        });
    }

    [Fact]
    public async Task UploadAsset_ValidFile_ReturnsCreated()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("Test file content"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        content.Add(fileContent, "file", "test.txt");
        content.Add(new StringContent("Test description"), "description");

        // Act
        var response = await client.PostAsync("/v1/uploads/asset", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<AssetUploadResponse>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.Id);
        Assert.Equal("test.txt", result.FileName);
        Assert.True(result.SizeInBytes > 0);
        Assert.NotEmpty(result.Url);
    }

    [Fact]
    public async Task UploadAsset_NoFile_ReturnsBadRequest()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        var content = new MultipartFormDataContent();

        // Act
        var response = await client.PostAsync("/v1/uploads/asset", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadAsset_FileTooLarge_ReturnsPayloadTooLarge()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        var content = new MultipartFormDataContent();
        
        // Create a 6MB file (exceeds 5MB limit)
        var largeContent = new byte[6 * 1024 * 1024];
        var fileContent = new ByteArrayContent(largeContent);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        content.Add(fileContent, "file", "large.bin");

        // Act
        var response = await client.PostAsync("/v1/uploads/asset", content);

        // Assert
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task UploadAndDownloadAsset_RoundTrip_Success()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        var originalContent = "This is test file content for round-trip";
        var uploadContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(originalContent));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        uploadContent.Add(fileContent, "file", "roundtrip.txt");

        // Act - Upload
        var uploadResponse = await client.PostAsync("/v1/uploads/asset", uploadContent);
        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);
        
        var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<AssetUploadResponse>();
        Assert.NotNull(uploadResult);

        // Act - Download
        var downloadResponse = await client.GetAsync($"/v1/assets/{uploadResult.Id}");
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
        Assert.Equal("text/plain", downloadResponse.Content.Headers.ContentType?.MediaType);
        
        var downloadedContent = await downloadResponse.Content.ReadAsStringAsync();
        Assert.Equal(originalContent, downloadedContent);
    }

    [Fact]
    public async Task GetAsset_NonExistent_ReturnsNotFound()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        var nonExistentId = Guid.NewGuid().ToString();

        // Act
        var response = await client.GetAsync($"/v1/assets/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAsset_Existing_ReturnsNoContent()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        
        // Upload a file first
        var uploadContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("File to delete"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        uploadContent.Add(fileContent, "file", "delete-me.txt");
        
        var uploadResponse = await client.PostAsync("/v1/uploads/asset", uploadContent);
        var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<AssetUploadResponse>();
        Assert.NotNull(uploadResult);

        // Act - Delete
        var deleteResponse = await client.DeleteAsync($"/v1/assets/{uploadResult.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        
        // Verify file is gone
        var getResponse = await client.GetAsync($"/v1/assets/{uploadResult.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetAssetMetadata_Existing_ReturnsMetadata()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        
        // Upload a file first
        var uploadContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("Metadata test"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
        uploadContent.Add(fileContent, "file", "metadata-test.json");
        uploadContent.Add(new StringContent("Test metadata description"), "description");
        
        var uploadResponse = await client.PostAsync("/v1/uploads/asset", uploadContent);
        var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<AssetUploadResponse>();
        Assert.NotNull(uploadResult);

        // Act
        var metadataResponse = await client.GetAsync($"/v1/assets/{uploadResult.Id}/metadata");

        // Assert
        Assert.Equal(HttpStatusCode.OK, metadataResponse.StatusCode);
        
        var metadata = await metadataResponse.Content.ReadFromJsonAsync<AssetDto>();
        Assert.NotNull(metadata);
        Assert.Equal(uploadResult.Id, metadata.Id);
        Assert.Equal("metadata-test.json", metadata.FileName);
        Assert.Equal("application/json", metadata.ContentType);
        Assert.Equal("Test metadata description", metadata.Description);
        Assert.True(metadata.SizeInBytes > 0);
    }

    [Theory]
    [InlineData("text/plain", ".txt")]
    [InlineData("image/png", ".png")]
    [InlineData("application/pdf", ".pdf")]
    [InlineData("application/json", ".json")]
    public async Task UploadAsset_DifferentContentTypes_Success(string contentType, string extension)
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("Test content"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        content.Add(fileContent, "file", $"test{extension}");

        // Act
        var response = await client.PostAsync("/v1/uploads/asset", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<AssetUploadResponse>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.Id);
    }

    private HttpClient CreateAuthenticatedClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwtToken());
        return client;
    }

    private string CreateJwtToken()
    {
        var config = _factory.Services.GetRequiredService<IConfiguration>();
        var key = config.GetValue<string>("Jwt:Key") ?? "test-key-for-testing-purposes-min-32-chars-long";
        var issuer = config.GetValue<string>("Jwt:Issuer") ?? "test-issuer";
        var audience = config.GetValue<string>("Jwt:Audience") ?? "test-audience";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user"),
            new Claim("app_tenant_id", Guid.NewGuid().ToString())
        };

        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience, claims, expires: DateTime.UtcNow.AddHours(1), signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public void Dispose()
    {
        // Clean up test storage directory
        if (Directory.Exists(_testStorageRoot))
        {
            Directory.Delete(_testStorageRoot, recursive: true);
        }
    }
}
