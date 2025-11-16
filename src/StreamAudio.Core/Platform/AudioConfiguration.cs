using SoundFlow.Structs;

namespace StreamAudio.Core.Platform;

/// <summary>
/// Configuration settings for audio playback and processing.
/// Provides platform-specific defaults optimized for different environments.
/// </summary>
public class AudioConfiguration
{
  /// <summary>
  /// Gets or sets the audio format to use.
  /// </summary>
  public AudioFormat Format { get; set; }

  /// <summary>
  /// Gets or sets the buffer size in frames.
  /// Smaller buffers reduce latency but may cause glitches on slower systems.
  /// </summary>
  public int BufferSizeInFrames { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether to use low-latency mode.
  /// </summary>
  public bool LowLatencyMode { get; set; }

  /// <summary>
  /// Creates a new AudioConfiguration with default settings.
  /// </summary>
  public AudioConfiguration()
  {
    Format = AudioFormat.DvdHq;
    BufferSizeInFrames = GetDefaultBufferSize();
    LowLatencyMode = false;
  }

  /// <summary>
  /// Creates a platform-optimized audio configuration.
  /// </summary>
  /// <returns>A configuration optimized for the current platform.</returns>
  public static AudioConfiguration CreateDefault()
  {
    return new AudioConfiguration
    {
      Format = AudioFormat.DvdHq,
      BufferSizeInFrames = GetDefaultBufferSize(),
      LowLatencyMode = false
    };
  }

  /// <summary>
  /// Creates a Raspberry Pi-optimized audio configuration.
  /// Uses larger buffers and conservative settings for stability on embedded hardware.
  /// </summary>
  /// <returns>A configuration optimized for Raspberry Pi.</returns>
  public static AudioConfiguration CreateForRaspberryPi()
  {
    return new AudioConfiguration
    {
      Format = AudioFormat.DvdHq,
      BufferSizeInFrames = 2048, // Larger buffer for stability
      LowLatencyMode = false
    };
  }

  /// <summary>
  /// Creates a low-latency audio configuration.
  /// Uses smaller buffers for real-time applications.
  /// </summary>
  /// <returns>A configuration optimized for low latency.</returns>
  public static AudioConfiguration CreateLowLatency()
  {
    return new AudioConfiguration
    {
      Format = AudioFormat.DvdHq,
      BufferSizeInFrames = 256, // Small buffer for low latency
      LowLatencyMode = true
    };
  }

  /// <summary>
  /// Gets the default buffer size based on the current platform.
  /// </summary>
  private static int GetDefaultBufferSize()
  {
    if (PlatformInfo.IsRaspberryPi)
    {
      // Raspberry Pi: Use larger buffers for stability
      return 2048;
    }
    else if (PlatformInfo.IsLinux)
    {
      // Linux: Moderate buffer size
      return 1024;
    }
    else if (PlatformInfo.IsWindows || PlatformInfo.IsMacOS)
    {
      // Windows/macOS: Can handle smaller buffers
      return 512;
    }
    
    // Default fallback
    return 1024;
  }

  /// <summary>
  /// Gets a description of the current configuration.
  /// </summary>
  public string GetDescription()
  {
    return $"Format: {Format.SampleRate}Hz, {Format.Channels}ch, {Format.Format}, " +
           $"Buffer: {BufferSizeInFrames} frames, " +
           $"Low Latency: {(LowLatencyMode ? "Yes" : "No")}";
  }
}
