using System.Runtime.InteropServices;

namespace StreamAudio.Core.Platform;

/// <summary>
/// Provides information about the current platform.
/// </summary>
public static class PlatformInfo
{
  /// <summary>
  /// Gets a value indicating whether the current platform is Windows.
  /// </summary>
  public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

  /// <summary>
  /// Gets a value indicating whether the current platform is Linux.
  /// </summary>
  public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

  /// <summary>
  /// Gets a value indicating whether the current platform is macOS.
  /// </summary>
  public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

  /// <summary>
  /// Gets a value indicating whether the current platform is Raspberry Pi.
  /// Note: This is a best-effort detection based on common Raspberry Pi characteristics.
  /// </summary>
  public static bool IsRaspberryPi
  {
    get
    {
      if (!IsLinux)
        return false;

      // Check for Raspberry Pi specific indicators
      try
      {
        // Check /proc/device-tree/model for Raspberry Pi
        if (File.Exists("/proc/device-tree/model"))
        {
          var model = File.ReadAllText("/proc/device-tree/model");
          return model.Contains("Raspberry Pi", StringComparison.OrdinalIgnoreCase);
        }

        // Check /proc/cpuinfo for BCM processor
        if (File.Exists("/proc/cpuinfo"))
        {
          var cpuInfo = File.ReadAllText("/proc/cpuinfo");
          return cpuInfo.Contains("BCM", StringComparison.OrdinalIgnoreCase);
        }
      }
      catch
      {
        // If we can't read the files, assume not a Raspberry Pi
      }

      return false;
    }
  }

  /// <summary>
  /// Gets the operating system description.
  /// </summary>
  public static string OSDescription => RuntimeInformation.OSDescription;

  /// <summary>
  /// Gets the process architecture (x64, ARM, ARM64, etc.).
  /// </summary>
  public static Architecture ProcessArchitecture => RuntimeInformation.ProcessArchitecture;

  /// <summary>
  /// Gets the OS architecture.
  /// </summary>
  public static Architecture OSArchitecture => RuntimeInformation.OSArchitecture;

  /// <summary>
  /// Gets a value indicating whether audio playback is available on the current system.
  /// </summary>
  public static bool HasAudioPlayback
  {
    get
    {
      try
      {
        var devices = AudioDeviceEnumerator.GetPlaybackDevices();
        return devices.Any();
      }
      catch
      {
        // If we can't enumerate devices, assume no audio playback
        return false;
      }
    }
  }

  /// <summary>
  /// Gets a friendly name for the current platform.
  /// </summary>
  public static string PlatformName
  {
    get
    {
      if (IsRaspberryPi)
        return "Raspberry Pi";
      if (IsWindows)
        return "Windows";
      if (IsLinux)
        return "Linux";
      if (IsMacOS)
        return "macOS";
      return "Unknown";
    }
  }
}
