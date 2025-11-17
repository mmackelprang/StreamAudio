using SoundFlow.Abstracts.Devices;
using SoundFlow.Components;
using SoundFlow.Structs;
using SoundFlow.Enums;
using StreamAudio.Core.Platform;
using StreamAudio.Core.Events;

namespace StreamAudio.Core.Playback;

/// <summary>
/// Manages audio playback using SoundFlow's playback device and mixer.
/// This class provides a simple interface for playing audio with mixing capabilities.
/// </summary>
public class AudioPlayback : IAudioPlayback
{
  private readonly AudioPlaybackDevice playbackDevice;
  private readonly AudioFormat format;
  private bool disposed;

  /// <summary>
  /// Occurs when the playback device encounters an error.
  /// </summary>
  public event EventHandler<DeviceEventArgs>? DeviceError;

  /// <summary>
  /// Occurs when the playback device is successfully recovered.
  /// </summary>
  public event EventHandler<DeviceEventArgs>? DeviceRecovered;

  /// <summary>
  /// Creates a new AudioPlayback instance with the specified format.
  /// </summary>
  /// <param name="format">The audio format to use. If null, defaults to DVD HQ quality.</param>
  public AudioPlayback(AudioFormat? format = null)
  {
    this.format = format ?? AudioFormat.DvdHq;

    // Initialize playback device with default device (null DeviceInfo)
    var engine = AudioEngineManager.Engine;
    playbackDevice = engine.InitializePlaybackDevice(null, this.format);
    playbackDevice.Start();
  }

  /// <summary>
  /// Creates a new AudioPlayback instance with the specified configuration.
  /// </summary>
  /// <param name="configuration">The audio configuration to use.</param>
  public AudioPlayback(AudioConfiguration configuration)
  {
    if (configuration == null)
      throw new ArgumentNullException(nameof(configuration));

    this.format = configuration.Format;

    // Initialize playback device with default device (null DeviceInfo)
    var engine = AudioEngineManager.Engine;
    playbackDevice = engine.InitializePlaybackDevice(null, this.format);
    playbackDevice.Start();
  }

  /// <summary>
  /// Gets the audio format being used.
  /// </summary>
  public AudioFormat Format => format;

  /// <summary>
  /// Gets the built-in mixer for this playback device.
  /// </summary>
  public Mixer Mixer => playbackDevice.MasterMixer;

  /// <summary>
  /// Gets the playback device.
  /// </summary>
  public AudioPlaybackDevice Device => playbackDevice;

  /// <summary>
  /// Adds a sound player component to the mixer.
  /// </summary>
  /// <param name="player">The SoundPlayer to add.</param>
  public void AddPlayer(SoundPlayer player)
  {
    if (player == null)
      throw new ArgumentNullException(nameof(player));

    playbackDevice.MasterMixer.AddComponent(player);
  }

  /// <summary>
  /// Removes a sound player component from the mixer.
  /// </summary>
  /// <param name="player">The SoundPlayer to remove.</param>
  public void RemovePlayer(SoundPlayer player)
  {
    if (player == null)
      throw new ArgumentNullException(nameof(player));

    playbackDevice.MasterMixer.RemoveComponent(player);
  }

  /// <summary>
  /// Sets the volume for a specific player in the mixer.
  /// </summary>
  /// <param name="player">The SoundPlayer.</param>
  /// <param name="volume">Volume level (0.0 to 1.0).</param>
  public void SetVolume(SoundPlayer player, float volume)
  {
    if (player == null)
      throw new ArgumentNullException(nameof(player));

    if (volume < 0.0f || volume > 1.0f)
      throw new ArgumentOutOfRangeException(nameof(volume), "Volume must be between 0.0 and 1.0.");

    player.Volume = volume;
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

    return player.Volume;
  }

  /// <summary>
  /// Stops the playback device.
  /// </summary>
  public void Stop()
  {
    playbackDevice?.Stop();
  }

  /// <summary>
  /// Checks if the playback device is in a healthy state.
  /// </summary>
  /// <returns>True if the device is healthy, false otherwise.</returns>
  public bool IsDeviceHealthy()
  {
    try
    {
      return playbackDevice != null && !disposed;
    }
    catch
    {
      return false;
    }
  }

  /// <summary>
  /// Attempts to restart the playback device if it has failed.
  /// </summary>
  /// <returns>True if the restart was successful, false otherwise.</returns>
  public bool TryRestartDevice()
  {
    try
    {
      if (playbackDevice != null)
      {
        playbackDevice.Stop();
        playbackDevice.Start();
        DeviceRecovered?.Invoke(this, new DeviceEventArgs("Default Device", "Device restarted successfully"));
        return true;
      }
      return false;
    }
    catch (Exception ex)
    {
      DeviceError?.Invoke(this, new DeviceEventArgs("Default Device", "Failed to restart device", ex));
      return false;
    }
  }

  public void Dispose()
  {
    if (disposed)
      return;

    playbackDevice?.Stop();
    playbackDevice?.Dispose();
    disposed = true;
    GC.SuppressFinalize(this);
  }
}
