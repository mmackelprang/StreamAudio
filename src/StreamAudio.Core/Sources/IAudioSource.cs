using SoundFlow.Components;
using SoundFlow.Structs;
using StreamAudio.Core.Audio;

namespace StreamAudio.Core.Sources;

/// <summary>
/// Defines the type of audio source based on its lifecycle and management.
/// </summary>
public enum SourceType
{
  /// <summary>
  /// Manual sources are user-controlled, long-running audio sources like Spotify,
  /// USB radio, or playlists. They remain active until explicitly removed by the user.
  /// </summary>
  Manual,

  /// <summary>
  /// Auto sources are system-controlled, short-running audio events like phone rings,
  /// doorbell rings, or notifications. They are automatically removed when playback completes.
  /// </summary>
  Auto
}

/// <summary>
/// Interface for all audio sources in the system.
/// Provides a common abstraction for file-based, TTS, composite, and other audio sources.
/// </summary>
public interface IAudioSource : IDisposable
{
  /// <summary>
  /// Gets the name of the audio source.
  /// </summary>
  string Name { get; }

  /// <summary>
  /// Gets the audio format.
  /// </summary>
  AudioFormat Format { get; }

  /// <summary>
  /// Gets the sample rate.
  /// </summary>
  int SampleRate { get; }

  /// <summary>
  /// Gets the number of channels.
  /// </summary>
  int Channels { get; }

  /// <summary>
  /// Gets the type of this source (Manual or Auto).
  /// </summary>
  SourceType SourceType { get; }

  /// <summary>
  /// Gets or sets the number of times to repeat this source.
  /// 0 means infinite loop (only applies to Auto sources, enforced by MaxStreamDuration).
  /// Default is 1 (play once).
  /// </summary>
  int RepeatCount { get; set; }

  /// <summary>
  /// Gets metadata for the currently playing content, if available.
  /// Returns null if metadata is not available or not applicable for this source type.
  /// </summary>
  SongMetadata? CurrentlyPlaying { get; }

  /// <summary>
  /// Gets the underlying SoundPlayer for advanced operations.
  /// </summary>
  SoundPlayer Player { get; }

  /// <summary>
  /// Gets the current playback state.
  /// </summary>
  SoundFlow.Enums.PlaybackState State { get; }

  /// <summary>
  /// Plays the audio.
  /// </summary>
  void Play();

  /// <summary>
  /// Pauses the audio.
  /// </summary>
  void Pause();

  /// <summary>
  /// Stops the audio.
  /// </summary>
  void Stop();
}
