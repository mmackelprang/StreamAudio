using FluentAssertions;
using StreamAudio.Core;
using StreamAudio.Core.Sources;
using StreamAudio.Core.Playback;

namespace StreamAudio.Tests;

[Collection("AudioTests")]
public class StreamManagerTests : IDisposable
{
  private const string TestDataPath = "../../../../../testdata";
  private readonly List<IDisposable> disposables = new();

  [Fact]
  public void Constructor_WithValidPlayback_ShouldInitialize()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    var playback = new AudioPlayback();
    disposables.Add(playback);

    // Act
    var manager = new StreamManager(playback);
    disposables.Add(manager);

    // Assert
    manager.StreamCount.Should().Be(0);
    manager.PrimaryStreamId.Should().BeNull();
    manager.BackgroundVolume.Should().Be(0.3f);
  }

  [Fact]
  public void AddSource_WithValidSource_ShouldAddToManager()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    var playback = new AudioPlayback();
    disposables.Add(playback);
    var manager = new StreamManager(playback);
    disposables.Add(manager);

    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    var source = new FileAudioSource(testFile);
    disposables.Add(source);

    // Act
    manager.AddSource("stream1", source);

    // Assert
    manager.StreamCount.Should().Be(1);
  }

  [Fact]
  public void AddSource_WithPrimaryFlag_ShouldSetAsPrimary()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    var playback = new AudioPlayback();
    disposables.Add(playback);
    var manager = new StreamManager(playback);
    disposables.Add(manager);

    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    var source = new FileAudioSource(testFile);
    disposables.Add(source);

    // Act
    manager.AddSource("stream1", source, isPrimary: true);

    // Assert
    manager.PrimaryStreamId.Should().Be("stream1");
  }

  [Fact]
  public void RemoveSource_WithValidId_ShouldRemoveStream()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    var playback = new AudioPlayback();
    disposables.Add(playback);
    var manager = new StreamManager(playback);
    disposables.Add(manager);

    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    var source = new FileAudioSource(testFile);
    disposables.Add(source);

    manager.AddSource("stream1", source);

    // Act
    manager.RemoveSource("stream1", fadeOut: false);

    // Assert
    manager.StreamCount.Should().Be(0);
  }

  [Fact]
  public void SetPrimaryStream_WithValidId_ShouldSetPrimary()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    var playback = new AudioPlayback();
    disposables.Add(playback);
    var manager = new StreamManager(playback);
    disposables.Add(manager);

    string testFile1 = Path.Combine(TestDataPath, "100hz.wav");
    string testFile2 = Path.Combine(TestDataPath, "200hz.wav");
    var source1 = new FileAudioSource(testFile1);
    var source2 = new FileAudioSource(testFile2);
    disposables.Add(source1);
    disposables.Add(source2);

    manager.AddSource("stream1", source1);
    manager.AddSource("stream2", source2);

    // Act
    manager.SetPrimaryStream("stream2");

    // Assert
    manager.PrimaryStreamId.Should().Be("stream2");
  }

  [Fact]
  public void SetPrimaryStream_ShouldAdjustVolumes()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    var playback = new AudioPlayback();
    disposables.Add(playback);
    var manager = new StreamManager(playback);
    disposables.Add(manager);

    string testFile1 = Path.Combine(TestDataPath, "100hz.wav");
    string testFile2 = Path.Combine(TestDataPath, "200hz.wav");
    var source1 = new FileAudioSource(testFile1);
    var source2 = new FileAudioSource(testFile2);
    disposables.Add(source1);
    disposables.Add(source2);

    manager.AddSource("stream1", source1);
    manager.AddSource("stream2", source2);
    manager.Play("stream1");
    manager.Play("stream2");

    Thread.Sleep(100); // Give time for volumes to be set

    // Act
    manager.SetPrimaryStream("stream1");
    Thread.Sleep(100);

    // Assert
    manager.GetVolume("stream1").Should().Be(1.0f);
    manager.GetVolume("stream2").Should().Be(0.3f); // Default background volume
  }

  [Fact]
  public void ClearPrimaryStream_ShouldSetAllToBackgroundVolume()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    var playback = new AudioPlayback();
    disposables.Add(playback);
    var manager = new StreamManager(playback);
    disposables.Add(manager);

    string testFile1 = Path.Combine(TestDataPath, "100hz.wav");
    string testFile2 = Path.Combine(TestDataPath, "200hz.wav");
    var source1 = new FileAudioSource(testFile1);
    var source2 = new FileAudioSource(testFile2);
    disposables.Add(source1);
    disposables.Add(source2);

    manager.AddSource("stream1", source1, isPrimary: true);
    manager.AddSource("stream2", source2);
    manager.Play("stream1");
    manager.Play("stream2");

    Thread.Sleep(100);

    // Act
    manager.ClearPrimaryStream();
    Thread.Sleep(100);

    // Assert
    manager.PrimaryStreamId.Should().BeNull();
    manager.GetVolume("stream1").Should().Be(0.3f);
    manager.GetVolume("stream2").Should().Be(0.3f);
  }

  [Fact]
  public void Mute_ShouldSetVolumeToZero()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    var playback = new AudioPlayback();
    disposables.Add(playback);
    var manager = new StreamManager(playback);
    disposables.Add(manager);

    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    var source = new FileAudioSource(testFile);
    disposables.Add(source);

    manager.AddSource("stream1", source, isPrimary: true);
    manager.Play("stream1");
    Thread.Sleep(100);

    // Act
    manager.Mute("stream1");

    // Assert
    manager.IsMuted("stream1").Should().BeTrue();
    manager.GetVolume("stream1").Should().Be(0.0f);
  }

  [Fact]
  public void Unmute_ShouldRestoreVolume()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    var playback = new AudioPlayback();
    disposables.Add(playback);
    var manager = new StreamManager(playback);
    disposables.Add(manager);

    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    var source = new FileAudioSource(testFile);
    disposables.Add(source);

    manager.AddSource("stream1", source, isPrimary: true);
    manager.Play("stream1");
    Thread.Sleep(100);
    manager.Mute("stream1");

    // Act
    manager.Unmute("stream1");
    Thread.Sleep(100);

    // Assert
    manager.IsMuted("stream1").Should().BeFalse();
    manager.GetVolume("stream1").Should().Be(1.0f); // Primary stream should be at full volume
  }

  [Fact]
  public void BackgroundVolume_ShouldAdjustAllBackgroundStreams()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    var playback = new AudioPlayback();
    disposables.Add(playback);
    var manager = new StreamManager(playback);
    disposables.Add(manager);

    string testFile1 = Path.Combine(TestDataPath, "100hz.wav");
    string testFile2 = Path.Combine(TestDataPath, "200hz.wav");
    var source1 = new FileAudioSource(testFile1);
    var source2 = new FileAudioSource(testFile2);
    disposables.Add(source1);
    disposables.Add(source2);

    manager.AddSource("stream1", source1, isPrimary: true);
    manager.AddSource("stream2", source2);
    manager.Play("stream1");
    manager.Play("stream2");
    Thread.Sleep(100);

    // Act
    manager.BackgroundVolume = 0.5f;
    Thread.Sleep(100);

    // Assert
    manager.GetVolume("stream1").Should().Be(1.0f); // Primary unchanged
    manager.GetVolume("stream2").Should().Be(0.5f); // Background adjusted
  }

  [Fact]
  public void Play_WithFadeIn_ShouldGraduallyIncreaseVolume()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    var playback = new AudioPlayback();
    disposables.Add(playback);
    var manager = new StreamManager(playback);
    disposables.Add(manager);

    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    var source = new FileAudioSource(testFile);
    disposables.Add(source);

    manager.AddSource("stream1", source, isPrimary: true);

    // Act
    var initialVolume = manager.GetVolume("stream1");
    manager.Play("stream1", fadeIn: true);
    Thread.Sleep(100); // Allow some fade time

    var midVolume = manager.GetVolume("stream1");
    Thread.Sleep(1000); // Allow fade to complete

    var finalVolume = manager.GetVolume("stream1");

    // Assert
    initialVolume.Should().Be(0.0f);
    midVolume.Should().BeGreaterThan(0.0f);
    midVolume.Should().BeLessThan(1.0f);
    finalVolume.Should().Be(1.0f);
  }

  [Fact]
  public void Stop_WithFadeOut_ShouldGraduallyDecreaseVolume()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    var playback = new AudioPlayback();
    disposables.Add(playback);
    var manager = new StreamManager(playback);
    disposables.Add(manager);

    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    var source = new FileAudioSource(testFile);
    disposables.Add(source);

    manager.AddSource("stream1", source, isPrimary: true);
    manager.Play("stream1");
    Thread.Sleep(100);

    // Act
    var initialVolume = manager.GetVolume("stream1");
    manager.Stop("stream1", fadeOut: true);
    Thread.Sleep(100); // Allow some fade time

    var midVolume = manager.GetVolume("stream1");

    // Assert
    initialVolume.Should().Be(1.0f);
    midVolume.Should().BeLessThan(1.0f);
    midVolume.Should().BeGreaterThanOrEqualTo(0.0f);
  }

  [Fact]
  public void SwitchingPrimaryStream_ShouldTransitionVolumes()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    var playback = new AudioPlayback();
    disposables.Add(playback);
    var manager = new StreamManager(playback);
    disposables.Add(manager);

    string testFile1 = Path.Combine(TestDataPath, "100hz.wav");
    string testFile2 = Path.Combine(TestDataPath, "200hz.wav");
    var source1 = new FileAudioSource(testFile1);
    var source2 = new FileAudioSource(testFile2);
    disposables.Add(source1);
    disposables.Add(source2);

    manager.AddSource("stream1", source1, isPrimary: true);
    manager.AddSource("stream2", source2);
    manager.Play("stream1");
    manager.Play("stream2");
    Thread.Sleep(100);

    // Verify initial state
    manager.GetVolume("stream1").Should().Be(1.0f);
    manager.GetVolume("stream2").Should().Be(0.3f);

    // Act - Switch primary
    manager.SetPrimaryStream("stream2");
    Thread.Sleep(100);

    // Assert
    manager.GetVolume("stream1").Should().Be(0.3f);
    manager.GetVolume("stream2").Should().Be(1.0f);
  }

  [Fact]
  public void AddSource_WithDuplicateId_ShouldThrowException()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    var playback = new AudioPlayback();
    disposables.Add(playback);
    var manager = new StreamManager(playback);
    disposables.Add(manager);

    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    var source1 = new FileAudioSource(testFile);
    var source2 = new FileAudioSource(testFile);
    disposables.Add(source1);
    disposables.Add(source2);

    manager.AddSource("stream1", source1);

    // Act & Assert
    var act = () => manager.AddSource("stream1", source2);
    act.Should().Throw<InvalidOperationException>()
       .WithMessage("*already exists*");
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
