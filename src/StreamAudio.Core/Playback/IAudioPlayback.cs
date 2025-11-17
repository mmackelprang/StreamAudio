using SoundFlow.Components;
using SoundFlow.Structs;
using StreamAudio.Core.Events;

namespace StreamAudio.Core.Playback;

/// <summary>
/// Interface for audio playback devices.
/// Enables mocking and alternate implementations like FFT analysis or Chromecast devices.
/// </summary>
public interface IAudioPlayback : IDisposable
{
  /// <summary>
  /// Occurs when the playback device encounters an error.
  /// </summary>
  event EventHandler<DeviceEventArgs>? DeviceError;

  /// <summary>
  /// Occurs when the playback device is successfully recovered.
  /// </summary>
  event EventHandler<DeviceEventArgs>? DeviceRecovered;

  /// <summary>
  /// Gets the audio format being used.
  /// </summary>
  AudioFormat Format { get; }

  /// <summary>
  /// Gets the built-in mixer for this playback device.
  /// </summary>
  Mixer Mixer { get; }

  /// <summary>
  /// Adds a sound player component to the mixer.
  /// </summary>
  /// <param name="player">The SoundPlayer to add.</param>
  void AddPlayer(SoundPlayer player);

  /// <summary>
  /// Removes a sound player component from the mixer.
  /// </summary>
  /// <param name="player">The SoundPlayer to remove.</param>
  void RemovePlayer(SoundPlayer player);

  /// <summary>
  /// Sets the volume for a specific player in the mixer.
  /// </summary>
  /// <param name="player">The SoundPlayer.</param>
  /// <param name="volume">Volume level (0.0 to 1.0).</param>
  void SetVolume(SoundPlayer player, float volume);

  /// <summary>
  /// Gets the volume for a specific player.
  /// </summary>
  /// <param name="player">The SoundPlayer.</param>
  /// <returns>Volume level (0.0 to 1.0).</returns>
  float GetVolume(SoundPlayer player);

  /// <summary>
  /// Stops the playback device.
  /// </summary>
  void Stop();

  /// <summary>
  /// Checks if the playback device is in a healthy state.
  /// </summary>
  /// <returns>True if the device is healthy, false otherwise.</returns>
  bool IsDeviceHealthy();

  /// <summary>
  /// Attempts to restart the playback device if it has failed.
  /// </summary>
  /// <returns>True if the restart was successful, false otherwise.</returns>
  bool TryRestartDevice();
}
