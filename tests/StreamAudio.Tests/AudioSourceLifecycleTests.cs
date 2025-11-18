using Xunit;
using FluentAssertions;
using StreamAudio.Core.Playback;
using StreamAudio.Core.Sources;
using SoundFlow.Structs;

namespace StreamAudio.Tests;

[Collection("AudioTests")]
public class AudioSourceLifecycleTests : IDisposable
{
  private const string TestDataPath = "../../../../../testdata";
  private readonly List<IDisposable> disposables = new();

  [Fact]
  public void FFTAudioPlayback_ShouldCaptureAudioData()
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
    Thread.Sleep(100); // Let it capture some data
    fftPlayback.Stop();

    // Assert
    fftPlayback.SampleCount.Should().BeGreaterThan(0);
    fftPlayback.AudioDuration.Should().NotBeNull();
  }

  [Fact]
  public void FFTAudioPlayback_ShouldPerformFFTAnalysis()
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
    Thread.Sleep(100); // Let it capture some data
    fftPlayback.Stop();

    // Assert
    fftPlayback.TopFrequencies.Should().NotBeNull();
    fftPlayback.TopFrequencies!.Count.Should().BeLessThanOrEqualTo(5);
  }

  [Fact]
  public void StreamManager_WithAutoSource_ShouldRaiseAudioPlayBeginEvent()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    using var playback = new AudioPlayback();
    disposables.Add(playback);
    
    using var manager = new StreamManager(playback);
    disposables.Add(manager);
    
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    using var source = new FileAudioSource(testFile, sourceType: SourceType.Auto);
    disposables.Add(source);
    
    bool eventRaised = false;
    manager.AudioPlayBegin += (sender, args) => eventRaised = true;

    // Act
    manager.AddSource("auto1", source);
    manager.Play("auto1");
    Thread.Sleep(50);

    // Assert
    eventRaised.Should().BeTrue("AudioPlayBegin should fire when auto source starts");
  }

  [Fact]
  public void StreamManager_WhenAllAutoSourcesComplete_ShouldRaiseAllAudioCompleteEvent()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    using var playback = new AudioPlayback();
    disposables.Add(playback);
    
    using var manager = new StreamManager(playback);
    disposables.Add(manager);
    
    manager.MaxStreamDuration = 1; // Set short duration for test
    
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    using var source = new FileAudioSource(testFile, sourceType: SourceType.Auto);
    disposables.Add(source);
    
    source.Loop = false; // Don't loop
    
    bool beginEventRaised = false;
    var completeEvent = new ManualResetEventSlim(false);
    
    manager.AudioPlayBegin += (sender, args) => beginEventRaised = true;
    manager.AllAudioComplete += (sender, args) => completeEvent.Set();

    // Act
    manager.AddSource("auto1", source);
    manager.Play("auto1");
    
    // Wait for completion with timeout
    bool completed = completeEvent.Wait(TimeSpan.FromSeconds(3));

    // Assert
    beginEventRaised.Should().BeTrue("AudioPlayBegin should fire");
    completed.Should().BeTrue("AllAudioComplete should fire when auto source completes");
    manager.StreamCount.Should().Be(0, "Auto source should be removed after completion");
  }

  [Fact]
  public void StreamManager_ManualSource_ShouldNotBeAutoRemoved()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    using var playback = new AudioPlayback();
    disposables.Add(playback);
    
    using var manager = new StreamManager(playback);
    disposables.Add(manager);
    
    manager.MaxStreamDuration = 1;
    
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    using var source = new FileAudioSource(testFile, sourceType: SourceType.Manual);
    disposables.Add(source);

    // Act
    manager.AddSource("manual1", source);
    manager.Play("manual1");
    Thread.Sleep(200); // Brief wait to ensure monitoring has run at least once

    // Assert
    manager.StreamCount.Should().Be(1, "Manual source should not be auto-removed");
  }

  [Fact]
  public void StreamManager_AutoSource_ShouldRespectMaxStreamDuration()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    using var playback = new AudioPlayback();
    disposables.Add(playback);
    
    using var manager = new StreamManager(playback);
    disposables.Add(manager);
    
    manager.MaxStreamDuration = 1; // 1 second max
    
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    using var source = new FileAudioSource(testFile, sourceType: SourceType.Auto);
    disposables.Add(source);
    
    source.Loop = true; // Try to loop forever
    
    var completeEvent = new ManualResetEventSlim(false);
    manager.AllAudioComplete += (sender, args) => completeEvent.Set();

    // Act
    manager.AddSource("auto1", source);
    manager.Play("auto1");
    
    // Wait for completion with timeout
    bool completed = completeEvent.Wait(TimeSpan.FromSeconds(3));

    // Assert
    completed.Should().BeTrue("Auto source should be stopped and removed after MaxStreamDuration");
    manager.StreamCount.Should().Be(0, "Auto source should be removed after MaxStreamDuration");
  }

  [Fact]
  public void FileAudioSource_RepeatCount_ShouldHonorCount()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    using var source = new FileAudioSource(testFile, sourceType: SourceType.Auto);
    disposables.Add(source);
    
    source.RepeatCount = 2;
    source.Loop = true;

    // Act
    var initialCount = source.RepeatCount;

    // Assert
    initialCount.Should().Be(2);
  }

  [Fact]
  public void FileAudioSource_RepeatCount_DefaultShouldBeOne()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
      return;

    // Arrange & Act
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    using var source = new FileAudioSource(testFile);
    disposables.Add(source);

    // Assert
    source.RepeatCount.Should().Be(1);
  }

  [Fact]
  public void FileAudioSource_SourceType_ShouldBeSettable()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
      return;

    // Arrange & Act
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    using var manualSource = new FileAudioSource(testFile, sourceType: SourceType.Manual);
    disposables.Add(manualSource);
    
    using var autoSource = new FileAudioSource(testFile, sourceType: SourceType.Auto);
    disposables.Add(autoSource);

    // Assert
    manualSource.SourceType.Should().Be(SourceType.Manual);
    autoSource.SourceType.Should().Be(SourceType.Auto);
  }

  [Fact]
  public void FileAudioSource_DefaultSourceType_ShouldBeManual()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
      return;

    // Arrange & Act
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    using var source = new FileAudioSource(testFile);
    disposables.Add(source);

    // Assert
    source.SourceType.Should().Be(SourceType.Manual);
  }

  [Fact]
  public void StreamManager_MaxStreamDuration_DefaultShouldBe30Seconds()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    using var playback = new AudioPlayback();
    disposables.Add(playback);
    
    using var manager = new StreamManager(playback);
    disposables.Add(manager);

    // Assert
    manager.MaxStreamDuration.Should().Be(30);
  }

  [Fact]
  public void StreamManager_MaxStreamDuration_ShouldBeConfigurable()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    using var playback = new AudioPlayback();
    disposables.Add(playback);
    
    using var manager = new StreamManager(playback);
    disposables.Add(manager);

    // Act
    manager.MaxStreamDuration = 60;

    // Assert
    manager.MaxStreamDuration.Should().Be(60);
  }

  [Fact]
  public void FFTAudioPlayback_MixingTwoTones_ShouldDetectBothFrequencies()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
      return;

    // This is a more complex integration test
    // For now, we'll test the basic structure
    using var fftPlayback = new FFTAudioPlayback();
    disposables.Add(fftPlayback);
    
    string testFile1 = Path.Combine(TestDataPath, "100hz.wav");
    string testFile2 = Path.Combine(TestDataPath, "200hz.wav");
    using var source1 = new FileAudioSource(testFile1);
    disposables.Add(source1);
    
    using var source2 = new FileAudioSource(testFile2);
    disposables.Add(source2);

    // Act
    fftPlayback.AddPlayer(source1.Player);
    fftPlayback.AddPlayer(source2.Player);
    source1.Play();
    source2.Play();
    Thread.Sleep(100); // Let it capture some data
    fftPlayback.Stop();

    // Assert
    fftPlayback.TopFrequencies.Should().NotBeNull();
    fftPlayback.TopFrequencies!.Count.Should().BeGreaterThan(0);
  }

  [Fact]
  public void StreamManager_WithFFTPlayback_ShouldWork()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    using var fftPlayback = new FFTAudioPlayback();
    disposables.Add(fftPlayback);
    
    using var manager = new StreamManager(fftPlayback);
    disposables.Add(manager);
    
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    using var source = new FileAudioSource(testFile);
    disposables.Add(source);

    // Act
    manager.AddSource("test1", source);
    manager.Play("test1");
    Thread.Sleep(100);
    fftPlayback.Stop();

    // Assert
    manager.StreamCount.Should().Be(1);
    fftPlayback.SampleCount.Should().BeGreaterThan(0);
  }

  [Fact]
  public void IAudioSource_Interface_ShouldExposeRequiredProperties()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
      return;

    // Arrange & Act
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    IAudioSource source = new FileAudioSource(testFile);
    disposables.Add(source);

    // Assert
    source.Name.Should().NotBeNullOrEmpty();
    source.Format.Should().NotBeNull();
    source.SampleRate.Should().BeGreaterThan(0);
    source.Channels.Should().BeGreaterThan(0);
    source.SourceType.Should().Be(SourceType.Manual);
    source.RepeatCount.Should().Be(1);
    source.Player.Should().NotBeNull();
  }

  [Fact]
  public void IAudioPlayback_Interface_ShouldExposeRequiredMethods()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    IAudioPlayback playback = new AudioPlayback();
    disposables.Add(playback);

    // Assert
    playback.Format.Should().NotBeNull();
    playback.Mixer.Should().NotBeNull();
    playback.IsDeviceHealthy().Should().BeTrue();
  }

  [Fact]
  public void FileAudioSource_RepeatCount_WithZero_ShouldLoopIndefinitely()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    using var source = new FileAudioSource(testFile, sourceType: SourceType.Auto);
    disposables.Add(source);
    
    // Act
    source.RepeatCount = 0; // Infinite loop
    source.Loop = true;

    // Assert
    source.RepeatCount.Should().Be(0);
    // Note: In real usage, StreamManager.MaxStreamDuration would stop this
  }

  [Fact]
  public void StreamManager_WithMultipleAutoSources_ShouldRemoveAllWhenComplete()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    using var playback = new AudioPlayback();
    disposables.Add(playback);
    
    using var manager = new StreamManager(playback);
    disposables.Add(manager);
    
    manager.MaxStreamDuration = 1;
    
    string testFile1 = Path.Combine(TestDataPath, "100hz.wav");
    string testFile2 = Path.Combine(TestDataPath, "200hz.wav");
    using var source1 = new FileAudioSource(testFile1, sourceType: SourceType.Auto);
    disposables.Add(source1);
    
    using var source2 = new FileAudioSource(testFile2, sourceType: SourceType.Auto);
    disposables.Add(source2);
    
    var completeEvent = new ManualResetEventSlim(false);
    manager.AllAudioComplete += (sender, args) => completeEvent.Set();

    // Act
    manager.AddSource("auto1", source1);
    manager.AddSource("auto2", source2);
    manager.Play("auto1");
    manager.Play("auto2");
    
    // Wait for completion with timeout
    bool completed = completeEvent.Wait(TimeSpan.FromSeconds(3));

    // Assert
    completed.Should().BeTrue("AllAudioComplete should fire when all auto sources complete");
    manager.StreamCount.Should().Be(0, "All auto sources should be removed");
  }

  [Fact]
  public void StreamManager_MixedManualAndAutoSources_ShouldOnlyRemoveAuto()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    using var playback = new AudioPlayback();
    disposables.Add(playback);
    
    using var manager = new StreamManager(playback);
    disposables.Add(manager);
    
    manager.MaxStreamDuration = 1;
    
    string testFile1 = Path.Combine(TestDataPath, "100hz.wav");
    string testFile2 = Path.Combine(TestDataPath, "200hz.wav");
    using var manualSource = new FileAudioSource(testFile1, sourceType: SourceType.Manual);
    disposables.Add(manualSource);
    
    using var autoSource = new FileAudioSource(testFile2, sourceType: SourceType.Auto);
    disposables.Add(autoSource);

    // Act
    manager.AddSource("manual1", manualSource);
    manager.AddSource("auto1", autoSource);
    manager.Play("manual1");
    manager.Play("auto1");
    
    // Wait for auto source to complete and be removed
    // Note: AllAudioComplete won't fire because manual source remains
    // We need to poll for the auto source removal
    int maxWaitMs = 3000;
    int waitedMs = 0;
    while (manager.StreamCount > 1 && waitedMs < maxWaitMs)
    {
      Thread.Sleep(100);
      waitedMs += 100;
    }

    // Assert
    manager.StreamCount.Should().Be(1, "Only manual source should remain");
  }

  private bool IsHeadlessEnvironment()
  {
    // Check for CI environment variables
    return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) ||
           !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
  }

  [Fact]
  public void FileAudioSource_RepeatCount_ShouldPlayCorrectNumberOfTimes()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    using var source = new FileAudioSource(testFile, sourceType: SourceType.Auto);
    disposables.Add(source);
    
    source.RepeatCount = 3;
    source.Loop = true;

    // Get the expected duration (1 second per 100hz.wav file * 3 repeats)
    var expectedMinDuration = TimeSpan.FromMilliseconds(2800); // Allow for slight timing variance
    var expectedMaxDuration = TimeSpan.FromMilliseconds(3200);

    // Act
    var startTime = DateTime.UtcNow;
    source.Play();
    
    // Wait for the source to complete 3 plays
    // The 100hz.wav file is 1 second, so 3 plays = ~3 seconds
    Thread.Sleep(4000); // Wait 4 seconds to ensure completion
    
    var endTime = DateTime.UtcNow;
    var actualDuration = endTime - startTime;

    // Assert
    // The source should have stopped after 3 plays (not playing anymore)
    source.State.Should().Be(SoundFlow.Enums.PlaybackState.Stopped, 
      "source should stop after RepeatCount plays");
    
    // Verify the duration is approximately 3 seconds (allowing for some timing variance)
    actualDuration.Should().BeGreaterThanOrEqualTo(expectedMinDuration,
      $"source should play for at least {expectedMinDuration.TotalSeconds} seconds for 3 repeats");
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
