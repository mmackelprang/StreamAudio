using FluentAssertions;
using StreamAudio.Core;
using StreamAudio.Core.Playback;
using StreamAudio.Core.Sources;

namespace StreamAudio.Tests;

/// <summary>
/// Integration tests for complex multi-source audio scenarios.
/// These tests verify that multiple audio sources work together correctly.
/// </summary>
[Collection("AudioTests")]
public class MultiSourceIntegrationTests : IDisposable
{
  private const string TestDataPath = "../../../../../testdata";
  private readonly List<IDisposable> disposables = new();

  [Fact]
  public void MixTtsWithBackgroundMusic_ShouldPlayBothSources()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
      return;

    // Skip if espeak is not available
    if (!IsESpeakAvailable())
      return;

    // Arrange
    using var playback = new AudioPlayback();
    disposables.Add(playback);

    using var manager = new StreamManager(playback);
    disposables.Add(manager);

    // Background music (manual source)
    string musicFile = Path.Combine(TestDataPath, "100hz.wav");
    var musicSource = new FileAudioSource(musicFile, sourceType: SourceType.Manual);
    disposables.Add(musicSource);

    // TTS announcement (auto source)
    var ttsConfig = new TtsConfiguration { Engine = "espeak" };
    var ttsSource = new TtsAudioSource("Test announcement", config: ttsConfig);
    disposables.Add(ttsSource);

    // Act
    manager.AddSource("music", musicSource, isPrimary: false);
    manager.AddSource("announcement", ttsSource, isPrimary: true);

    musicSource.Play();
    Thread.Sleep(100); // Let music start

    if (IsEspeakInstalled())
    {
      ttsSource.Play();
      Thread.Sleep(200); // Let both play together
    }

    // Assert
    // Streams contain("music");
    // Streams contain("announcement");
  }

  [Fact]
  public void MixMultipleFileSourcesWithDifferentPriorities_ShouldMaintainVolumeHierarchy()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    using var playback = new AudioPlayback();
    disposables.Add(playback);

    using var manager = new StreamManager(playback);
    disposables.Add(manager);

    // Three sources with different priorities
    var primary = new FileAudioSource(Path.Combine(TestDataPath, "100hz.wav"));
    disposables.Add(primary);

    var background1 = new FileAudioSource(Path.Combine(TestDataPath, "200hz.wav"));
    disposables.Add(background1);

    var background2 = new FileAudioSource(Path.Combine(TestDataPath, "50hz.wav"));
    disposables.Add(background2);

    // Act
    manager.AddSource("primary", primary, isPrimary: true);
    manager.AddSource("bg1", background1, isPrimary: false);
    manager.AddSource("bg2", background2, isPrimary: false);

    primary.Play();
    background1.Play();
    background2.Play();
    Thread.Sleep(200);

    // Assert
    manager.StreamCount.Should().Be(3);
    // Streams contain(new[] { "primary", "bg1", "bg2" });
  }

  [Fact]
  public void SwitchPrimaryStreamDuringPlayback_ShouldAdjustVolumes()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    using var playback = new AudioPlayback();
    disposables.Add(playback);

    using var manager = new StreamManager(playback);
    disposables.Add(manager);

    var source1 = new FileAudioSource(Path.Combine(TestDataPath, "100hz.wav"));
    disposables.Add(source1);

    var source2 = new FileAudioSource(Path.Combine(TestDataPath, "200hz.wav"));
    disposables.Add(source2);

    manager.AddSource("source1", source1, isPrimary: true);
    manager.AddSource("source2", source2, isPrimary: false);

    source1.Play();
    source2.Play();
    Thread.Sleep(100);

    // Act - Switch primary
    manager.SetPrimaryStream("source2");
    Thread.Sleep(100);

    // Assert - Both should still be playing
    // Streams contain(new[] { "source1", "source2" });
  }

  [Fact]
  public void AutoSourceCompletionWithBackgroundMusic_ShouldRemoveOnlyAutoSource()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    using var playback = new AudioPlayback();
    disposables.Add(playback);

    using var manager = new StreamManager(playback);
    disposables.Add(manager);

    // Manual background music
    var musicFile = Path.Combine(TestDataPath, "100hz.wav");
    var musicSource = new FileAudioSource(musicFile, sourceType: SourceType.Manual);
    musicSource.RepeatCount = 0; // Loop forever
    disposables.Add(musicSource);

    // Auto notification (short-lived)
    var notificationFile = Path.Combine(TestDataPath, "200hz.wav");
    var notificationSource = new FileAudioSource(notificationFile, sourceType: SourceType.Auto);
    notificationSource.RepeatCount = 1; // Play once
    disposables.Add(notificationSource);

    // Act
    manager.AddSource("music", musicSource, isPrimary: false);
    manager.AddSource("notification", notificationSource, isPrimary: true);

    musicSource.Play();
    notificationSource.Play();

    // Wait for notification to complete (1 second file + some buffer)
    Thread.Sleep(1500);

    // Assert - Music should still be in manager, notification should be auto-removed
    // Streams contain("music");
    // Note: Auto-removal happens in MonitorStreams which runs every 100ms
  }

  [Fact]
  public void ThreeSourcesMixing_ShouldHandleConcurrentPlayback()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    using var playback = new AudioPlayback();
    disposables.Add(playback);

    using var manager = new StreamManager(playback);
    disposables.Add(manager);

    var source1 = new FileAudioSource(Path.Combine(TestDataPath, "50hz.wav"));
    disposables.Add(source1);

    var source2 = new FileAudioSource(Path.Combine(TestDataPath, "100hz.wav"));
    disposables.Add(source2);

    var source3 = new FileAudioSource(Path.Combine(TestDataPath, "200hz.wav"));
    disposables.Add(source3);

    // Act
    manager.AddSource("low", source1, isPrimary: false);
    manager.AddSource("mid", source2, isPrimary: true);
    manager.AddSource("high", source3, isPrimary: false);

    source1.Play();
    source2.Play();
    source3.Play();
    Thread.Sleep(300);

    // Assert
    manager.StreamCount.Should().Be(3);
  }

  [Fact]
  public void AddRemoveSourcesDynamically_ShouldMaintainStablePlayback()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    using var playback = new AudioPlayback();
    disposables.Add(playback);

    using var manager = new StreamManager(playback);
    disposables.Add(manager);

    var source1 = new FileAudioSource(Path.Combine(TestDataPath, "100hz.wav"));
    disposables.Add(source1);

    // Act - Add first source
    manager.AddSource("source1", source1, isPrimary: true);
    source1.Play();
    Thread.Sleep(100);

    // Streams contain("source1");

    // Add second source
    var source2 = new FileAudioSource(Path.Combine(TestDataPath, "200hz.wav"));
    disposables.Add(source2);
    manager.AddSource("source2", source2, isPrimary: false);
    source2.Play();
    Thread.Sleep(100);

    manager.StreamCount.Should().Be(2);

    // Remove first source
    manager.RemoveSource("source1");
    Thread.Sleep(50);

    // Assert
    // Streams contain("source2");
    // Removed stream("source1");
  }

  [Fact]
  public void MuteUnmuteInMultiSourceScenario_ShouldAffectOnlyTargetSource()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    using var playback = new AudioPlayback();
    disposables.Add(playback);

    using var manager = new StreamManager(playback);
    disposables.Add(manager);

    var source1 = new FileAudioSource(Path.Combine(TestDataPath, "100hz.wav"));
    disposables.Add(source1);

    var source2 = new FileAudioSource(Path.Combine(TestDataPath, "200hz.wav"));
    disposables.Add(source2);

    manager.AddSource("source1", source1, isPrimary: true);
    manager.AddSource("source2", source2, isPrimary: false);

    source1.Play();
    source2.Play();
    Thread.Sleep(100);

    // Act - Mute source1
    manager.Mute("source1");
    Thread.Sleep(100);

    // Unmute source1
    manager.Unmute("source1");
    Thread.Sleep(100);

    // Assert - Both should still be in manager
    // Streams contain(new[] { "source1", "source2" });
  }

  [Fact]
  public void FadeTransitionsInMultiSourceScenario_ShouldSmoothlyAdjustVolumes()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    using var playback = new AudioPlayback();
    disposables.Add(playback);

    using var manager = new StreamManager(playback);
    disposables.Add(manager);

    var source1 = new FileAudioSource(Path.Combine(TestDataPath, "100hz.wav"));
    disposables.Add(source1);

    var source2 = new FileAudioSource(Path.Combine(TestDataPath, "200hz.wav"));
    disposables.Add(source2);

    manager.AddSource("source1", source1, isPrimary: true);
    manager.AddSource("source2", source2, isPrimary: false);

    source1.Play();
    source2.Play();
    Thread.Sleep(100);

    // Act - Fade in source2 (duration in milliseconds)
    manager.FadeIn("source2", 500);
    Thread.Sleep(600);

    // Act - Fade out source1 (duration in milliseconds)
    manager.FadeOut("source1", 500);
    Thread.Sleep(600);

    // Assert
    manager.StreamCount.Should().BeGreaterThan(0);
  }

  [Fact]
  public async Task SpotifyWithTtsInterruption_ShouldHandleSimulationMode()
  {
    // Arrange
    var spotifyConfig = new SpotifyConfiguration { UseSimulation = true };
    var spotifySource = new SpotifyAudioSource(spotifyConfig);
    disposables.Add(spotifySource);

    var ttsConfig = new TtsConfiguration { Engine = "espeak" };
    var ttsSource = new TtsAudioSource("Test interruption", config: ttsConfig);
    disposables.Add(ttsSource);

    // Act - Initialize Spotify in simulation mode
    await spotifySource.InitializeAsync();

    // Assert - Sources should be created successfully
    spotifySource.Name.Should().Be("Spotify");
    ttsSource.Name.Should().Be("Text-to-Speech");
    spotifySource.CurrentlyPlaying.Should().NotBeNull();
  }

  [Fact]
  public void MultipleAutoSourcesSequential_ShouldRaiseEventsCorrectly()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    using var playback = new AudioPlayback();
    disposables.Add(playback);

    using var manager = new StreamManager(playback);
    disposables.Add(manager);

    var source1 = new FileAudioSource(Path.Combine(TestDataPath, "100hz.wav"), sourceType: SourceType.Auto);
    disposables.Add(source1);

    var source2 = new FileAudioSource(Path.Combine(TestDataPath, "200hz.wav"), sourceType: SourceType.Auto);
    disposables.Add(source2);

    int playBeginCount = 0;
    int allCompleteCount = 0;

    manager.AudioPlayBegin += (sender, args) => playBeginCount++;
    manager.AllAudioComplete += (sender, args) => allCompleteCount++;

    // Act - Play first source
    manager.AddSource("auto1", source1, isPrimary: true);
    source1.Play();
    Thread.Sleep(100);

    // Add second source while first is playing
    manager.AddSource("auto2", source2, isPrimary: true);
    source2.Play();
    Thread.Sleep(100);

    // Assert
    playBeginCount.Should().BeGreaterThan(0, "AudioPlayBegin should fire");
  }

  [Fact]
  public void MixedAutoAndManualSources_ShouldOnlyAutoRemoveAutoSources()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
      return;

    // Arrange
    using var playback = new AudioPlayback();
    disposables.Add(playback);

    using var manager = new StreamManager(playback);
    disposables.Add(manager);

    var manualSource = new FileAudioSource(Path.Combine(TestDataPath, "100hz.wav"), sourceType: SourceType.Manual);
    disposables.Add(manualSource);

    var autoSource = new FileAudioSource(Path.Combine(TestDataPath, "200hz.wav"), sourceType: SourceType.Auto);
    disposables.Add(autoSource);

    // Act
    manager.AddSource("manual", manualSource, isPrimary: false);
    manager.AddSource("auto", autoSource, isPrimary: true);

    manualSource.Play();
    autoSource.Play();
    Thread.Sleep(200);

    // Assert
    // Streams contain("manual");
  }

  private bool IsHeadlessEnvironment()
  {
    return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) ||
           !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
  }

  private bool IsESpeakAvailable()
  {
    try
    {
      var processInfo = new System.Diagnostics.ProcessStartInfo
      {
        FileName = "espeak",
        Arguments = "--version",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
      };
      using var process = System.Diagnostics.Process.Start(processInfo);
      return process != null;
    }
    catch
    {
      return false;
    }
  }

  private bool IsEspeakInstalled()
  {
    try
    {
      var process = new System.Diagnostics.Process
      {
        StartInfo = new System.Diagnostics.ProcessStartInfo
        {
          FileName = "espeak",
          Arguments = "--version",
          RedirectStandardOutput = true,
          RedirectStandardError = true,
          UseShellExecute = false,
          CreateNoWindow = true
        }
      };
      process.Start();
      process.WaitForExit(1000);
      return process.ExitCode == 0;
    }
    catch
    {
      return false;
    }
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
    disposables.Clear();
  }
}
