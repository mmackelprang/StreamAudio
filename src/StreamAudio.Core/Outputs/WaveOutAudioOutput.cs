using NAudio.Wave;
using StreamAudio.Core.Interfaces;

namespace StreamAudio.Core.Outputs;

/// <summary>
/// Audio output using NAudio's WaveOut for cross-platform playback.
/// </summary>
public class WaveOutAudioOutput : IAudioOutput
{
  private IWavePlayer? waveOut;
  private SampleToWaveProvider? waveProvider;
  private IAudioSource? source;
  private bool disposed;

  public string DeviceName { get; }

  public int SampleRate { get; }

  public int Channels { get; }

  public WaveOutAudioOutput(int sampleRate = 44100, int channels = 1)
  {
    SampleRate = sampleRate;
    Channels = channels;
    DeviceName = "Default Audio Device";
  }

  public void Initialize(IAudioSource source)
  {
    if (source == null)
      throw new ArgumentNullException(nameof(source));

    if (source.SampleRate != SampleRate)
      throw new ArgumentException($"Source sample rate ({source.SampleRate}) does not match output sample rate ({SampleRate}).");

    if (source.Channels != Channels)
      throw new ArgumentException($"Source channels ({source.Channels}) do not match output channels ({Channels}).");

    this.source = source;

    // Create wave provider that wraps our audio source
    waveProvider = new SampleToWaveProvider(source);

    // Create wave out device
    waveOut = new WaveOutEvent();
    waveOut.Init(waveProvider);
  }

  public void Play()
  {
    if (waveOut == null)
      throw new InvalidOperationException("Output must be initialized before playing.");

    waveOut.Play();
  }

  public void Stop()
  {
    waveOut?.Stop();
  }

  public void Pause()
  {
    waveOut?.Pause();
  }

  public void Dispose()
  {
    if (disposed)
      return;

    waveOut?.Stop();
    waveOut?.Dispose();
    waveOut = null;

    source?.Dispose();
    source = null;

    disposed = true;
    GC.SuppressFinalize(this);
  }

  /// <summary>
  /// Adapter that converts IAudioSource to IWaveProvider for NAudio.
  /// </summary>
  private class SampleToWaveProvider : IWaveProvider
  {
    private readonly IAudioSource audioSource;
    private readonly WaveFormat waveFormat;

    public SampleToWaveProvider(IAudioSource audioSource)
    {
      this.audioSource = audioSource;
      waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(audioSource.SampleRate, audioSource.Channels);
    }

    public WaveFormat WaveFormat => waveFormat;

    public int Read(byte[] buffer, int offset, int count)
    {
      // Convert byte count to float count (4 bytes per float)
      int floatCount = count / 4;
      float[] floatBuffer = new float[floatCount];

      int samplesRead = audioSource.Read(floatBuffer, 0, floatCount);

      // Convert float samples to bytes
      Buffer.BlockCopy(floatBuffer, 0, buffer, offset, samplesRead * 4);

      return samplesRead * 4;
    }
  }
}
