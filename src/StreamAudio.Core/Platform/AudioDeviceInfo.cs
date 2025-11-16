namespace StreamAudio.Core.Platform;

/// <summary>
/// Represents information about an audio device.
/// </summary>
public class AudioDeviceInfo
{
  /// <summary>
  /// Gets or sets the device ID.
  /// </summary>
  public IntPtr Id { get; set; }

  /// <summary>
  /// Gets or sets the device name.
  /// </summary>
  public string Name { get; set; } = string.Empty;

  /// <summary>
  /// Gets or sets a value indicating whether this is a playback device.
  /// </summary>
  public bool IsPlayback { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether this is a capture device.
  /// </summary>
  public bool IsCapture { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether this is the default device.
  /// </summary>
  public bool IsDefault { get; set; }

  /// <summary>
  /// Gets or sets the device type (USB, Internal, etc.).
  /// </summary>
  public string DeviceType { get; set; } = "Unknown";

  public override string ToString()
  {
    var flags = new List<string>();
    if (IsDefault) flags.Add("Default");
    if (IsPlayback) flags.Add("Playback");
    if (IsCapture) flags.Add("Capture");
    
    var flagStr = flags.Count > 0 ? $" [{string.Join(", ", flags)}]" : "";
    return $"{Name} ({DeviceType}){flagStr}";
  }
}
