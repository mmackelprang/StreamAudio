namespace StreamAudio.Core.Interfaces;

/// <summary>
/// Represents an audio output device or destination.
/// </summary>
public interface IAudioOutput : IDisposable
{
  /// <summary>
  /// Gets the name of the output device.
  /// </summary>
  string DeviceName { get; }

  /// <summary>
  /// Gets the sample rate for output in Hz.
  /// </summary>
  int SampleRate { get; }

  /// <summary>
  /// Gets the number of channels for output.
  /// </summary>
  int Channels { get; }

  /// <summary>
  /// Initializes the output device with the specified audio source.
  /// </summary>
  /// <param name="source">The audio source to play.</param>
  void Initialize(IAudioSource source);

  /// <summary>
  /// Starts playback.
  /// </summary>
  void Play();

  /// <summary>
  /// Stops playback.
  /// </summary>
  void Stop();

  /// <summary>
  /// Pauses playback.
  /// </summary>
  void Pause();
}
