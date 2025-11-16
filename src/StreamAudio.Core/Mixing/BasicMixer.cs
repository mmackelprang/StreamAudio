using StreamAudio.Core.Interfaces;

namespace StreamAudio.Core.Mixing;

/// <summary>
/// Basic audio mixer that combines multiple audio sources with volume control.
/// Uses 32-bit float processing to avoid clipping and maintain quality.
/// </summary>
public class BasicMixer : IMixer
{
  private readonly List<MixerInput> inputs = new();
  private readonly object lockObject = new();
  private readonly int sampleRate;
  private readonly int channels;
  private IAudioSource? primarySource;
  private bool disposed;

  public BasicMixer(int sampleRate, int channels)
  {
    if (sampleRate <= 0)
      throw new ArgumentException("Sample rate must be positive.", nameof(sampleRate));
    
    if (channels <= 0)
      throw new ArgumentException("Channels must be positive.", nameof(channels));

    this.sampleRate = sampleRate;
    this.channels = channels;
  }

  public string Name => "BasicMixer";

  public int SampleRate => sampleRate;

  public int Channels => channels;

  public bool Repeat { get; set; }

  public bool HasEnded
  {
    get
    {
      lock (lockObject)
      {
        // Mixer has ended if all non-repeating sources have ended
        return inputs.All(i => i.Source.HasEnded || i.Source.Repeat);
      }
    }
  }

  public float PrimaryVolume { get; set; } = 1.0f;

  public float BackgroundVolume { get; set; } = 0.3f;

  public void AddSource(IAudioSource source, bool isPrimary = false)
  {
    if (source == null)
      throw new ArgumentNullException(nameof(source));

    if (source.SampleRate != sampleRate)
      throw new ArgumentException($"Source sample rate ({source.SampleRate}) does not match mixer sample rate ({sampleRate}).");

    if (source.Channels != channels)
      throw new ArgumentException($"Source channels ({source.Channels}) do not match mixer channels ({channels}).");

    lock (lockObject)
    {
      var input = new MixerInput
      {
        Source = source,
        Volume = isPrimary ? PrimaryVolume : BackgroundVolume
      };

      inputs.Add(input);

      if (isPrimary)
      {
        primarySource = source;
      }
    }
  }

  public void RemoveSource(IAudioSource source)
  {
    if (source == null)
      throw new ArgumentNullException(nameof(source));

    lock (lockObject)
    {
      var input = inputs.FirstOrDefault(i => i.Source == source);
      if (input != null)
      {
        inputs.Remove(input);
        
        if (primarySource == source)
        {
          primarySource = null;
        }
      }
    }
  }

  public void SetVolume(IAudioSource source, float volume)
  {
    if (source == null)
      throw new ArgumentNullException(nameof(source));

    if (volume < 0.0f || volume > 1.0f)
      throw new ArgumentOutOfRangeException(nameof(volume), "Volume must be between 0.0 and 1.0.");

    lock (lockObject)
    {
      var input = inputs.FirstOrDefault(i => i.Source == source);
      if (input != null)
      {
        input.Volume = volume;
      }
    }
  }

  public float GetVolume(IAudioSource source)
  {
    if (source == null)
      throw new ArgumentNullException(nameof(source));

    lock (lockObject)
    {
      var input = inputs.FirstOrDefault(i => i.Source == source);
      return input?.Volume ?? 0.0f;
    }
  }

  public void SetPrimarySource(IAudioSource? source)
  {
    lock (lockObject)
    {
      // Reset all sources to background volume
      foreach (var input in inputs)
      {
        input.Volume = BackgroundVolume;
      }

      // Set the new primary source
      primarySource = source;

      if (source != null)
      {
        var input = inputs.FirstOrDefault(i => i.Source == source);
        if (input != null)
        {
          input.Volume = PrimaryVolume;
        }
      }
    }
  }

  public int Read(float[] buffer, int offset, int count)
  {
    if (disposed)
      throw new ObjectDisposedException(nameof(BasicMixer));

    if (buffer == null)
      throw new ArgumentNullException(nameof(buffer));

    if (offset < 0 || offset >= buffer.Length)
      throw new ArgumentOutOfRangeException(nameof(offset));

    if (count < 0 || offset + count > buffer.Length)
      throw new ArgumentOutOfRangeException(nameof(count));

    // Clear the buffer
    Array.Fill(buffer, 0f, offset, count);

    lock (lockObject)
    {
      if (inputs.Count == 0)
      {
        return count; // Return silence
      }

      // Temporary buffer for reading from each source
      float[] tempBuffer = new float[count];

      foreach (var input in inputs.ToList()) // ToList to avoid modification during iteration
      {
        if (input.Source.HasEnded && !input.Source.Repeat)
          continue;

        // Read from source
        int samplesRead = input.Source.Read(tempBuffer, 0, count);

        // Mix into output buffer with volume control
        for (int i = 0; i < samplesRead; i++)
        {
          buffer[offset + i] += tempBuffer[i] * input.Volume;
        }

        // Clamp to prevent clipping (values outside -1.0 to 1.0 range)
        for (int i = 0; i < count; i++)
        {
          buffer[offset + i] = Math.Clamp(buffer[offset + i], -1.0f, 1.0f);
        }
      }
    }

    return count;
  }

  public void Dispose()
  {
    if (disposed)
      return;

    lock (lockObject)
    {
      foreach (var input in inputs)
      {
        input.Source.Dispose();
      }
      inputs.Clear();
    }

    disposed = true;
    GC.SuppressFinalize(this);
  }

  private class MixerInput
  {
    public IAudioSource Source { get; set; } = null!;
    public float Volume { get; set; }
  }
}
