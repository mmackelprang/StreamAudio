using FluentAssertions;
using StreamAudio.Core.Sources;
using StreamAudio.Core.Audio;

namespace StreamAudio.Tests;

[Collection("AudioTests")]
public class TtsAudioSourceTests : IDisposable
{
  private readonly List<IDisposable> disposables = new();

  [Fact]
  public void Constructor_WithValidText_ShouldInitialize()
  {
    // Arrange
    string text = "Hello world";

    // Act
    var source = new TtsAudioSource(text);
    disposables.Add(source);

    // Assert
    source.Name.Should().Be("Text-to-Speech");
    source.SampleRate.Should().BeGreaterThan(0);
    source.Channels.Should().BeGreaterThan(0);
    source.SourceType.Should().Be(SourceType.Auto);
    source.RepeatCount.Should().Be(1);
    source.CurrentlyPlaying.Should().BeNull(); // TTS doesn't have metadata
  }

  [Fact]
  public void Constructor_WithNullText_ShouldThrowArgumentException()
  {
    // Act
    Action act = () => new TtsAudioSource(null!);

    // Assert
    act.Should().Throw<ArgumentException>()
      .WithParameterName("text");
  }

  [Fact]
  public void Constructor_WithEmptyText_ShouldThrowArgumentException()
  {
    // Act
    Action act = () => new TtsAudioSource("");

    // Assert
    act.Should().Throw<ArgumentException>()
      .WithParameterName("text");
  }

  [Fact]
  public void Constructor_WithWhitespaceText_ShouldThrowArgumentException()
  {
    // Act
    Action act = () => new TtsAudioSource("   ");

    // Assert
    act.Should().Throw<ArgumentException>()
      .WithParameterName("text");
  }

  [Fact]
  public void Constructor_WithCustomConfiguration_ShouldUseConfiguration()
  {
    // Arrange
    var config = new TtsConfiguration
    {
      Engine = "espeak",
      Voice = "en-us",
      Rate = 1.5,
      Pitch = 0.2,
      Volume = 0.8
    };

    // Act
    var source = new TtsAudioSource("Test text", config: config);
    disposables.Add(source);

    // Assert
    source.Name.Should().Be("Text-to-Speech");
  }

  [Fact]
  public void GenerateSpeech_WithEspeakEngine_ShouldSucceed()
  {
    // Skip if espeak is not installed
    if (!IsEspeakInstalled())
    {
      return;
    }

    // Arrange
    var config = new TtsConfiguration { Engine = "espeak" };
    var source = new TtsAudioSource("Test speech", config: config);
    disposables.Add(source);

    // Act & Assert (should not throw)
    source.State.Should().NotBe(SoundFlow.Enums.PlaybackState.Playing);
  }

  [Fact]
  public void GenerateSpeech_WithGoogleEngine_WithoutApiKey_ShouldThrowInvalidOperationException()
  {
    // Arrange
    var config = new TtsConfiguration { Engine = "google" };
    var source = new TtsAudioSource("Test text", config: config);
    disposables.Add(source);

    // Act
    Action act = () => source.Play();

    // Assert
    act.Should().Throw<InvalidOperationException>()
      .WithMessage("*GoogleApiKey*");
  }

  [Fact]
  public void GenerateSpeech_WithAzureEngine_WithoutCredentials_ShouldThrowInvalidOperationException()
  {
    // Arrange
    var config = new TtsConfiguration { Engine = "azure" };
    var source = new TtsAudioSource("Test text", config: config);
    disposables.Add(source);

    // Act
    Action act = () => source.Play();

    // Assert
    act.Should().Throw<InvalidOperationException>()
      .WithMessage("*AzureSpeechKey*");
  }

  [Fact]
  public void GenerateSpeech_WithInvalidEngine_ShouldThrowNotSupportedException()
  {
    // Arrange
    var config = new TtsConfiguration { Engine = "invalid_engine" };
    var source = new TtsAudioSource("Test text", config: config);
    disposables.Add(source);

    // Act
    Action act = () => source.Play();

    // Assert
    act.Should().Throw<NotSupportedException>()
      .WithMessage("*not supported*");
  }

  [Theory]
  [InlineData(0.5)]
  [InlineData(1.0)]
  [InlineData(1.5)]
  [InlineData(2.0)]
  public void Configuration_RateProperty_ShouldAcceptValidValues(double rate)
  {
    // Arrange & Act
    var config = new TtsConfiguration { Rate = rate };

    // Assert
    config.Rate.Should().Be(rate);
  }

  [Theory]
  [InlineData(-1.0)]
  [InlineData(-0.5)]
  [InlineData(0.0)]
  [InlineData(0.5)]
  [InlineData(1.0)]
  public void Configuration_PitchProperty_ShouldAcceptValidValues(double pitch)
  {
    // Arrange & Act
    var config = new TtsConfiguration { Pitch = pitch };

    // Assert
    config.Pitch.Should().Be(pitch);
  }

  [Theory]
  [InlineData(0.0)]
  [InlineData(0.5)]
  [InlineData(1.0)]
  public void Configuration_VolumeProperty_ShouldAcceptValidValues(double volume)
  {
    // Arrange & Act
    var config = new TtsConfiguration { Volume = volume };

    // Assert
    config.Volume.Should().Be(volume);
  }

  [Fact]
  public void SourceType_ShouldDefaultToAuto()
  {
    // Arrange & Act
    var source = new TtsAudioSource("Test");
    disposables.Add(source);

    // Assert
    source.SourceType.Should().Be(SourceType.Auto);
  }

  [Fact]
  public void RepeatCount_ShouldDefaultToOne()
  {
    // Arrange & Act
    var source = new TtsAudioSource("Test");
    disposables.Add(source);

    // Assert
    source.RepeatCount.Should().Be(1);
  }

  [Fact]
  public void CurrentlyPlaying_ShouldReturnNull()
  {
    // Arrange & Act
    var source = new TtsAudioSource("Test");
    disposables.Add(source);

    // Assert (TTS doesn't have song metadata)
    source.CurrentlyPlaying.Should().BeNull();
  }

  [Fact]
  public void Dispose_ShouldCleanupResources()
  {
    // Arrange
    var source = new TtsAudioSource("Test");

    // Act
    source.Dispose();

    // Assert (should not throw)
    source.Dispose(); // Second dispose should be safe
  }

  [Fact]
  public void DefaultConfiguration_ShouldHaveExpectedDefaults()
  {
    // Arrange & Act
    var config = new TtsConfiguration();

    // Assert
    config.Engine.Should().Be("espeak");
    config.Voice.Should().BeNull();
    config.Rate.Should().Be(1.0);
    config.Pitch.Should().Be(0.0);
    config.Volume.Should().Be(1.0);
    config.GoogleApiKey.Should().BeNull();
    config.AzureSpeechKey.Should().BeNull();
    config.AzureSpeechRegion.Should().BeNull();
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
      disposable?.Dispose();
    }
    disposables.Clear();
  }
}
