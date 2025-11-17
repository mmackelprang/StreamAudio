using NAudio.Wave;
using SoundFlow.Components;
using SoundFlow.Interfaces;
using SoundFlow.Providers;
using SoundFlow.Structs;
using StreamAudio.Core.Audio;
using System.Collections.Concurrent;

namespace StreamAudio.Core.Sources;

/// <summary>
/// Configuration for USB audio source.
/// </summary>
public class UsbAudioConfiguration
{
  /// <summary>
  /// Device number to use (-1 for default device)
  /// </summary>
  public int DeviceNumber { get; set; } = -1;

  /// <summary>
  /// Name/description of the device
  /// </summary>
  public string DeviceName { get; set; } = "USB Audio Device";

  /// <summary>
  /// Sample rate (default 44100 Hz)
  /// </summary>
  public int SampleRate { get; set; } = 44100;

  /// <summary>
  /// Number of channels (default 2 for stereo)
  /// </summary>
  public int Channels { get; set; } = 2;

  /// <summary>
  /// Bits per sample (default 16)
  /// </summary>
  public int BitsPerSample { get; set; } = 16;

  /// <summary>
  /// Buffer size in milliseconds (default 100ms)
  /// </summary>
  public int BufferMilliseconds { get; set; } = 100;
}

/// <summary>
/// Audio source that captures audio from a USB device (radio, turntable, etc).
/// Uses NAudio to capture from the device and provides it as an audio source.
/// </summary>
public class UsbAudioSource : IAudioSource
{
  private readonly UsbAudioConfiguration config;
  private WaveInEvent? waveIn;
  private SoundPlayer? player;
  private ISoundDataProvider? dataProvider;
  private CircularBuffer? audioBuffer;
  private bool disposed;
  private bool isCapturing;
  private readonly AudioFormat format;

  public UsbAudioSource(UsbAudioConfiguration config, AudioFormat? format = null)
  {
    this.config = config ?? throw new ArgumentNullException(nameof(config));
    this.format = format ?? AudioFormat.DvdHq;
  }

  /// <summary>
  /// Gets the name of the audio source.
  /// </summary>
  public string Name => config.DeviceName;

  /// <summary>
  /// Gets the audio format.
  /// </summary>
  public AudioFormat Format => format;

  /// <summary>
  /// Gets the sample rate.
  /// </summary>
  public int SampleRate => config.SampleRate;

  /// <summary>
  /// Gets the number of channels.
  /// </summary>
  public int Channels => config.Channels;

  /// <summary>
  /// USB sources are Manual type (long-running capture).
  /// </summary>
  public SourceType SourceType => SourceType.Manual;

  /// <summary>
  /// USB sources don't repeat (continuous capture).
  /// </summary>
  public int RepeatCount { get; set; } = 0; // 0 = infinite

  /// <summary>
  /// Gets metadata for USB audio (null - live capture doesn't have metadata).
  /// </summary>
  public SongMetadata? CurrentlyPlaying => null;

  /// <summary>
  /// Gets the underlying SoundPlayer.
  /// </summary>
  public SoundPlayer Player
  {
    get
    {
      if (player == null)
      {
        InitializePlayer();
      }
      return player!;
    }
  }

  /// <summary>
  /// Gets the current playback state.
  /// </summary>
  public SoundFlow.Enums.PlaybackState State => player?.State ?? SoundFlow.Enums.PlaybackState.Stopped;

  /// <summary>
  /// Starts capturing and playing audio from the USB device.
  /// </summary>
  public void Play()
  {
    if (disposed)
      throw new ObjectDisposedException(nameof(UsbAudioSource));

    if (player == null)
    {
      InitializePlayer();
    }

    StartCapture();
    player!.Play();
  }

  /// <summary>
  /// Pauses the audio.
  /// </summary>
  public void Pause()
  {
    player?.Pause();
    StopCapture();
  }

  /// <summary>
  /// Stops the audio.
  /// </summary>
  public void Stop()
  {
    player?.Stop();
    StopCapture();
  }

  private void InitializePlayer()
  {
    // Create a circular buffer for streaming audio
    // Size: 10 seconds of audio at configured sample rate
    var bufferSize = config.SampleRate * config.Channels * (config.BitsPerSample / 8) * 10;
    audioBuffer = new CircularBuffer(bufferSize);

    // Create data provider from buffer
    var engine = AudioEngineManager.Engine;
    dataProvider = new StreamDataProvider(engine, format, audioBuffer);

    // Create player
    player = new SoundPlayer(engine, format, dataProvider);
  }

  private void StartCapture()
  {
    if (isCapturing)
      return;

    try
    {
      // Check if device exists
      var deviceCount = WaveInEvent.DeviceCount;
      var actualDeviceNumber = config.DeviceNumber == -1 ? 0 : config.DeviceNumber;

      if (actualDeviceNumber < 0 || actualDeviceNumber >= deviceCount)
      {
        throw new InvalidOperationException(
          $"USB audio device {actualDeviceNumber} not found. Available devices: 0-{deviceCount - 1}");
      }

      // Create wave input
      waveIn = new WaveInEvent
      {
        DeviceNumber = actualDeviceNumber,
        WaveFormat = new WaveFormat(config.SampleRate, config.BitsPerSample, config.Channels),
        BufferMilliseconds = config.BufferMilliseconds
      };

      waveIn.DataAvailable += OnDataAvailable;
      waveIn.RecordingStopped += OnRecordingStopped;

      waveIn.StartRecording();
      isCapturing = true;
    }
    catch (Exception ex)
    {
      throw new InvalidOperationException($"Failed to start USB audio capture: {ex.Message}", ex);
    }
  }

  private void StopCapture()
  {
    if (!isCapturing || waveIn == null)
      return;

    try
    {
      waveIn.StopRecording();
      waveIn.DataAvailable -= OnDataAvailable;
      waveIn.RecordingStopped -= OnRecordingStopped;
      waveIn.Dispose();
      waveIn = null;
      isCapturing = false;
    }
    catch
    {
      // Ignore errors during cleanup
    }
  }

  private void OnDataAvailable(object? sender, WaveInEventArgs e)
  {
    if (audioBuffer != null && e.BytesRecorded > 0)
    {
      // Write captured data to circular buffer
      audioBuffer.Write(e.Buffer, 0, e.BytesRecorded);
    }
  }

  private void OnRecordingStopped(object? sender, StoppedEventArgs e)
  {
    if (e.Exception != null)
    {
      // Log or handle recording error
      Console.Error.WriteLine($"USB audio capture error: {e.Exception.Message}");
    }
    isCapturing = false;
  }

  public void Dispose()
  {
    if (disposed)
      return;

    disposed = true;
    StopCapture();
    player?.Stop();
    player?.Dispose();
    dataProvider?.Dispose();
    GC.SuppressFinalize(this);
  }

  /// <summary>
  /// Simple circular buffer for streaming audio data.
  /// </summary>
  private class CircularBuffer : Stream
  {
    private readonly byte[] buffer;
    private int writePosition;
    private int readPosition;
    private int dataAvailable;
    private readonly object lockObject = new();

    public CircularBuffer(int size)
    {
      buffer = new byte[size];
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => dataAvailable;
    public override long Position
    {
      get => readPosition;
      set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
      lock (lockObject)
      {
        var bytesToRead = Math.Min(count, dataAvailable);
        if (bytesToRead == 0)
          return 0;

        var firstChunk = Math.Min(bytesToRead, this.buffer.Length - readPosition);
        Array.Copy(this.buffer, readPosition, buffer, offset, firstChunk);

        if (firstChunk < bytesToRead)
        {
          var secondChunk = bytesToRead - firstChunk;
          Array.Copy(this.buffer, 0, buffer, offset + firstChunk, secondChunk);
        }

        readPosition = (readPosition + bytesToRead) % this.buffer.Length;
        dataAvailable -= bytesToRead;

        return bytesToRead;
      }
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
      lock (lockObject)
      {
        var bytesToWrite = Math.Min(count, this.buffer.Length - dataAvailable);
        if (bytesToWrite == 0)
          return; // Buffer full

        var firstChunk = Math.Min(bytesToWrite, this.buffer.Length - writePosition);
        Array.Copy(buffer, offset, this.buffer, writePosition, firstChunk);

        if (firstChunk < bytesToWrite)
        {
          var secondChunk = bytesToWrite - firstChunk;
          Array.Copy(buffer, offset + firstChunk, this.buffer, 0, secondChunk);
        }

        writePosition = (writePosition + bytesToWrite) % this.buffer.Length;
        dataAvailable += bytesToWrite;
      }
    }

    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
  }
}
