using Xunit;
using FluentAssertions;
using StreamAudio.Core.Playback;
using StreamAudio.Core.Sources;
using SoundFlow.Structs;

namespace StreamAudio.Tests;

/// <summary>
/// Tests for audio format conversion and mixing behavior.
/// </summary>
public class AudioMixerTests : IDisposable
{
  private const string TestDataPath = "../../../../../testdata";
  private readonly List<IDisposable> disposables = new();

  [Fact]
  public void Mixer_WithMonoSource_ShouldConvertToStereoOutput()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    using var playback = new AudioPlayback();
    disposables.Add(playback);
    
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    using var source = new FileAudioSource(testFile);
    disposables.Add(source);

    // Act
    playback.AddPlayer(source.Player);
    source.Play();
    Thread.Sleep(100); // Let it play briefly

    // Assert
    // The mixer format should be stereo (2 channels)
    playback.Format.Channels.Should().BeGreaterThanOrEqualTo(2, 
      "Mixer should output stereo even with mono input");
  }

  [Fact]
  public void Mixer_WithMultipleMonoSources_ShouldMixToStereo()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    using var playback = new AudioPlayback();
    disposables.Add(playback);
    
    string testFile1 = Path.Combine(TestDataPath, "100hz.wav");
    string testFile2 = Path.Combine(TestDataPath, "200hz.wav");
    using var source1 = new FileAudioSource(testFile1);
    disposables.Add(source1);
    using var source2 = new FileAudioSource(testFile2);
    disposables.Add(source2);

    // Act
    playback.AddPlayer(source1.Player);
    playback.AddPlayer(source2.Player);
    playback.SetVolume(source1.Player, 0.5f);
    playback.SetVolume(source2.Player, 0.5f);
    
    source1.Play();
    source2.Play();
    Thread.Sleep(100); // Let it play briefly

    // Assert
    playback.Format.Channels.Should().BeGreaterThanOrEqualTo(2,
      "Mixer should output stereo when mixing multiple mono sources");
    
    playback.GetVolume(source1.Player).Should().BeApproximately(0.5f, 0.01f);
    playback.GetVolume(source2.Player).Should().BeApproximately(0.5f, 0.01f);
  }

  [Fact]
  public void Mixer_MonoToStereoConversion_ShouldPreserveVolume()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    using var playback = new AudioPlayback();
    disposables.Add(playback);
    
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    using var source = new FileAudioSource(testFile);
    disposables.Add(source);

    // Act
    playback.AddPlayer(source.Player);
    float expectedVolume = 0.75f;
    playback.SetVolume(source.Player, expectedVolume);
    source.Play();
    Thread.Sleep(50);

    // Assert
    float actualVolume = playback.GetVolume(source.Player);
    actualVolume.Should().BeApproximately(expectedVolume, 0.01f,
      "Volume should be preserved during mono to stereo conversion");
  }

  [Fact]
  public void FFTAudioPlayback_WithMonoSource_ShouldCaptureCorrectly()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    using var fftPlayback = new FFTAudioPlayback();
    disposables.Add(fftPlayback);
    
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    using var source = new FileAudioSource(testFile);
    disposables.Add(source);

    // Act
    fftPlayback.AddPlayer(source.Player);
    source.Play();
    Thread.Sleep(200); // Capture some data
    fftPlayback.Stop();

    // Assert
    fftPlayback.SampleCount.Should().BeGreaterThan(0,
      "FFT playback should capture samples from mono source");
    fftPlayback.AudioDuration.Should().NotBeNull();
    fftPlayback.TopFrequencies.Should().NotBeNull();
  }

  [Fact]
  public void StreamManager_WithMonoSources_ShouldMixCorrectly()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    using var playback = new AudioPlayback();
    disposables.Add(playback);
    
    using var manager = new StreamManager(playback);
    disposables.Add(manager);
    
    string testFile1 = Path.Combine(TestDataPath, "100hz.wav");
    string testFile2 = Path.Combine(TestDataPath, "200hz.wav");
    using var source1 = new FileAudioSource(testFile1, sourceType: SourceType.Manual);
    disposables.Add(source1);
    using var source2 = new FileAudioSource(testFile2, sourceType: SourceType.Manual);
    disposables.Add(source2);

    // Act
    manager.AddSource("mono1", source1, isPrimary: true);
    manager.AddSource("mono2", source2);
    
    manager.Play("mono1");
    manager.Play("mono2");
    Thread.Sleep(100);

    // Assert
    manager.StreamCount.Should().Be(2);
    manager.PrimaryStreamId.Should().Be("mono1");
    
    // Primary should be at full volume, background at reduced volume
    float primaryVolume = manager.GetVolume("mono1");
    float backgroundVolume = manager.GetVolume("mono2");
    
    primaryVolume.Should().BeGreaterThan(backgroundVolume,
      "Primary stream should have higher volume than background");
  }

  [Fact]
  public void Mixer_FormatInfo_ShouldExposeChannelConfiguration()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
      return;

    // Arrange & Act
    using var playback = new AudioPlayback();
    disposables.Add(playback);

    // Assert
    playback.Format.Should().NotBeNull();
    playback.Format.Channels.Should().BeGreaterThan(0);
    playback.Format.SampleRate.Should().BeGreaterThan(0);
    
    // Standard audio formats are typically 1 (mono) or 2 (stereo)
    playback.Format.Channels.Should().BeLessThanOrEqualTo(2,
      "Standard mixer should use mono or stereo output");
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
        // Ignore disposal errors
      }
    }
  }
}
