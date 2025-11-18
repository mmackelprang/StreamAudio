using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using StreamAudio.Api.Controllers;

namespace StreamAudio.Tests;

/// <summary>
/// Integration tests for the Streams API endpoints.
/// These tests use the WebApplicationFactory to test the full HTTP stack.
/// </summary>
[Collection("ApiTests")]
public class StreamsApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
  private readonly WebApplicationFactory<Program> _factory;
  private readonly HttpClient _client;
  private const string TestDataPath = "../../../../../testdata";
  private static readonly SemaphoreSlim ApiTestLock = new(1, 1);

  public StreamsApiIntegrationTests(WebApplicationFactory<Program> factory)
  {
    // Wait for lock to ensure sequential execution
    ApiTestLock.Wait();
    
    _factory = factory;
    _client = factory.CreateClient();
    
    // Ensure clean state
    try
    {
      _client.PostAsync("/api/streams/shutdown", null).Wait();
    }
    catch
    {
      // Ignore if not initialized
    }
    
    // Reset the AudioEngineManager to allow recreation of the engine
    StreamAudio.Core.AudioEngineManager.Reset();
  }

  [Fact]
  public async Task GetStatus_WhenNotInitialized_ReturnsNotInitialized()
  {
    // Arrange & Act
    var response = await _client.GetAsync("/api/streams/status");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var content = await response.Content.ReadAsStringAsync();
    content.Should().Contain("\"initialized\":false");
  }

  [Fact]
  public async Task Initialize_WithDefaultSettings_ReturnsSuccess()
  {
    // Skip if headless
    if (IsHeadlessEnvironment())
      return;

    // Arrange & Act
    var response = await _client.PostAsync("/api/streams/initialize", null);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var content = await response.Content.ReadAsStringAsync();
    content.Should().Contain("\"success\":true");

    // Cleanup
    await _client.PostAsync("/api/streams/shutdown", null);
  }

  [Fact]
  public async Task Initialize_WithCustomSettings_ReturnsSuccess()
  {
    // Skip if headless
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    var request = new
    {
      BackgroundVolume = 0.5f,
      MaxStreamDuration = 60
    };

    // Act
    var response = await _client.PostAsJsonAsync("/api/streams/initialize", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    // Verify settings were applied
    var statusResponse = await _client.GetAsync("/api/streams/status");
    var statusContent = await statusResponse.Content.ReadAsStringAsync();
    statusContent.Should().Contain("\"backgroundVolume\":0.5");
    statusContent.Should().Contain("\"maxStreamDuration\":60");

    // Cleanup
    await _client.PostAsync("/api/streams/shutdown", null);
  }

  [Fact]
  public async Task Initialize_WhenAlreadyInitialized_ReturnsBadRequest()
  {
    // Arrange
    await _client.PostAsync("/api/streams/initialize", null);

    // Act
    var response = await _client.PostAsync("/api/streams/initialize", null);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    // Cleanup
    await _client.PostAsync("/api/streams/shutdown", null);
  }

  [Fact]
  public async Task Shutdown_WhenInitialized_ReturnsSuccess()
  {
    // Skip if headless
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    await _client.PostAsync("/api/streams/initialize", null);

    // Act
    var response = await _client.PostAsync("/api/streams/shutdown", null);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var content = await response.Content.ReadAsStringAsync();
    content.Should().Contain("\"success\":true");
  }

  [Fact]
  public async Task Shutdown_WhenNotInitialized_ReturnsBadRequest()
  {
    // Act
    var response = await _client.PostAsync("/api/streams/shutdown", null);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }

  [Fact]
  public async Task AddFileSource_WithValidFile_ReturnsSuccess()
  {
    // Skip if headless
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    await _client.PostAsync("/api/streams/initialize", null);
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    
    var request = new
    {
      StreamId = "test-stream",
      FilePath = testFile,
      IsPrimary = true,
      AutoPlay = false
    };

    // Act
    var response = await _client.PostAsJsonAsync("/api/streams/sources/file", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var content = await response.Content.ReadAsStringAsync();
    content.Should().Contain("\"success\":true");

    // Cleanup
    await _client.DeleteAsync("/api/streams/sources/test-stream");
    await _client.PostAsync("/api/streams/shutdown", null);
  }

  [Fact]
  public async Task AddFileSource_WhenNotInitialized_ReturnsBadRequest()
  {
    // Arrange
    var request = new
    {
      StreamId = "test-stream",
      FilePath = "test.mp3",
      IsPrimary = false
    };

    // Act
    var response = await _client.PostAsJsonAsync("/api/streams/sources/file", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }

  [Fact]
  public async Task RemoveSource_WithExistingStream_ReturnsSuccess()
  {
    // Skip if headless
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    await _client.PostAsync("/api/streams/initialize", null);
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    
    var request = new
    {
      StreamId = "test-stream",
      FilePath = testFile,
      IsPrimary = false,
      AutoPlay = false
    };
    await _client.PostAsJsonAsync("/api/streams/sources/file", request);

    // Act
    var response = await _client.DeleteAsync("/api/streams/sources/test-stream?fadeOut=false");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    // Cleanup
    await _client.PostAsync("/api/streams/shutdown", null);
  }

  [Fact]
  public async Task SetPrimaryStream_WithExistingStream_ReturnsSuccess()
  {
    // Skip if headless
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    await _client.PostAsync("/api/streams/initialize", null);
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    
    var request = new
    {
      StreamId = "test-stream",
      FilePath = testFile,
      IsPrimary = false,
      AutoPlay = false
    };
    await _client.PostAsJsonAsync("/api/streams/sources/file", request);

    // Act
    var response = await _client.PostAsync("/api/streams/primary/test-stream", null);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    // Cleanup
    await _client.DeleteAsync("/api/streams/sources/test-stream?fadeOut=false");
    await _client.PostAsync("/api/streams/shutdown", null);
  }

  [Fact]
  public async Task ClearPrimaryStream_WhenInitialized_ReturnsSuccess()
  {
    // Skip if headless
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    await _client.PostAsync("/api/streams/initialize", null);

    // Act
    var response = await _client.DeleteAsync("/api/streams/primary");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    // Cleanup
    await _client.PostAsync("/api/streams/shutdown", null);
  }

  [Fact]
  public async Task SetBackgroundVolume_WithValidValue_ReturnsSuccess()
  {
    // Skip if headless
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    await _client.PostAsync("/api/streams/initialize", null);
    var request = new { Volume = 0.7f };

    // Act
    var response = await _client.PostAsJsonAsync("/api/streams/background-volume", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    // Verify
    var getResponse = await _client.GetAsync("/api/streams/background-volume");
    var content = await getResponse.Content.ReadAsStringAsync();
    content.Should().Contain("\"volume\":0.7");

    // Cleanup
    await _client.PostAsync("/api/streams/shutdown", null);
  }

  [Fact]
  public async Task GetBackgroundVolume_WhenInitialized_ReturnsVolume()
  {
    // Skip if headless
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    await _client.PostAsync("/api/streams/initialize", null);

    // Act
    var response = await _client.GetAsync("/api/streams/background-volume");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var content = await response.Content.ReadAsStringAsync();
    content.Should().Contain("\"volume\":");

    // Cleanup
    await _client.PostAsync("/api/streams/shutdown", null);
  }

  [Fact]
  public async Task PlayStream_WithExistingStream_ReturnsSuccess()
  {
    // Skip if headless
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    await _client.PostAsync("/api/streams/initialize", null);
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    
    var request = new
    {
      StreamId = "test-stream",
      FilePath = testFile,
      IsPrimary = false,
      AutoPlay = false
    };
    await _client.PostAsJsonAsync("/api/streams/sources/file", request);

    // Act
    var response = await _client.PostAsync("/api/streams/sources/test-stream/play", null);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    // Cleanup
    await _client.PostAsync("/api/streams/sources/test-stream/stop", null);
    await _client.DeleteAsync("/api/streams/sources/test-stream?fadeOut=false");
    await _client.PostAsync("/api/streams/shutdown", null);
  }

  [Fact]
  public async Task PauseStream_WithPlayingStream_ReturnsSuccess()
  {
    // Skip if headless
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    await _client.PostAsync("/api/streams/initialize", null);
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    
    var request = new
    {
      StreamId = "test-stream",
      FilePath = testFile,
      IsPrimary = false,
      AutoPlay = true
    };
    await _client.PostAsJsonAsync("/api/streams/sources/file", request);
    await Task.Delay(100); // Let it start playing

    // Act
    var response = await _client.PostAsync("/api/streams/sources/test-stream/pause", null);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    // Cleanup
    await _client.DeleteAsync("/api/streams/sources/test-stream?fadeOut=false");
    await _client.PostAsync("/api/streams/shutdown", null);
  }

  [Fact]
  public async Task StopStream_WithPlayingStream_ReturnsSuccess()
  {
    // Skip if headless
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    await _client.PostAsync("/api/streams/initialize", null);
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    
    var request = new
    {
      StreamId = "test-stream",
      FilePath = testFile,
      IsPrimary = false,
      AutoPlay = true
    };
    await _client.PostAsJsonAsync("/api/streams/sources/file", request);

    // Act
    var response = await _client.PostAsync("/api/streams/sources/test-stream/stop", null);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    // Cleanup
    await _client.DeleteAsync("/api/streams/sources/test-stream?fadeOut=false");
    await _client.PostAsync("/api/streams/shutdown", null);
  }

  [Fact]
  public async Task MuteAndUnmute_WithExistingStream_ReturnsSuccess()
  {
    // Skip if headless
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    await _client.PostAsync("/api/streams/initialize", null);
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    
    var request = new
    {
      StreamId = "test-stream",
      FilePath = testFile,
      IsPrimary = false,
      AutoPlay = false
    };
    await _client.PostAsJsonAsync("/api/streams/sources/file", request);

    // Act - Mute
    var muteResponse = await _client.PostAsync("/api/streams/sources/test-stream/mute", null);
    muteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    // Check mute status
    var statusResponse = await _client.GetAsync("/api/streams/sources/test-stream/mute");
    var statusContent = await statusResponse.Content.ReadAsStringAsync();
    statusContent.Should().Contain("\"isMuted\":true");

    // Act - Unmute
    var unmuteResponse = await _client.PostAsync("/api/streams/sources/test-stream/unmute", null);
    unmuteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    // Cleanup
    await _client.DeleteAsync("/api/streams/sources/test-stream?fadeOut=false");
    await _client.PostAsync("/api/streams/shutdown", null);
  }

  [Fact]
  public async Task FadeIn_WithExistingStream_ReturnsSuccess()
  {
    // Skip if headless
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    await _client.PostAsync("/api/streams/initialize", null);
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    
    var request = new
    {
      StreamId = "test-stream",
      FilePath = testFile,
      IsPrimary = false,
      AutoPlay = false
    };
    await _client.PostAsJsonAsync("/api/streams/sources/file", request);

    // Act
    var response = await _client.PostAsync("/api/streams/sources/test-stream/fadein?durationMs=500", null);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    // Cleanup
    await _client.DeleteAsync("/api/streams/sources/test-stream?fadeOut=false");
    await _client.PostAsync("/api/streams/shutdown", null);
  }

  [Fact]
  public async Task FadeOut_WithExistingStream_ReturnsSuccess()
  {
    // Skip if headless
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    await _client.PostAsync("/api/streams/initialize", null);
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    
    var request = new
    {
      StreamId = "test-stream",
      FilePath = testFile,
      IsPrimary = false,
      AutoPlay = true
    };
    await _client.PostAsJsonAsync("/api/streams/sources/file", request);

    // Act
    var response = await _client.PostAsync("/api/streams/sources/test-stream/fadeout?durationMs=500", null);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    // Cleanup
    await _client.DeleteAsync("/api/streams/sources/test-stream?fadeOut=false");
    await _client.PostAsync("/api/streams/shutdown", null);
  }

  [Fact]
  public async Task GetVolume_WithExistingStream_ReturnsVolume()
  {
    // Skip if headless
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    await _client.PostAsync("/api/streams/initialize", null);
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    
    var request = new
    {
      StreamId = "test-stream",
      FilePath = testFile,
      IsPrimary = false,
      AutoPlay = false
    };
    await _client.PostAsJsonAsync("/api/streams/sources/file", request);

    // Act
    var response = await _client.GetAsync("/api/streams/sources/test-stream/volume");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var content = await response.Content.ReadAsStringAsync();
    content.Should().Contain("\"volume\":");

    // Cleanup
    await _client.DeleteAsync("/api/streams/sources/test-stream?fadeOut=false");
    await _client.PostAsync("/api/streams/shutdown", null);
  }

  private static bool IsHeadlessEnvironment()
  {
    // Check for headless environment
    var display = Environment.GetEnvironmentVariable("DISPLAY");
    var ci = Environment.GetEnvironmentVariable("CI");
    return string.IsNullOrEmpty(display) || !string.IsNullOrEmpty(ci);
  }

  public void Dispose()
  {
    // Ensure cleanup
    try
    {
      _client.PostAsync("/api/streams/shutdown", null).Wait();
    }
    catch
    {
      // Ignore cleanup errors
    }
    _client?.Dispose();
    
    // Reset AudioEngineManager to allow next test to create a new engine
    StreamAudio.Core.AudioEngineManager.Reset();
    
    // Release the API test lock
    ApiTestLock.Release();
  }
}

// Define the ApiTests collection to ensure these tests run sequentially
[CollectionDefinition("ApiTests", DisableParallelization = true)]
public class ApiTestsCollection
{
}
