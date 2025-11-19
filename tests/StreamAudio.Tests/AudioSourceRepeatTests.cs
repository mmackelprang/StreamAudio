using FluentAssertions;
using StreamAudio.Core.Sources;
using StreamAudio.Core.Playback;
using System.IO;

namespace StreamAudio.Tests;

/// <summary>
/// Tests for RepeatCount and Loop behavior in audio sources.
/// </summary>
[Collection("AudioTests")]
public class AudioSourceRepeatTests : IDisposable
{
  private const string TestDataPath = "../../../../../testdata";
  private readonly List<IDisposable> disposables = new();

  [Fact]
  public void FileAudioSource_WithRepeatCountZero_ShouldLoopInfinitely()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    var source = new FileAudioSource(testFile)
    {
      RepeatCount = 0 // Infinite loop
    };
    disposables.Add(source);

    using var playback = new AudioPlayback();
    playback.AddPlayer(source.Player);

    // Act
    source.Play();
    Thread.Sleep(100); // Let it start playing

    // Wait for file to finish and loop multiple times
    // The 100hz.wav file is 1 second long
    Thread.Sleep(2500); // Wait 2.5 seconds to see if it loops

    // Assert
    // With RepeatCount = 0, it should keep playing (not stop)
    source.State.Should().NotBe(SoundFlow.Enums.PlaybackState.Stopped,
      "RepeatCount = 0 should cause infinite looping");
  }

  [Fact]
  public void FileAudioSource_WithRepeatCountTwo_ShouldPlayTwice()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    var source = new FileAudioSource(testFile)
    {
      RepeatCount = 2 // Play twice
    };
    disposables.Add(source);

    using var playback = new AudioPlayback();
    playback.AddPlayer(source.Player);

    // Act
    source.Play();
    Thread.Sleep(100); // Let it start playing

    // Wait for file to play twice and stop
    // The 100hz.wav file is 1 second long
    Thread.Sleep(2300); // Wait 2.3 seconds - should play twice and stop

    // Assert
    // With RepeatCount = 2, it should have stopped after playing twice
    source.State.Should().Be(SoundFlow.Enums.PlaybackState.Stopped,
      "RepeatCount = 2 should stop after playing twice");
  }

  [Fact]
  public void FileAudioSource_WithRepeatCountOne_ShouldPlayOnce()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    var source = new FileAudioSource(testFile)
    {
      RepeatCount = 1 // Play once (default)
    };
    disposables.Add(source);

    using var playback = new AudioPlayback();
    playback.AddPlayer(source.Player);

    // Act
    source.Play();
    Thread.Sleep(100); // Let it start playing

    // Wait for file to finish
    // The 100hz.wav file is 1 second long
    Thread.Sleep(1200); // Wait 1.2 seconds

    // Assert
    // With RepeatCount = 1, it should stop after playing once
    source.State.Should().Be(SoundFlow.Enums.PlaybackState.Stopped,
      "RepeatCount = 1 should stop after playing once");
  }

  [Fact]
  public void FileAudioSource_WithLoopTrue_ShouldLoop()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    var source = new FileAudioSource(testFile)
    {
      Loop = true,
      RepeatCount = 1 // Even with RepeatCount = 1, Loop = true should cause looping
    };
    disposables.Add(source);

    using var playback = new AudioPlayback();
    playback.AddPlayer(source.Player);

    // Act
    source.Play();
    Thread.Sleep(100); // Let it start playing

    // Wait for file to finish and loop
    // The 100hz.wav file is 1 second long
    Thread.Sleep(2500); // Wait 2.5 seconds to see if it loops

    // Assert
    // With Loop = true, it should keep playing
    source.State.Should().NotBe(SoundFlow.Enums.PlaybackState.Stopped,
      "Loop = true should cause looping regardless of RepeatCount");
  }

  [Fact]
  public void TtsAudioSource_WithRepeatCountZero_ShouldLoopInfinitely()
  {
    // Skip audio tests in headless environment or if espeak is not installed
    if (IsHeadlessEnvironment() || !IsEspeakInstalled())
    {
      return;
    }

    // Arrange
    var source = new TtsAudioSource("Test")
    {
      RepeatCount = 0 // Infinite loop
    };
    disposables.Add(source);

    using var playback = new AudioPlayback();
    playback.AddPlayer(source.Player);

    // Act
    source.Play();
    Thread.Sleep(500); // Let it start playing

    // Wait for TTS to finish and loop
    Thread.Sleep(3000); // Wait 3 seconds to see if it loops

    // Assert
    // With RepeatCount = 0, it should keep playing (not stop)
    source.State.Should().NotBe(SoundFlow.Enums.PlaybackState.Stopped,
      "RepeatCount = 0 should cause infinite looping");
  }

  [Fact]
  public void TtsAudioSource_WithRepeatCountTwo_ShouldPlayTwice()
  {
    // Skip audio tests in headless environment or if espeak is not installed
    if (IsHeadlessEnvironment() || !IsEspeakInstalled())
    {
      return;
    }

    // Arrange
    var source = new TtsAudioSource("Test")
    {
      RepeatCount = 2 // Play twice
    };
    disposables.Add(source);

    using var playback = new AudioPlayback();
    playback.AddPlayer(source.Player);

    // Act
    source.Play();
    Thread.Sleep(500); // Let it start playing

    // Wait for TTS to play twice and stop
    Thread.Sleep(3500); // Wait 3.5 seconds - should play twice and stop

    // Assert
    // With RepeatCount = 2, it should have stopped after playing twice
    source.State.Should().Be(SoundFlow.Enums.PlaybackState.Stopped,
      "RepeatCount = 2 should stop after playing twice");
  }

  [Fact]
  public void TtsAudioSource_WithRepeatCountOne_ShouldPlayOnce()
  {
    // Skip audio tests in headless environment or if espeak is not installed
    if (IsHeadlessEnvironment() || !IsEspeakInstalled())
    {
      return;
    }

    // Arrange
    var source = new TtsAudioSource("Test")
    {
      RepeatCount = 1 // Play once (default)
    };
    disposables.Add(source);

    using var playback = new AudioPlayback();
    playback.AddPlayer(source.Player);

    // Act
    source.Play();
    Thread.Sleep(500); // Let it start playing

    // Wait for TTS to finish
    Thread.Sleep(2000); // Wait 2 seconds

    // Assert
    // With RepeatCount = 1, it should stop after playing once
    source.State.Should().Be(SoundFlow.Enums.PlaybackState.Stopped,
      "RepeatCount = 1 should stop after playing once");
  }

  [Fact]
  public void TtsAudioSource_WithLoopTrue_ShouldLoop()
  {
    // Skip audio tests in headless environment or if espeak is not installed
    if (IsHeadlessEnvironment() || !IsEspeakInstalled())
    {
      return;
    }

    // Arrange
    var source = new TtsAudioSource("Test")
    {
      Loop = true,
      RepeatCount = 1 // Even with RepeatCount = 1, Loop = true should cause looping
    };
    disposables.Add(source);

    using var playback = new AudioPlayback();
    playback.AddPlayer(source.Player);

    // Act
    source.Play();
    Thread.Sleep(500); // Let it start playing

    // Wait for TTS to finish and loop
    Thread.Sleep(3000); // Wait 3 seconds to see if it loops

    // Assert
    // With Loop = true, it should keep playing
    source.State.Should().NotBe(SoundFlow.Enums.PlaybackState.Stopped,
      "Loop = true should cause looping regardless of RepeatCount");
  }

  private bool IsHeadlessEnvironment()
  {
    // Check for CI environment variables
    return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) ||
           !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
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
        // Ignore disposal errors for individual resources
      }
    }
    disposables.Clear();
  }
}
