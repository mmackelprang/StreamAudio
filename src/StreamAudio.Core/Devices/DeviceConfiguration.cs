using StreamAudio.Core.Sources;
using StreamAudio.Core.Playback;

namespace StreamAudio.Core.Devices;

/// <summary>
/// Represents a device configuration that can be saved/loaded from storage
/// </summary>
public class DeviceConfiguration
{
  /// <summary>
  /// Unique identifier for this device configuration
  /// </summary>
  public string Id { get; set; } = string.Empty;

  /// <summary>
  /// Display name for the device
  /// </summary>
  public string Name { get; set; } = string.Empty;

  /// <summary>
  /// Device type (File, Spotify, USB, ChromeCast, etc.)
  /// </summary>
  public string DeviceType { get; set; } = string.Empty;

  /// <summary>
  /// Whether this device is visible in the UI
  /// </summary>
  public bool IsVisible { get; set; } = true;

  /// <summary>
  /// Whether this device is enabled
  /// </summary>
  public bool IsEnabled { get; set; } = true;

  /// <summary>
  /// Configuration data specific to this device type (JSON serialized)
  /// </summary>
  public Dictionary<string, string> Configuration { get; set; } = new();

  /// <summary>
  /// Device category: AudioSource or AudioPlayback
  /// </summary>
  public string Category { get; set; } = string.Empty;
}

/// <summary>
/// Represents a device descriptor with metadata
/// </summary>
public class DeviceDescriptor
{
  /// <summary>
  /// Unique identifier
  /// </summary>
  public string Id { get; set; } = string.Empty;

  /// <summary>
  /// Display name
  /// </summary>
  public string Name { get; set; } = string.Empty;

  /// <summary>
  /// Device type
  /// </summary>
  public string DeviceType { get; set; } = string.Empty;

  /// <summary>
  /// Whether device is currently available
  /// </summary>
  public bool IsAvailable { get; set; }

  /// <summary>
  /// Whether device is visible in UI
  /// </summary>
  public bool IsVisible { get; set; } = true;

  /// <summary>
  /// Whether device is enabled
  /// </summary>
  public bool IsEnabled { get; set; } = true;

  /// <summary>
  /// Device category
  /// </summary>
  public string Category { get; set; } = string.Empty;

  /// <summary>
  /// Additional metadata
  /// </summary>
  public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Configuration for creating an Auto audio source (TTS, notification, etc.)
/// </summary>
public class AutoSourceConfiguration
{
  /// <summary>
  /// Type of auto source: TTS, FileAlert, etc.
  /// </summary>
  public string Type { get; set; } = string.Empty;

  /// <summary>
  /// Text for TTS or file path for FileAlert
  /// </summary>
  public string Content { get; set; } = string.Empty;

  /// <summary>
  /// TTS voice/engine configuration
  /// </summary>
  public Dictionary<string, string> TtsConfig { get; set; } = new();

  /// <summary>
  /// Repeat count (0 = infinite, but limited by StreamManager.MaxStreamDuration)
  /// </summary>
  public int RepeatCount { get; set; } = 1;
}
