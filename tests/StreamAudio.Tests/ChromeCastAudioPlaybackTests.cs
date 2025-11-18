using FluentAssertions;
using StreamAudio.Core.Playback;
using StreamAudio.Core.Storage;
using StreamAudio.Core.Audio;
using SoundFlow.Structs;

namespace StreamAudio.Tests;

[Collection("AudioTests")]
public class ChromeCastAudioPlaybackTests : IDisposable
{
  private readonly List<IDisposable> disposables = new();
  private readonly string _testDir;
  private readonly IStorage _testStorage;

  public ChromeCastAudioPlaybackTests()
  {
    _testDir = Path.Combine(Path.GetTempPath(), $"streamaudio_chromecast_test_{Guid.NewGuid():N}");
    Directory.CreateDirectory(_testDir);
    
    var storageDir = Path.Combine(_testDir, "storage");
    var backupDir = Path.Combine(_testDir, "backup");
    _testStorage = new JsonFileStorage(storageDir, backupDir);
    
    // Temporarily replace the global storage with our test storage
    StorageManager.Instance.SetStorage(_testStorage);
  }

  [Fact]
  public void Constructor_WithValidDeviceName_ShouldInitialize()
  {
    // Arrange & Act
    var chromecast = new ChromeCastAudioPlayback("Living Room Speaker");
    disposables.Add(chromecast);

    // Assert
    chromecast.Should().NotBeNull();
    chromecast.Format.Should().Be(AudioFormat.DvdHq);
  }

  [Fact]
  public void Constructor_WithNullDeviceName_ShouldThrow()
  {
    // Arrange & Act
    Action act = () => new ChromeCastAudioPlayback(null!);

    // Assert
    act.Should().Throw<ArgumentNullException>();
  }

  [Fact]
  public void Constructor_WithCustomFormat_ShouldUseCustomFormat()
  {
    // Arrange
    var format = AudioFormat.DvdHq;

    // Act
    var chromecast = new ChromeCastAudioPlayback("Living Room Speaker", null, format);
    disposables.Add(chromecast);

    // Assert
    chromecast.Format.Should().Be(format);
  }

  [Fact]
  public async Task SaveConfigurationAsync_ShouldStoreInStorage()
  {
    // Arrange
    var chromecast = new ChromeCastAudioPlayback("Living Room Speaker", "device-123");
    disposables.Add(chromecast);

    // Act
    await chromecast.SaveConfigurationAsync("living-room");

    // Assert - use the same test storage instance
    var config = await _testStorage.LoadAsync<ChromeCastConfiguration>("ChromeCast", "living-room");
    config.Should().NotBeNull();
    config!.DeviceName.Should().Be("Living Room Speaker");
    config.DeviceId.Should().Be("device-123");
  }

  [Fact]
  public async Task FromStorageAsync_WithValidConfig_ShouldLoad()
  {
    // Arrange
    var config = new ChromeCastConfiguration
    {
      DeviceName = "Kitchen Speaker",
      DeviceId = "kitchen-001"
    };
    await _testStorage.SaveAsync("ChromeCast", "kitchen", config);

    // Act
    var chromecast = await ChromeCastAudioPlayback.FromStorageAsync("kitchen");
    disposables.Add(chromecast);

    // Assert
    chromecast.Should().NotBeNull();
    chromecast.Format.Should().Be(AudioFormat.DvdHq);
  }

  [Fact]
  public async Task FromStorageAsync_WithNonExistentConfig_ShouldThrow()
  {
    // Arrange & Act
    Func<Task> act = async () => await ChromeCastAudioPlayback.FromStorageAsync("non-existent");

    // Assert
    await act.Should().ThrowAsync<InvalidOperationException>()
      .WithMessage("*not found*");
  }

  [Fact]
  public void SendMetadata_WithValidMetadata_ShouldNotThrow()
  {
    // Arrange
    var chromecast = new ChromeCastAudioPlayback("Living Room Speaker");
    disposables.Add(chromecast);

    var metadata = new SongMetadata
    {
      Title = "Test Song",
      Artist = "Test Artist",
      Album = "Test Album",
      Band = "FM",
      FrequencyHz = 95300000
    };

    // Act
    Action act = () => chromecast.SendMetadata(metadata);

    // Assert
    act.Should().NotThrow();
  }

  [Fact]
  public void SendMetadata_WithNullMetadata_ShouldThrow()
  {
    // Arrange
    var chromecast = new ChromeCastAudioPlayback("Living Room Speaker");
    disposables.Add(chromecast);

    // Act
    Action act = () => chromecast.SendMetadata(null!);

    // Assert
    act.Should().Throw<ArgumentNullException>();
  }

  [Fact]
  public void IsDeviceHealthy_WhenNotDisposed_ShouldReturnTrue()
  {
    // Arrange
    var chromecast = new ChromeCastAudioPlayback("Living Room Speaker");
    disposables.Add(chromecast);

    // Act
    var isHealthy = chromecast.IsDeviceHealthy();

    // Assert
    isHealthy.Should().BeTrue();
  }

  [Fact]
  public void IsDeviceHealthy_WhenDisposed_ShouldReturnFalse()
  {
    // Arrange
    var chromecast = new ChromeCastAudioPlayback("Living Room Speaker");
    chromecast.Dispose();

    // Act
    var isHealthy = chromecast.IsDeviceHealthy();

    // Assert
    isHealthy.Should().BeFalse();
  }

  [Fact]
  public void TryRestartDevice_ShouldSucceed()
  {
    // Arrange
    var chromecast = new ChromeCastAudioPlayback("Living Room Speaker");
    disposables.Add(chromecast);

    // Act
    var result = chromecast.TryRestartDevice();

    // Assert
    result.Should().BeTrue();
  }

  [Fact]
  public void Stop_ShouldNotThrow()
  {
    // Arrange
    var chromecast = new ChromeCastAudioPlayback("Living Room Speaker");
    disposables.Add(chromecast);

    // Act
    Action act = () => chromecast.Stop();

    // Assert
    act.Should().NotThrow();
  }

  [Fact]
  public void Mixer_ShouldThrowNotSupportedException()
  {
    // Arrange
    var chromecast = new ChromeCastAudioPlayback("Living Room Speaker");
    disposables.Add(chromecast);

    // Act
    Action act = () => { var mixer = chromecast.Mixer; };

    // Assert
    act.Should().Throw<NotSupportedException>()
      .WithMessage("*do not expose a local mixer*");
  }

  public void Dispose()
  {
    foreach (var disposable in disposables)
    {
      disposable?.Dispose();
    }
    disposables.Clear();
    
    // Reset the global storage manager
    StorageManager.Reset();
    _testStorage?.Dispose();
    
    if (Directory.Exists(_testDir))
    {
      try
      {
        Directory.Delete(_testDir, true);
      }
      catch
      {
        // Ignore cleanup errors
      }
    }
  }
}
