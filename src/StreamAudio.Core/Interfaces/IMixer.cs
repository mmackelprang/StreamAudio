namespace StreamAudio.Core.Interfaces;

/// <summary>
/// Represents an audio mixer that combines multiple audio sources with volume control.
/// </summary>
public interface IMixer : IAudioSource
{
  /// <summary>
  /// Adds an audio source to the mixer.
  /// </summary>
  /// <param name="source">The audio source to add.</param>
  /// <param name="isPrimary">True if this should be the primary source, false for background.</param>
  void AddSource(IAudioSource source, bool isPrimary = false);

  /// <summary>
  /// Removes an audio source from the mixer.
  /// </summary>
  /// <param name="source">The audio source to remove.</param>
  void RemoveSource(IAudioSource source);

  /// <summary>
  /// Sets the volume for a specific source.
  /// </summary>
  /// <param name="source">The audio source.</param>
  /// <param name="volume">Volume level (0.0 to 1.0).</param>
  void SetVolume(IAudioSource source, float volume);

  /// <summary>
  /// Gets the volume for a specific source.
  /// </summary>
  /// <param name="source">The audio source.</param>
  /// <returns>Volume level (0.0 to 1.0).</returns>
  float GetVolume(IAudioSource source);

  /// <summary>
  /// Sets which source should be considered primary.
  /// </summary>
  /// <param name="source">The source to make primary, or null for none.</param>
  void SetPrimarySource(IAudioSource? source);

  /// <summary>
  /// Gets the primary volume level (for primary sources).
  /// </summary>
  float PrimaryVolume { get; set; }

  /// <summary>
  /// Gets the background volume level (for non-primary sources).
  /// </summary>
  float BackgroundVolume { get; set; }
}
