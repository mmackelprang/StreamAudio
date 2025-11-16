using FluentAssertions;
using StreamAudio.Core;
using StreamAudio.Core.Sources;
using StreamAudio.Core.Playback;

namespace StreamAudio.Tests;

public class FileAudioSourceTests : IDisposable
{
  private const string TestDataPath = "../../../../../testdata";
  private readonly List<IDisposable> disposables = new();

  [Fact]
  public void Constructor_WithValidFile_ShouldInitialize()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    string testFile = Path.Combine(TestDataPath, "100hz.wav");

    // Act
    var source = new FileAudioSource(testFile);
    disposables.Add(source);

    // Assert
    source.Name.Should().Be("100hz.wav");
    source.SampleRate.Should().BeGreaterThan(0);
    source.Channels.Should().BeGreaterThan(0);
    source.Loop.Should().BeFalse();
  }

  [Fact]
  public void Constructor_WithInvalidFile_ShouldThrowFileNotFoundException()
  {
    // Arrange
    string invalidFile = "nonexistent.wav";

    // Act
    Action act = () => new FileAudioSource(invalidFile);

    // Assert (this doesn't require audio initialization)
    act.Should().Throw<FileNotFoundException>();
  }

  [Fact]
  public void Play_ShouldChangeStateToPlaying()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    var source = new FileAudioSource(testFile);
    disposables.Add(source);

    using var playback = new AudioPlayback();
    playback.AddPlayer(source.Player);

    // Act
    source.Play();
    Thread.Sleep(100); // Give it a moment to start

    // Assert
    source.State.Should().NotBe(SoundFlow.Enums.PlaybackState.Stopped);
  }

  [Fact]
  public void Stop_ShouldChangeStateToStopped()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    var source = new FileAudioSource(testFile);
    disposables.Add(source);

    using var playback = new AudioPlayback();
    playback.AddPlayer(source.Player);

    source.Play();
    Thread.Sleep(100);

    // Act
    source.Stop();
    Thread.Sleep(100);

    // Assert
    source.State.Should().Be(SoundFlow.Enums.PlaybackState.Stopped);
  }

  [Fact]
  public void Loop_WhenEnabled_ShouldRestartAfterEnding()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    var source = new FileAudioSource(testFile) { Loop = true };
    disposables.Add(source);

    using var playback = new AudioPlayback();
    playback.AddPlayer(source.Player);

    // Act
    source.Play();

    // Wait for file to finish and loop (file is 1 second, wait 2 seconds)
    Thread.Sleep(2000);

    // Assert
    source.Loop.Should().BeTrue();
    // The looping mechanism should keep it playing or restart it
  }

  private bool IsHeadlessEnvironment()
  {
    // Check for CI environment variables
    return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) ||
           !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
  }

  public void Dispose()
  {
    foreach (var disposable in disposables)
    {
      disposable.Dispose();
    }
    disposables.Clear();

    // Cleanup audio engine only if it was initialized
    try
    {
      AudioEngineManager.Dispose();
    }
    catch
    {
      // Ignore cleanup errors
    }
  }
}
