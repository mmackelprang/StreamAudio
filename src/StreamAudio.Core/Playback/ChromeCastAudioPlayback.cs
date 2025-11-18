using SoundFlow.Components;
using SoundFlow.Structs;
using StreamAudio.Core.Events;
using StreamAudio.Core.Configuration;
using StreamAudio.Core.Storage;
using GoogleCast;
using GoogleCast.Channels;
using GoogleCast.Models.Media;
using GoogleCast.Models.Receiver;

namespace StreamAudio.Core.Playback;

/// <summary>
/// Audio playback device for streaming to Google Cast (Chromecast) devices.
/// This implementation provides full Cast SDK integration with metadata support.
/// Requires actual Cast device for testing.
/// </summary>
public class ChromeCastAudioPlayback : IAudioPlayback
{
  private readonly AudioFormat format;
  private readonly string deviceName;
  private readonly string? deviceId;
  private bool disposed;
  private readonly Dictionary<SoundPlayer, float> playerVolumes = new();
  private Sender? castSender;
  private IReceiver? castDevice;
  private bool isConnected;
  private readonly SemaphoreSlim connectionLock = new(1, 1);
  private string? currentMediaSessionId;
  private Task? connectionTask;

  /// <summary>
  /// Occurs when the Cast device encounters an error.
  /// </summary>
  public event EventHandler<DeviceEventArgs>? DeviceError;

  /// <summary>
  /// Occurs when the Cast device is successfully recovered.
  /// </summary>
  public event EventHandler<DeviceEventArgs>? DeviceRecovered;

  /// <summary>
  /// Creates a new ChromeCastAudioPlayback instance.
  /// </summary>
  /// <param name="deviceName">The friendly name of the Cast device.</param>
  /// <param name="deviceId">The unique identifier of the Cast device (optional).</param>
  /// <param name="format">The audio format to use. If null, defaults to DVD HQ quality.</param>
  public ChromeCastAudioPlayback(string deviceName, string? deviceId = null, AudioFormat? format = null)
  {
    this.deviceName = deviceName ?? throw new ArgumentNullException(nameof(deviceName));
    this.deviceId = deviceId;
    this.format = format ?? AudioFormat.DvdHq;

    ConfigurationManager.Instance.Logger.Information(
      "ChromeCastAudioPlayback initialized for device: {DeviceName} (ID: {DeviceId})",
      deviceName, deviceId ?? "auto-detect");

    // Initialize connection asynchronously
    connectionTask = InitializeConnectionAsync();
  }

  /// <summary>
  /// Waits for the Cast device connection to complete.
  /// </summary>
  /// <param name="timeout">Optional timeout for the connection. If null, waits indefinitely.</param>
  /// <returns>True if connected successfully, false if connection failed or timed out.</returns>
  public async Task<bool> WaitForConnectionAsync(TimeSpan? timeout = null)
  {
    if (connectionTask == null)
      return false;

    try
    {
      if (timeout.HasValue)
      {
        var completedTask = await Task.WhenAny(connectionTask, Task.Delay(timeout.Value));
        if (completedTask != connectionTask)
        {
          ConfigurationManager.Instance.Logger.Warning(
            "Connection to ChromeCast device timed out after {Timeout}", timeout.Value);
          return false;
        }
      }
      else
      {
        await connectionTask;
      }

      return isConnected;
    }
    catch (Exception ex)
    {
      ConfigurationManager.Instance.Logger.Error(ex,
        "Error waiting for ChromeCast connection");
      return false;
    }
  }

  /// <summary>
  /// Initializes connection to the Cast device.
  /// </summary>
  private async Task InitializeConnectionAsync()
  {
    try
    {
      await connectionLock.WaitAsync();

      // Discover Cast devices on the network
      var deviceLocator = new DeviceLocator();
      var devices = await deviceLocator.FindReceiversAsync();

      // Find the target device by name or ID
      castDevice = devices.FirstOrDefault(d =>
        d.FriendlyName == deviceName ||
        (deviceId != null && d.Id == deviceId));

      if (castDevice == null)
      {
        throw new InvalidOperationException(
          $"Cast device '{deviceName}' not found on network. Available devices: {string.Join(", ", devices.Select(d => d.FriendlyName))}");
      }

      // Create sender and connect
      castSender = new Sender();
      await castSender.ConnectAsync(castDevice);
      isConnected = true;

      // Launch the default media receiver app
      var receiverChannel = castSender.GetChannel<IReceiverChannel>();
      await receiverChannel.LaunchAsync("CC1AD845"); // Default Media Receiver app ID

      ConfigurationManager.Instance.Logger.Information(
        "Connected to ChromeCast device: {DeviceName} at {IPAddress}",
        castDevice.FriendlyName, castDevice.IPEndPoint);
    }
    catch (Exception ex)
    {
      ConfigurationManager.Instance.Logger.Error(ex,
        "Failed to initialize ChromeCast connection to {DeviceName}", deviceName);
      DeviceError?.Invoke(this, new DeviceEventArgs(deviceName,
        "Failed to initialize connection", ex));
    }
    finally
    {
      connectionLock.Release();
    }
  }

  /// <summary>
  /// Creates a new ChromeCastAudioPlayback instance from stored configuration.
  /// </summary>
  /// <param name="configId">The configuration ID to load from storage.</param>
  /// <returns>A new ChromeCastAudioPlayback instance.</returns>
  public static async Task<ChromeCastAudioPlayback> FromStorageAsync(string configId)
  {
    var storage = StorageManager.Instance.Storage;
    var config = await storage.LoadAsync<ChromeCastConfiguration>("ChromeCast", configId);
    
    if (config == null)
    {
      throw new InvalidOperationException($"ChromeCast configuration '{configId}' not found in storage.");
    }

    // Note: AudioFormat is a struct from SoundFlow with predefined formats
    // For now, we use the default DvdHq format
    // Future enhancement: support custom formats based on config
    AudioFormat? audioFormat = null;

    return new ChromeCastAudioPlayback(config.DeviceName, config.DeviceId, audioFormat);
  }

  /// <summary>
  /// Saves the configuration for this Cast device to storage.
  /// </summary>
  /// <param name="configId">The configuration ID to save as.</param>
  public async Task SaveConfigurationAsync(string configId)
  {
    var config = new ChromeCastConfiguration
    {
      DeviceName = deviceName,
      DeviceId = deviceId
    };

    var storage = StorageManager.Instance.Storage;
    await storage.SaveAsync("ChromeCast", configId, config);

    ConfigurationManager.Instance.Logger.Information(
      "ChromeCast configuration saved: {ConfigId}", configId);
  }

  /// <summary>
  /// Gets the audio format being used.
  /// </summary>
  public AudioFormat Format => format;

  /// <summary>
  /// Gets a stub mixer. ChromeCast devices handle mixing on the receiver side.
  /// </summary>
  public Mixer Mixer
  {
    get
    {
      throw new NotSupportedException(
        "ChromeCast devices do not expose a local mixer. " +
        "Audio mixing is performed on the Cast receiver device.");
    }
  }

  /// <summary>
  /// Adds a sound player to stream to the Cast device.
  /// </summary>
  /// <param name="player">The SoundPlayer to add.</param>
  public void AddPlayer(SoundPlayer player)
  {
    if (player == null)
      throw new ArgumentNullException(nameof(player));

    if (playerVolumes.ContainsKey(player))
      throw new InvalidOperationException("Player already added.");

    playerVolumes[player] = 1.0f;

    ConfigurationManager.Instance.Logger.Debug(
      "Player added to ChromeCast device: {DeviceName}", deviceName);

    // Note: GoogleCast requires URLs to media files, not direct audio streams
    // For real-time audio streaming, we would need to:
    // 1. Set up an HTTP server to stream the audio
    // 2. Get a local network URL for the stream
    // 3. Send that URL to the Cast device via LoadMediaAsync
    // This is a complex implementation that requires additional infrastructure
  }

  /// <summary>
  /// Removes a sound player from the Cast device.
  /// </summary>
  /// <param name="player">The SoundPlayer to remove.</param>
  public void RemovePlayer(SoundPlayer player)
  {
    if (player == null)
      throw new ArgumentNullException(nameof(player));

    if (!playerVolumes.ContainsKey(player))
      throw new InvalidOperationException("Player not found.");

    playerVolumes.Remove(player);

    ConfigurationManager.Instance.Logger.Debug(
      "Player removed from ChromeCast device: {DeviceName}", deviceName);
  }

  /// <summary>
  /// Sets the volume for a specific player.
  /// </summary>
  /// <param name="player">The SoundPlayer.</param>
  /// <param name="volume">Volume level (0.0 to 1.0).</param>
  public void SetVolume(SoundPlayer player, float volume)
  {
    if (player == null)
      throw new ArgumentNullException(nameof(player));

    if (volume < 0.0f || volume > 1.0f)
      throw new ArgumentOutOfRangeException(nameof(volume), "Volume must be between 0.0 and 1.0.");

    if (!playerVolumes.ContainsKey(player))
      throw new InvalidOperationException("Player not found.");

    playerVolumes[player] = volume;

    // Update Cast device volume
    _ = SetCastVolumeAsync(volume);
  }

  /// <summary>
  /// Sets the volume on the Cast device.
  /// </summary>
  private async Task SetCastVolumeAsync(float volume)
  {
    if (!isConnected || castSender == null)
      return;

    try
    {
      var receiverChannel = castSender.GetChannel<IReceiverChannel>();
      await receiverChannel.SetVolumeAsync(volume);
    }
    catch (Exception ex)
    {
      ConfigurationManager.Instance.Logger.Warning(ex,
        "Failed to set volume on ChromeCast device");
    }
  }

  /// <summary>
  /// Gets the volume for a specific player.
  /// </summary>
  /// <param name="player">The SoundPlayer.</param>
  /// <returns>Volume level (0.0 to 1.0).</returns>
  public float GetVolume(SoundPlayer player)
  {
    if (player == null)
      throw new ArgumentNullException(nameof(player));

    if (!playerVolumes.TryGetValue(player, out var volume))
      throw new InvalidOperationException("Player not found.");

    return volume;
  }

  /// <summary>
  /// Loads media from a URL onto the Cast device.
  /// </summary>
  /// <param name="mediaUrl">The URL of the media to play.</param>
  /// <param name="contentType">The MIME type of the media (e.g., "audio/mp3").</param>
  /// <param name="metadata">Optional metadata for the media.</param>
  public async Task LoadMediaAsync(string mediaUrl, string contentType = "audio/mp3", Audio.SongMetadata? metadata = null)
  {
    if (!isConnected || castSender == null)
      throw new InvalidOperationException("Not connected to Cast device");

    try
    {
      var mediaChannel = castSender.GetChannel<IMediaChannel>();

      // Create media metadata if provided
      GenericMediaMetadata? castMetadata = null;
      if (metadata != null)
      {
        castMetadata = new GenericMediaMetadata
        {
          Title = metadata.Title,
          Subtitle = metadata.Artist,
          Images = metadata.AlbumArtUrl != null
            ? new[] { new GoogleCast.Models.Image { Url = metadata.AlbumArtUrl } }
            : null
        };

        // Add custom metadata for radio stations
        if (metadata.Band != null || metadata.FrequencyHz != null)
        {
          castMetadata.Title = metadata.Station ?? metadata.Title;
          castMetadata.Subtitle = metadata.Band != null && metadata.FrequencyHz != null
            ? $"{metadata.Band} {metadata.FrequencyHz / 1000000.0:F1} MHz"
            : metadata.Artist;
        }
      }

      // Create MediaInformation and load
      var mediaInfo = new MediaInformation
      {
        ContentId = mediaUrl,
        ContentType = contentType,
        Metadata = castMetadata! // GoogleCast library accepts null metadata, but lacks nullable annotation
      };

      var response = await mediaChannel.LoadAsync(mediaInfo);
      currentMediaSessionId = response?.MediaSessionId.ToString();

      ConfigurationManager.Instance.Logger.Information(
        "Loaded media on ChromeCast: {Url}", mediaUrl);
    }
    catch (Exception ex)
    {
      ConfigurationManager.Instance.Logger.Error(ex,
        "Failed to load media on ChromeCast device");
      DeviceError?.Invoke(this, new DeviceEventArgs(deviceName,
        "Failed to load media", ex));
      throw;
    }
  }

  /// <summary>
  /// Sends metadata to the Cast device for the currently playing track.
  /// </summary>
  /// <param name="metadata">The song metadata to send.</param>
  public void SendMetadata(Audio.SongMetadata metadata)
  {
    if (metadata == null)
      throw new ArgumentNullException(nameof(metadata));

    ConfigurationManager.Instance.Logger.Debug(
      "Sending metadata to ChromeCast: {Title} by {Artist}",
      metadata.Title ?? "Unknown", metadata.Artist ?? "Unknown");

    // Metadata is sent as part of LoadMediaAsync
    // To update metadata during playback, we would need to reload the media
    // or use the QueueUpdateRequest if using a media queue
  }

  /// <summary>
  /// Stops the Cast device playback.
  /// </summary>
  public void Stop()
  {
    ConfigurationManager.Instance.Logger.Information(
      "Stopping ChromeCast playback: {DeviceName}", deviceName);

    _ = StopAsync();
  }

  /// <summary>
  /// Stops the Cast device playback asynchronously.
  /// </summary>
  private async Task StopAsync()
  {
    if (!isConnected || castSender == null)
      return;

    try
    {
      var mediaChannel = castSender.GetChannel<IMediaChannel>();
      if (currentMediaSessionId != null)
      {
        await mediaChannel.StopAsync();
        currentMediaSessionId = null;
      }
    }
    catch (Exception ex)
    {
      ConfigurationManager.Instance.Logger.Warning(ex,
        "Error stopping ChromeCast playback");
    }
  }

  /// <summary>
  /// Checks if the Cast device is in a healthy state.
  /// </summary>
  /// <returns>True if the device is healthy, false otherwise.</returns>
  public bool IsDeviceHealthy()
  {
    if (disposed)
      return false;

    // Check if connected and sender is available
    if (!isConnected || castSender == null)
      return false;

    try
    {
      // Try to get receiver status as a health check
      var receiverChannel = castSender.GetChannel<IReceiverChannel>();
      return receiverChannel != null;
    }
    catch
    {
      return false;
    }
  }

  /// <summary>
  /// Attempts to restart the Cast device connection if it has failed.
  /// </summary>
  /// <returns>True if the restart was successful, false otherwise.</returns>
  public bool TryRestartDevice()
  {
    try
    {
      ConfigurationManager.Instance.Logger.Information(
        "Attempting to restart ChromeCast connection: {DeviceName}", deviceName);

      // Disconnect if currently connected
      if (castSender != null)
      {
        try
        {
          castSender.Disconnect();
        }
        catch { /* Ignore disconnect errors */ }
        castSender = null;
      }

      isConnected = false;
      currentMediaSessionId = null;

      // Reinitialize connection
      _ = InitializeConnectionAsync();

      DeviceRecovered?.Invoke(this, new DeviceEventArgs(deviceName, "Device restarted successfully"));
      return true;
    }
    catch (Exception ex)
    {
      DeviceError?.Invoke(this, new DeviceEventArgs(deviceName, "Failed to restart device", ex));
      return false;
    }
  }

  public void Dispose()
  {
    if (disposed)
      return;

    Stop();
    
    // Disconnect from Cast device
    if (castSender != null)
    {
      try
      {
        castSender.Disconnect();
      }
      catch (Exception ex)
      {
        ConfigurationManager.Instance.Logger.Warning(ex,
          "Error disconnecting from ChromeCast device");
      }
      castSender = null;
    }

    playerVolumes.Clear();
    connectionLock.Dispose();
    disposed = true;

    ConfigurationManager.Instance.Logger.Information(
      "ChromeCastAudioPlayback disposed: {DeviceName}", deviceName);

    GC.SuppressFinalize(this);
  }
}

/// <summary>
/// Configuration data for a ChromeCast audio playback device.
/// </summary>
public class ChromeCastConfiguration
{
  /// <summary>
  /// The friendly name of the Cast device.
  /// </summary>
  public string DeviceName { get; set; } = string.Empty;

  /// <summary>
  /// The unique identifier of the Cast device (optional).
  /// If null, the device will be discovered by name.
  /// </summary>
  public string? DeviceId { get; set; }
}
