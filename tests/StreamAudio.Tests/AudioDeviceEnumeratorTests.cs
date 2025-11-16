using StreamAudio.Core.Platform;
using Xunit;
using FluentAssertions;
using System;

namespace StreamAudio.Tests;

public class AudioDeviceEnumeratorTests
{
  [Fact]
  public void GetPlaybackDevices_ShouldNotThrow()
  {
    var action = () => AudioDeviceEnumerator.GetPlaybackDevices();
    
    action.Should().NotThrow();
  }

  [Fact]
  public void GetPlaybackDevices_ShouldReturnList()
  {
    var devices = AudioDeviceEnumerator.GetPlaybackDevices();
    
    devices.Should().NotBeNull();
    // May be empty in headless environment, but should be a valid list
  }

  [Fact]
  public void GetCaptureDevices_ShouldNotThrow()
  {
    var action = () => AudioDeviceEnumerator.GetCaptureDevices();
    
    action.Should().NotThrow();
  }

  [Fact]
  public void GetCaptureDevices_ShouldReturnList()
  {
    var devices = AudioDeviceEnumerator.GetCaptureDevices();
    
    devices.Should().NotBeNull();
    // May be empty in headless environment, but should be a valid list
  }

  [Fact]
  public void GetAllDevices_ShouldIncludeBothTypes()
  {
    var allDevices = AudioDeviceEnumerator.GetAllDevices();
    var playbackDevices = AudioDeviceEnumerator.GetPlaybackDevices();
    var captureDevices = AudioDeviceEnumerator.GetCaptureDevices();
    
    allDevices.Should().NotBeNull();
    allDevices.Count.Should().Be(playbackDevices.Count + captureDevices.Count);
  }

  [Fact]
  public void GetDefaultPlaybackDevice_ShouldNotThrow()
  {
    var action = () => AudioDeviceEnumerator.GetDefaultPlaybackDevice();
    
    action.Should().NotThrow();
  }

  [Fact]
  public void GetDefaultCaptureDevice_ShouldNotThrow()
  {
    var action = () => AudioDeviceEnumerator.GetDefaultCaptureDevice();
    
    action.Should().NotThrow();
  }

  [Fact]
  public void AudioDeviceInfo_ToString_ShouldFormatCorrectly()
  {
    var device = new AudioDeviceInfo
    {
      Id = IntPtr.Zero,
      Name = "Test Device",
      IsPlayback = true,
      IsDefault = true,
      DeviceType = "USB"
    };

    var str = device.ToString();
    
    str.Should().Contain("Test Device");
    str.Should().Contain("USB");
    str.Should().Contain("Default");
    str.Should().Contain("Playback");
  }

  [Fact]
  public void PlaybackDevices_ShouldHaveCorrectFlags()
  {
    var devices = AudioDeviceEnumerator.GetPlaybackDevices();
    
    foreach (var device in devices)
    {
      device.IsPlayback.Should().BeTrue();
      device.IsCapture.Should().BeFalse();
    }
  }

  [Fact]
  public void CaptureDevices_ShouldHaveCorrectFlags()
  {
    var devices = AudioDeviceEnumerator.GetCaptureDevices();
    
    foreach (var device in devices)
    {
      device.IsPlayback.Should().BeFalse();
      device.IsCapture.Should().BeTrue();
    }
  }
}
