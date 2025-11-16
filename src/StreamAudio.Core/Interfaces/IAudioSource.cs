namespace StreamAudio.Core.Interfaces;

/// <summary>
/// Represents a source of audio data that can be read and mixed.
/// </summary>
public interface IAudioSource : IDisposable
{
  /// <summary>
  /// Gets the name or description of this audio source.
  /// </summary>
  string Name { get; }

  /// <summary>
  /// Gets the sample rate of the audio in Hz (e.g., 44100, 48000).
  /// </summary>
  int SampleRate { get; }

  /// <summary>
  /// Gets the number of channels (1 = mono, 2 = stereo).
  /// </summary>
  int Channels { get; }

  /// <summary>
  /// Gets a value indicating whether this source should repeat when it reaches the end.
  /// </summary>
  bool Repeat { get; set; }

  /// <summary>
  /// Gets a value indicating whether this source has reached the end and has no more data.
  /// </summary>
  bool HasEnded { get; }

  /// <summary>
  /// Reads audio samples from this source.
  /// </summary>
  /// <param name="buffer">Buffer to fill with audio samples as 32-bit floats.</param>
  /// <param name="offset">Offset in the buffer to start writing.</param>
  /// <param name="count">Number of samples to read.</param>
  /// <returns>The actual number of samples read.</returns>
  int Read(float[] buffer, int offset, int count);
}
