using SoundFlow.Abstracts;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Providers;
using SoundFlow.Structs;
using SoundFlow.Interfaces;
using StreamAudio.Core.Interfaces;

namespace StreamAudio.Core.Outputs;

/// <summary>
/// Audio output using SoundFlow for cross-platform playback.
/// </summary>
public class SoundFlowAudioOutput : IAudioOutput
{
  private readonly AudioEngine engine;
  private AudioPlaybackDevice? playbackDevice;
  private SoundPlayer? player;
  private IAudioSourceStream? sourceStream;
  private ISoundDataProvider? dataProvider;
  private IAudioSource? source;
  private bool disposed;

  public string DeviceName { get; }

  public int SampleRate { get; }

  public int Channels { get; }

  public SoundFlowAudioOutput(int sampleRate = 44100, int channels = 1)
  {
    SampleRate = sampleRate;
    Channels = channels;
    DeviceName = "Default Audio Device";
    
    // Create the SoundFlow audio engine
    engine = new MiniAudioEngine();
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

    // Create audio format
    var format = new AudioFormat(SampleRate, Channels);

    // Create a stream wrapper for our audio source
    sourceStream = new IAudioSourceStream(source);

    // Create data provider from the stream
    dataProvider = new StreamDataProvider(engine, format, sourceStream);

    // Create playback device
    playbackDevice = engine.CreatePlaybackDevice();
    playbackDevice.Start();

    // Create a player with the data provider
    player = new SoundPlayer(engine, format, dataProvider);
    
    // Add the player to the master mixer
    playbackDevice.MasterMixer.AddComponent(player);
  }

  public void Play()
  {
    if (player == null)
      throw new InvalidOperationException("Output must be initialized before playing.");

    player.Play();
  }

  public void Stop()
  {
    player?.Stop();
  }

  public void Pause()
  {
    player?.Pause();
  }

  public void Dispose()
  {
    if (disposed)
      return;

    if (player != null && playbackDevice != null)
    {
      player.Stop();
      playbackDevice.MasterMixer.RemoveComponent(player);
    }

    player?.Dispose();
    player = null;

    playbackDevice?.Stop();
    playbackDevice?.Dispose();
    playbackDevice = null;

    dataProvider?.Dispose();
    dataProvider = null;

    sourceStream?.Dispose();
    sourceStream = null;

    engine?.Dispose();

    source?.Dispose();
    source = null;

    disposed = true;
    GC.SuppressFinalize(this);
  }

  /// <summary>
  /// Stream wrapper that adapts IAudioSource to Stream for use with StreamDataProvider.
  /// </summary>
  private class IAudioSourceStream : Stream
  {
    private readonly IAudioSource audioSource;
    private long position;

    public IAudioSourceStream(IAudioSource audioSource)
    {
      this.audioSource = audioSource;
      position = 0;
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => long.MaxValue;

    public override long Position
    {
      get => position;
      set => throw new NotSupportedException("Seeking is not supported.");
    }

    public override void Flush() { }

    public override int Read(byte[] buffer, int offset, int count)
    {
      // Convert byte count to float count
      int floatCount = count / sizeof(float);
      float[] floatBuffer = new float[floatCount];

      // Read from the audio source
      int samplesRead = audioSource.Read(floatBuffer, 0, floatCount);

      // Convert floats to bytes
      Buffer.BlockCopy(floatBuffer, 0, buffer, offset, samplesRead * sizeof(float));

      int bytesRead = samplesRead * sizeof(float);
      position += bytesRead;
      return bytesRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
      throw new NotSupportedException("Seeking is not supported.");
    }

    public override void SetLength(long value)
    {
      throw new NotSupportedException("SetLength is not supported.");
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
      throw new NotSupportedException("Writing is not supported.");
    }
  }
}
