using SoundFlow.Abstracts.Devices;

namespace StreamAudio.Core.Platform;

/// <summary>
/// Provides cross-platform audio device enumeration using SoundFlow.
/// </summary>
public class AudioDeviceEnumerator
{
  /// <summary>
  /// Gets all available playback devices.
  /// </summary>
  /// <returns>A list of playback device information.</returns>
  public static List<AudioDeviceInfo> GetPlaybackDevices()
  {
    var devices = new List<AudioDeviceInfo>();
    var engine = AudioEngineManager.Engine;

    try
    {
      var deviceInfos = engine.PlaybackDevices;
      
      foreach (var deviceInfo in deviceInfos)
      {
        devices.Add(new AudioDeviceInfo
        {
          Id = deviceInfo.Id,
          Name = deviceInfo.Name,
          IsPlayback = true,
          IsCapture = false,
          IsDefault = deviceInfo.IsDefault,
          DeviceType = DetermineDeviceType(deviceInfo.Name)
        });
      }
    }
    catch (Exception ex)
    {
      // Log error but don't throw - return empty list
      Console.WriteLine($"Error enumerating playback devices: {ex.Message}");
    }

    return devices;
  }

  /// <summary>
  /// Gets all available capture devices.
  /// </summary>
  /// <returns>A list of capture device information.</returns>
  public static List<AudioDeviceInfo> GetCaptureDevices()
  {
    var devices = new List<AudioDeviceInfo>();
    var engine = AudioEngineManager.Engine;

    try
    {
      var deviceInfos = engine.CaptureDevices;
      
      foreach (var deviceInfo in deviceInfos)
      {
        devices.Add(new AudioDeviceInfo
        {
          Id = deviceInfo.Id,
          Name = deviceInfo.Name,
          IsPlayback = false,
          IsCapture = true,
          IsDefault = deviceInfo.IsDefault,
          DeviceType = DetermineDeviceType(deviceInfo.Name)
        });
      }
    }
    catch (Exception ex)
    {
      // Log error but don't throw - return empty list
      Console.WriteLine($"Error enumerating capture devices: {ex.Message}");
    }

    return devices;
  }

  /// <summary>
  /// Gets all available audio devices (both playback and capture).
  /// </summary>
  /// <returns>A list of all audio device information.</returns>
  public static List<AudioDeviceInfo> GetAllDevices()
  {
    var devices = new List<AudioDeviceInfo>();
    devices.AddRange(GetPlaybackDevices());
    devices.AddRange(GetCaptureDevices());
    return devices;
  }

  /// <summary>
  /// Gets the default playback device.
  /// </summary>
  /// <returns>The default playback device, or null if not found.</returns>
  public static AudioDeviceInfo? GetDefaultPlaybackDevice()
  {
    return GetPlaybackDevices().FirstOrDefault(d => d.IsDefault);
  }

  /// <summary>
  /// Gets the default capture device.
  /// </summary>
  /// <returns>The default capture device, or null if not found.</returns>
  public static AudioDeviceInfo? GetDefaultCaptureDevice()
  {
    return GetCaptureDevices().FirstOrDefault(d => d.IsDefault);
  }

  /// <summary>
  /// Determines the device type based on the device name.
  /// </summary>
  private static string DetermineDeviceType(string deviceName)
  {
    if (string.IsNullOrWhiteSpace(deviceName))
      return "Unknown";

    var nameLower = deviceName.ToLowerInvariant();

    if (nameLower.Contains("usb"))
      return "USB";
    if (nameLower.Contains("hdmi"))
      return "HDMI";
    if (nameLower.Contains("bluetooth") || nameLower.Contains("bt"))
      return "Bluetooth";
    if (nameLower.Contains("headphone") || nameLower.Contains("speaker"))
      return "Internal";
    if (nameLower.Contains("bcm") || nameLower.Contains("raspberry"))
      return "Raspberry Pi Audio";
    if (nameLower.Contains("pulse") || nameLower.Contains("alsa"))
      return "System";

    return "Audio Device";
  }
}
