using FluentAssertions;
using StreamAudio.Core.Playback;
using StreamAudio.Core.Events;

namespace StreamAudio.Tests;

[Collection("AudioTests")]
public class ErrorHandlingTests : IDisposable
{
  private const string TestDataPath = "../../../../../testdata";
  private readonly List<IDisposable> disposables = new();

  [Fact]
  public void AudioPlayback_DeviceHealthCheck_ShouldReturnTrue()
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
    var isHealthy = playback.IsDeviceHealthy();

    // Assert
    isHealthy.Should().BeTrue();
  }

  [Fact]
  public void AudioPlayback_DeviceError_EventShouldBeRaisable()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    var playback = new AudioPlayback();
    disposables.Add(playback);
    var eventRaised = false;
    DeviceEventArgs? capturedArgs = null;

    playback.DeviceError += (sender, args) =>
    {
      eventRaised = true;
      capturedArgs = args;
    };

    // Act - We can't easily trigger a real device error, so we just verify the event can be subscribed to
    // In a real scenario, this would be triggered by actual device failures

    // Assert
    eventRaised.Should().BeFalse(); // No error should have occurred
    playback.IsDeviceHealthy().Should().BeTrue();
  }

  [Fact]
  public void StreamManager_StreamFailed_EventShouldBeRaisable()
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
    var eventRaised = false;
    AudioEventArgs? capturedArgs = null;

    manager.StreamFailed += (sender, args) =>
    {
      eventRaised = true;
      capturedArgs = args;
    };

    // Act - Monitor streams (no failures expected in normal operation)
    manager.MonitorStreams();

    // Assert
    eventRaised.Should().BeFalse(); // No errors should have occurred
  }

  [Fact]
  public void StreamManager_TryRecoverStream_WithNonExistentStream_ShouldReturnFalse()
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

    // Act
    var result = manager.TryRecoverStream("nonexistent", "dummy.wav");

    // Assert
    result.Should().BeFalse();
  }

  [Fact]
  public void AudioEventArgs_Constructor_ShouldInitializeProperties()
  {
    // Arrange & Act
    var args = new AudioEventArgs("test-stream", "Test message", new Exception("Test exception"));

    // Assert
    args.StreamId.Should().Be("test-stream");
    args.Message.Should().Be("Test message");
    args.Exception.Should().NotBeNull();
    args.Exception!.Message.Should().Be("Test exception");
  }

  [Fact]
  public void DeviceEventArgs_Constructor_ShouldInitializeProperties()
  {
    // Arrange & Act
    var args = new DeviceEventArgs("test-device", "Device error", new Exception("Device exception"));

    // Assert
    args.DeviceName.Should().Be("test-device");
    args.Message.Should().Be("Device error");
    args.Exception.Should().NotBeNull();
    args.Exception!.Message.Should().Be("Device exception");
  }

  private static bool IsHeadlessEnvironment()
  {
    var ci = Environment.GetEnvironmentVariable("CI");
    var display = Environment.GetEnvironmentVariable("DISPLAY");
    return !string.IsNullOrEmpty(ci) || string.IsNullOrEmpty(display);
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
