using FluentAssertions;
using StreamAudio.Core.Sources;
using StreamAudio.Core.Audio;

namespace StreamAudio.Tests;

[Collection("AudioTests")]
public class UsbAudioSourceTests : IDisposable
{
  private readonly List<IDisposable> disposables = new();

  [Fact]
  public void Constructor_WithValidConfiguration_ShouldInitialize()
  {
    // Arrange
    var config = new UsbAudioConfiguration
    {
      DeviceName = "Test USB Device"
    };

    // Act
    var source = new UsbAudioSource(config);
    disposables.Add(source);

    // Assert
    source.Name.Should().Be("Test USB Device");
    source.SampleRate.Should().Be(44100); // Default
    source.Channels.Should().Be(2); // Default
    source.SourceType.Should().Be(SourceType.Manual);
    source.RepeatCount.Should().Be(0); // Infinite for continuous capture
  }

  [Fact]
  public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
  {
    // Act
    Action act = () => new UsbAudioSource(null!);

    // Assert
    act.Should().Throw<ArgumentNullException>()
      .WithParameterName("config");
  }

  [Fact]
  public void Constructor_WithCustomSampleRate_ShouldUseConfiguredValue()
  {
    // Arrange
    var config = new UsbAudioConfiguration
    {
      SampleRate = 48000
    };

    // Act
    var source = new UsbAudioSource(config);
    disposables.Add(source);

    // Assert
    source.SampleRate.Should().Be(48000);
  }

  [Fact]
  public void Constructor_WithCustomChannels_ShouldUseConfiguredValue()
  {
    // Arrange
    var config = new UsbAudioConfiguration
    {
      Channels = 1 // Mono
    };

    // Act
    var source = new UsbAudioSource(config);
    disposables.Add(source);

    // Assert
    source.Channels.Should().Be(1);
  }

  [Fact]
  public void SourceType_ShouldBeManual()
  {
    // Arrange
    var config = new UsbAudioConfiguration();
    var source = new UsbAudioSource(config);
    disposables.Add(source);

    // Assert
    source.SourceType.Should().Be(SourceType.Manual);
  }

  [Fact]
  public void RepeatCount_ShouldBeZero()
  {
    // Arrange
    var config = new UsbAudioConfiguration();
    var source = new UsbAudioSource(config);
    disposables.Add(source);

    // Assert (0 = infinite for continuous capture)
    source.RepeatCount.Should().Be(0);
  }

  [Fact]
  public void CurrentlyPlaying_ShouldReturnNull()
  {
    // Arrange
    var config = new UsbAudioConfiguration();
    var source = new UsbAudioSource(config);
    disposables.Add(source);

    // Assert (USB capture doesn't have metadata)
    source.CurrentlyPlaying.Should().BeNull();
  }

  [Fact]
  public void Configuration_DefaultValues_ShouldBeCorrect()
  {
    // Arrange & Act
    var config = new UsbAudioConfiguration();

    // Assert
    config.DeviceNumber.Should().Be(-1); // Default device
    config.DeviceName.Should().Be("USB Audio Device");
    config.SampleRate.Should().Be(44100);
    config.Channels.Should().Be(2);
    config.BitsPerSample.Should().Be(16);
    config.BufferMilliseconds.Should().Be(100);
  }

  [Fact]
  public void Configuration_CustomValues_ShouldBeAccepted()
  {
    // Arrange & Act
    var config = new UsbAudioConfiguration
    {
      DeviceNumber = 1,
      DeviceName = "Custom USB Device",
      SampleRate = 48000,
      Channels = 1,
      BitsPerSample = 24,
      BufferMilliseconds = 200
    };

    // Assert
    config.DeviceNumber.Should().Be(1);
    config.DeviceName.Should().Be("Custom USB Device");
    config.SampleRate.Should().Be(48000);
    config.Channels.Should().Be(1);
    config.BitsPerSample.Should().Be(24);
    config.BufferMilliseconds.Should().Be(200);
  }

  [Theory]
  [InlineData(44100)]
  [InlineData(48000)]
  [InlineData(96000)]
  public void Configuration_CommonSampleRates_ShouldBeAccepted(int sampleRate)
  {
    // Arrange & Act
    var config = new UsbAudioConfiguration
    {
      SampleRate = sampleRate
    };

    // Assert
    config.SampleRate.Should().Be(sampleRate);
  }

  [Theory]
  [InlineData(1)] // Mono
  [InlineData(2)] // Stereo
  public void Configuration_CommonChannelCounts_ShouldBeAccepted(int channels)
  {
    // Arrange & Act
    var config = new UsbAudioConfiguration
    {
      Channels = channels
    };

    // Assert
    config.Channels.Should().Be(channels);
  }

  [Theory]
  [InlineData(16)]
  [InlineData(24)]
  [InlineData(32)]
  public void Configuration_CommonBitDepths_ShouldBeAccepted(int bitsPerSample)
  {
    // Arrange & Act
    var config = new UsbAudioConfiguration
    {
      BitsPerSample = bitsPerSample
    };

    // Assert
    config.BitsPerSample.Should().Be(bitsPerSample);
  }

  [Theory]
  [InlineData(50)]
  [InlineData(100)]
  [InlineData(200)]
  public void Configuration_CommonBufferSizes_ShouldBeAccepted(int bufferMs)
  {
    // Arrange & Act
    var config = new UsbAudioConfiguration
    {
      BufferMilliseconds = bufferMs
    };

    // Assert
    config.BufferMilliseconds.Should().Be(bufferMs);
  }

  [Fact]
  public void State_BeforePlay_ShouldBeStopped()
  {
    // Arrange
    var config = new UsbAudioConfiguration();
    var source = new UsbAudioSource(config);
    disposables.Add(source);

    // Act
    var state = source.State;

    // Assert
    state.Should().Be(SoundFlow.Enums.PlaybackState.Stopped);
  }

  [Fact]
  public void Name_ShouldMatchConfiguredDeviceName()
  {
    // Arrange
    var config = new UsbAudioConfiguration
    {
      DeviceName = "My Radio Receiver"
    };

    // Act
    var source = new UsbAudioSource(config);
    disposables.Add(source);

    // Assert
    source.Name.Should().Be("My Radio Receiver");
  }

  [Fact]
  public void Dispose_ShouldCleanupResources()
  {
    // Arrange
    var config = new UsbAudioConfiguration();
    var source = new UsbAudioSource(config);

    // Act
    source.Dispose();

    // Assert (should not throw)
    source.Dispose(); // Second dispose should be safe
  }

  [Fact]
  public void Format_ShouldNotBeNull()
  {
    // Arrange
    var config = new UsbAudioConfiguration();
    var source = new UsbAudioSource(config);
    disposables.Add(source);

    // Act
    var format = source.Format;

    // Assert
    format.Should().NotBeNull();
  }

  [Fact]
  public void RepeatCount_ShouldBeSettable()
  {
    // Arrange
    var config = new UsbAudioConfiguration();
    var source = new UsbAudioSource(config);
    disposables.Add(source);

    // Act
    source.RepeatCount = 5;

    // Assert
    source.RepeatCount.Should().Be(5);
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
