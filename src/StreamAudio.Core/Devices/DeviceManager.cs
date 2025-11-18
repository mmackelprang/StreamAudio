using System.Text.Json;
using StreamAudio.Core.Configuration;
using StreamAudio.Core.Platform;
using StreamAudio.Core.Sources;
using StreamAudio.Core.Storage;
using SoundFlow.Structs;

namespace StreamAudio.Core.Devices;

/// <summary>
/// Manages audio source and playback devices with configuration storage.
/// Provides device enumeration, configuration management, and Auto source creation helpers.
/// </summary>
public class DeviceManager
{
  private const string DeviceConfigTable = "DeviceConfigurations";
  private readonly IStorage _storage;
  private readonly Dictionary<string, DeviceConfiguration> _cachedConfigs = new();

  public DeviceManager()
  {
    _storage = StorageManager.Instance.Storage;
    ConfigurationManager.Instance.Logger.Information("DeviceManager initialized");
    LoadConfigurationsAsync().GetAwaiter().GetResult();
  }

  /// <summary>
  /// Gets all available audio input sources (hardware + configured sources)
  /// </summary>
  public async Task<IEnumerable<DeviceDescriptor>> GetAudioSourcesAsync()
  {
    var devices = new List<DeviceDescriptor>();

    ConfigurationManager.Instance.Logger.Debug("Enumerating audio input sources");

    // Add configured sources from storage
    var configs = await GetDeviceConfigurationsAsync("AudioSource");
    
    foreach (var config in configs.Where(c => c.IsVisible))
    {
      devices.Add(new DeviceDescriptor
      {
        Id = config.Id,
        Name = config.Name,
        DeviceType = config.DeviceType,
        IsAvailable = true, // Assume configured sources are available
        IsVisible = config.IsVisible,
        IsEnabled = config.IsEnabled,
        Category = "AudioSource"
      });
    }

    // Add hardware USB audio devices
    try
    {
      var captureDevices = AudioDeviceEnumerator.GetCaptureDevices();
      foreach (var device in captureDevices)
      {
        devices.Add(new DeviceDescriptor
        {
          Id = $"usb_{device.Id}",
          Name = device.Name,
          DeviceType = "USBCapture",
          IsAvailable = true,
          IsVisible = true,
          IsEnabled = true,
          Category = "AudioSource",
          Metadata = new Dictionary<string, string>
          {
            ["DeviceId"] = device.Id.ToString(),
            ["DeviceType"] = device.DeviceType
          }
        });
      }
    }
    catch (Exception ex)
    {
      ConfigurationManager.Instance.Logger.Error(ex, "Failed to enumerate capture devices");
    }

    ConfigurationManager.Instance.Logger.Information(
      "Found {Count} audio input sources", devices.Count);

    return devices;
  }

  /// <summary>
  /// Gets all available audio playback devices (hardware + configured devices)
  /// </summary>
  public async Task<IEnumerable<DeviceDescriptor>> GetAudioPlaybackDevicesAsync()
  {
    var devices = new List<DeviceDescriptor>();

    ConfigurationManager.Instance.Logger.Debug("Enumerating audio playback devices");

    // Add configured playback devices from storage
    var configs = await GetDeviceConfigurationsAsync("AudioPlayback");
    
    foreach (var config in configs.Where(c => c.IsVisible))
    {
      devices.Add(new DeviceDescriptor
      {
        Id = config.Id,
        Name = config.Name,
        DeviceType = config.DeviceType,
        IsAvailable = true,
        IsVisible = config.IsVisible,
        IsEnabled = config.IsEnabled,
        Category = "AudioPlayback"
      });
    }

    // Add hardware playback devices
    try
    {
      var playbackDevices = AudioDeviceEnumerator.GetPlaybackDevices();
      foreach (var device in playbackDevices)
      {
        devices.Add(new DeviceDescriptor
        {
          Id = $"hw_{device.Id}",
          Name = device.Name,
          DeviceType = "Hardware",
          IsAvailable = true,
          IsVisible = true,
          IsEnabled = true,
          Category = "AudioPlayback",
          Metadata = new Dictionary<string, string>
          {
            ["DeviceId"] = device.Id.ToString(),
            ["DeviceType"] = device.DeviceType,
            ["IsDefault"] = device.IsDefault.ToString()
          }
        });
      }
    }
    catch (Exception ex)
    {
      ConfigurationManager.Instance.Logger.Error(ex, "Failed to enumerate playback devices");
    }

    ConfigurationManager.Instance.Logger.Information(
      "Found {Count} audio playback devices", devices.Count);

    return devices;
  }

  /// <summary>
  /// Saves a device configuration to storage
  /// </summary>
  public async Task SaveDeviceConfigurationAsync(DeviceConfiguration config)
  {
    if (string.IsNullOrWhiteSpace(config.Id))
      throw new ArgumentException("Device configuration ID cannot be empty", nameof(config));

    await _storage.SaveAsync(DeviceConfigTable, config.Id, config);
    _cachedConfigs[config.Id] = config;

    ConfigurationManager.Instance.Logger.Information(
      "Saved device configuration: {Id} ({Name})", config.Id, config.Name);
  }

  /// <summary>
  /// Gets a device configuration from storage
  /// </summary>
  public async Task<DeviceConfiguration?> GetDeviceConfigurationAsync(string id)
  {
    if (_cachedConfigs.TryGetValue(id, out var cached))
      return cached;

    var config = await _storage.LoadAsync<DeviceConfiguration>(DeviceConfigTable, id);
    if (config != null)
    {
      _cachedConfigs[id] = config;
    }
    return config;
  }

  /// <summary>
  /// Gets all device configurations of a specific category
  /// </summary>
  public async Task<IEnumerable<DeviceConfiguration>> GetDeviceConfigurationsAsync(string? category = null)
  {
    var all = await _storage.GetAllAsync<DeviceConfiguration>(DeviceConfigTable);
    
    if (string.IsNullOrWhiteSpace(category))
      return all;

    return all.Where(c => c.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
  }

  /// <summary>
  /// Deletes a device configuration
  /// </summary>
  public async Task DeleteDeviceConfigurationAsync(string id)
  {
    await _storage.DeleteAsync(DeviceConfigTable, id);
    _cachedConfigs.Remove(id);

    ConfigurationManager.Instance.Logger.Information(
      "Deleted device configuration: {Id}", id);
  }

  /// <summary>
  /// Creates a TTS Auto audio source
  /// </summary>
  public IAudioSource CreateTtsAutoSource(AutoSourceConfiguration config)
  {
    if (config.Type != "TTS")
      throw new ArgumentException("Configuration type must be TTS", nameof(config));

    ConfigurationManager.Instance.Logger.Information(
      "Creating TTS auto source with text: {Text}", 
      config.Content.Length > 50 ? config.Content.Substring(0, 50) + "..." : config.Content);

    // Parse TTS configuration
    var ttsConfig = new TtsConfiguration
    {
      Engine = GetConfigValue(config.TtsConfig, "Engine", "espeak"),
      Voice = GetConfigValue(config.TtsConfig, "Voice", null),
      Rate = double.TryParse(GetConfigValue(config.TtsConfig, "Rate", "1.0"), out var rate) ? rate : 1.0,
      Pitch = double.TryParse(GetConfigValue(config.TtsConfig, "Pitch", "0.0"), out var pitch) ? pitch : 0.0,
      Volume = double.TryParse(GetConfigValue(config.TtsConfig, "Volume", "1.0"), out var volume) ? volume : 1.0
    };

    var source = new TtsAudioSource(config.Content, config: ttsConfig);
    source.RepeatCount = config.RepeatCount;
    return source;
  }

  /// <summary>
  /// Creates a File Auto audio source (for alerts, notifications, etc.)
  /// </summary>
  public IAudioSource CreateFileAutoSource(AutoSourceConfiguration config)
  {
    if (config.Type != "FileAlert")
      throw new ArgumentException("Configuration type must be FileAlert", nameof(config));

    if (!File.Exists(config.Content))
      throw new FileNotFoundException($"Audio file not found: {config.Content}");

    ConfigurationManager.Instance.Logger.Information(
      "Creating file auto source: {FilePath}", config.Content);

    var source = new FileAudioSource(config.Content, sourceType: SourceType.Auto);
    source.RepeatCount = config.RepeatCount;
    return source;
  }

  /// <summary>
  /// Creates a File audio source from a saved configuration
  /// </summary>
  public async Task<IAudioSource> CreateFileSourceFromConfigAsync(string configId)
  {
    var config = await GetDeviceConfigurationAsync(configId);
    if (config == null)
      throw new InvalidOperationException($"Device configuration not found: {configId}");

    if (config.DeviceType != "File")
      throw new InvalidOperationException($"Configuration is not a File source: {config.DeviceType}");

    ConfigurationManager.Instance.Logger.Information(
      "Creating file source from configuration: {ConfigId}", configId);

    // Parse file source configuration
    var paths = GetConfigValue(config.Configuration, "Paths", "");
    var pathList = paths.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();

    if (pathList.Count == 0)
      throw new InvalidOperationException("No file paths configured");

    if (pathList.Count == 1 && Directory.Exists(pathList[0]))
    {
      return FileAudioSource.FromDirectory(pathList[0]);
    }
    else if (pathList.Count == 1)
    {
      return new FileAudioSource(pathList[0]);
    }
    else
    {
      return new FileAudioSource(pathList);
    }
  }

  /// <summary>
  /// Creates a Spotify audio source from a saved configuration
  /// </summary>
  public async Task<IAudioSource> CreateSpotifySourceFromConfigAsync(string configId)
  {
    var config = await GetDeviceConfigurationAsync(configId);
    if (config == null)
      throw new InvalidOperationException($"Device configuration not found: {configId}");

    if (config.DeviceType != "Spotify")
      throw new InvalidOperationException($"Configuration is not a Spotify source: {config.DeviceType}");

    ConfigurationManager.Instance.Logger.Information(
      "Creating Spotify source from configuration: {ConfigId}", configId);

    // Parse Spotify configuration with secret resolution
    var spotifyConfig = new SpotifyConfiguration
    {
      ClientId = GetConfigValue(config.Configuration, "ClientId", null),
      ClientSecret = GetConfigValue(config.Configuration, "ClientSecret", null),
      RefreshToken = GetConfigValue(config.Configuration, "RefreshToken", null),
      RedirectUri = GetConfigValue(config.Configuration, "RedirectUri", "http://localhost:5000/callback"),
      Market = GetConfigValue(config.Configuration, "Market", "US")
    };

    var source = new SpotifyAudioSource(spotifyConfig);
    await source.InitializeAsync();
    return source;
  }

  /// <summary>
  /// Creates a USB audio source from a saved configuration
  /// </summary>
  public async Task<IAudioSource> CreateUsbSourceFromConfigAsync(string configId)
  {
    var config = await GetDeviceConfigurationAsync(configId);
    if (config == null)
      throw new InvalidOperationException($"Device configuration not found: {configId}");

    if (config.DeviceType != "USB")
      throw new InvalidOperationException($"Configuration is not a USB source: {config.DeviceType}");

    ConfigurationManager.Instance.Logger.Information(
      "Creating USB source from configuration: {ConfigId}", configId);

    // Parse USB configuration
    var usbConfig = new UsbAudioConfiguration
    {
      DeviceNumber = int.TryParse(GetConfigValue(config.Configuration, "DeviceNumber", "-1"), out var devNum) ? devNum : -1,
      DeviceName = GetConfigValue(config.Configuration, "DeviceName", "USB Audio Device"),
      SampleRate = int.TryParse(GetConfigValue(config.Configuration, "SampleRate", "44100"), out var sr) ? sr : 44100,
      Channels = int.TryParse(GetConfigValue(config.Configuration, "Channels", "2"), out var ch) ? ch : 2
    };

    return new UsbAudioSource(usbConfig);
  }

  private async Task LoadConfigurationsAsync()
  {
    try
    {
      var configs = await _storage.GetAllAsync<DeviceConfiguration>(DeviceConfigTable);
      foreach (var config in configs)
      {
        _cachedConfigs[config.Id] = config;
      }

      ConfigurationManager.Instance.Logger.Debug(
        "Loaded {Count} device configurations", _cachedConfigs.Count);
    }
    catch (Exception ex)
    {
      ConfigurationManager.Instance.Logger.Error(ex, "Failed to load device configurations");
    }
  }

  private static string GetConfigValue(Dictionary<string, string> config, string key, string defaultValue)
  {
    return config.TryGetValue(key, out var value) ? value : defaultValue;
  }
}
