using StreamAudio.Core.Platform;
using Xunit;
using FluentAssertions;

namespace StreamAudio.Tests;

public class AudioConfigurationTests
{
  [Fact]
  public void CreateDefault_ShouldReturnValidConfiguration()
  {
    var config = AudioConfiguration.CreateDefault();
    
    config.Should().NotBeNull();
    config.Format.Should().NotBeNull();
    config.BufferSizeInFrames.Should().BeGreaterThan(0);
  }

  [Fact]
  public void CreateForRaspberryPi_ShouldUseLargerBuffer()
  {
    var config = AudioConfiguration.CreateForRaspberryPi();
    
    config.Should().NotBeNull();
    config.BufferSizeInFrames.Should().Be(2048, "Raspberry Pi should use larger buffer for stability");
    config.LowLatencyMode.Should().BeFalse();
  }

  [Fact]
  public void CreateLowLatency_ShouldUseSmallBuffer()
  {
    var config = AudioConfiguration.CreateLowLatency();
    
    config.Should().NotBeNull();
    config.BufferSizeInFrames.Should().Be(256, "Low latency mode should use small buffer");
    config.LowLatencyMode.Should().BeTrue();
  }

  [Fact]
  public void GetDescription_ShouldReturnFormattedString()
  {
    var config = AudioConfiguration.CreateDefault();
    var description = config.GetDescription();
    
    description.Should().NotBeNullOrEmpty();
    description.Should().Contain("Hz");
    description.Should().Contain("ch");
    description.Should().Contain("Buffer");
    description.Should().Contain("frames");
  }

  [Fact]
  public void Constructor_ShouldInitializeDefaults()
  {
    var config = new AudioConfiguration();
    
    config.Format.Should().NotBeNull();
    config.BufferSizeInFrames.Should().BeGreaterThan(0);
    config.LowLatencyMode.Should().BeFalse();
  }
}
