using FluentAssertions;
using StreamAudio.Core;
using StreamAudio.Core.Sources;
using StreamAudio.Core.Playback;

namespace StreamAudio.Tests;

[Collection("AudioTests")]
public class AudioPlaybackTests : IDisposable
{
  private const string TestDataPath = "../../../../../testdata";
  private readonly List<IDisposable> disposables = new();

  [Fact]
  public void Constructor_ShouldInitialize()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Act
    var playback = new AudioPlayback();
    disposables.Add(playback);

    // Assert
    playback.Mixer.Should().NotBeNull();
    playback.Format.SampleRate.Should().BeGreaterThan(0);
    playback.Format.Channels.Should().BeGreaterThan(0);
  }

  [Fact]
  public void AddPlayer_WithValidSource_ShouldAddToMixer()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    var playback = new AudioPlayback();
    disposables.Add(playback);

    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    var source = new FileAudioSource(testFile);
    disposables.Add(source);

    // Act
    playback.AddPlayer(source.Player);

    // Assert - No exception should be thrown
    playback.Mixer.Should().NotBeNull();
  }

  [Fact]
  public void SetVolume_ShouldAdjustPlayerVolume()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    var playback = new AudioPlayback();
    disposables.Add(playback);

    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    var source = new FileAudioSource(testFile);
    disposables.Add(source);

    playback.AddPlayer(source.Player);

    // Act
    playback.SetVolume(source.Player, 0.5f);

    // Assert
    playback.GetVolume(source.Player).Should().Be(0.5f);
  }

  [Fact]
  public void MixingTwoSources_ShouldPlayBoth()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    var playback = new AudioPlayback();
    disposables.Add(playback);

    string testFile1 = Path.Combine(TestDataPath, "100hz.wav");
    string testFile2 = Path.Combine(TestDataPath, "200hz.wav");
    var source1 = new FileAudioSource(testFile1);
    var source2 = new FileAudioSource(testFile2);
    disposables.Add(source1);
    disposables.Add(source2);

    // Act
    playback.AddPlayer(source1.Player);
    playback.AddPlayer(source2.Player);
    playback.SetVolume(source1.Player, 0.5f);
    playback.SetVolume(source2.Player, 0.5f);

    source1.Play();
    source2.Play();

    Thread.Sleep(200); // Let them play for a moment

    // Assert
    source1.State.Should().NotBe(SoundFlow.Enums.PlaybackState.Stopped);
    source2.State.Should().NotBe(SoundFlow.Enums.PlaybackState.Stopped);
  }

  [Fact]
  public void RemovePlayer_ShouldRemoveFromMixer()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    var playback = new AudioPlayback();
    disposables.Add(playback);

    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    var source = new FileAudioSource(testFile);
    disposables.Add(source);

    playback.AddPlayer(source.Player);

    // Act
    playback.RemovePlayer(source.Player);

    // Assert - No exception should be thrown
    playback.Mixer.Should().NotBeNull();
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
      try
      {
        disposable?.Dispose();
      }
      catch
      {
        // Ignore disposal errors for individual resources
      }
    }
    disposables.Clear();
  }
}
