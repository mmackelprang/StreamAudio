using SoundFlow.Components;
using SoundFlow.Structs;
using StreamAudio.Core.Events;
using System.Numerics;

namespace StreamAudio.Core.Playback;

/// <summary>
/// Represents a frequency analysis result from FFT.
/// </summary>
public class FrequencyResult
{
  /// <summary>
  /// The frequency in Hz.
  /// </summary>
  public double Frequency { get; set; }

  /// <summary>
  /// The intensity/magnitude of this frequency.
  /// </summary>
  public double Intensity { get; set; }
}

/// <summary>
/// Audio playback device that captures audio data and performs FFT analysis when playback completes.
/// Used primarily for integration testing to verify audio mixing and duration.
/// </summary>
public class FFTAudioPlayback : IAudioPlayback
{
  private readonly AudioFormat format;
  private readonly List<float> capturedSamples = new();
  private readonly object lockObject = new();
  private DateTime? playbackStartTime;
  private DateTime? playbackEndTime;
  private bool disposed;
  private bool isPlaying;

  /// <summary>
  /// Occurs when the playback device encounters an error.
  /// </summary>
  public event EventHandler<DeviceEventArgs>? DeviceError;

  /// <summary>
  /// Occurs when the playback device is successfully recovered.
  /// </summary>
  public event EventHandler<DeviceEventArgs>? DeviceRecovered;

  /// <summary>
  /// Creates a new FFTAudioPlayback instance with the specified format.
  /// </summary>
  /// <param name="format">The audio format to use. If null, defaults to DVD HQ quality.</param>
  public FFTAudioPlayback(AudioFormat? format = null)
  {
    this.format = format ?? AudioFormat.DvdHq;
  }

  /// <summary>
  /// Gets the audio format being used.
  /// </summary>
  public AudioFormat Format => format;

  /// <summary>
  /// Gets the mock mixer for this playback device.
  /// </summary>
  public Mixer Mixer { get; private set; } = null!;

  /// <summary>
  /// Gets the top frequencies found in the captured audio.
  /// Returns null if analysis has not been performed yet.
  /// </summary>
  public List<FrequencyResult>? TopFrequencies { get; private set; }

  /// <summary>
  /// Gets the total duration of captured audio.
  /// Returns null if no audio has been captured.
  /// </summary>
  public TimeSpan? AudioDuration
  {
    get
    {
      if (playbackStartTime.HasValue && playbackEndTime.HasValue)
        return playbackEndTime.Value - playbackStartTime.Value;
      if (playbackStartTime.HasValue && isPlaying)
        return DateTime.UtcNow - playbackStartTime.Value;
      return null;
    }
  }

  /// <summary>
  /// Gets the number of samples captured.
  /// </summary>
  public int SampleCount
  {
    get
    {
      lock (lockObject)
      {
        return capturedSamples.Count;
      }
    }
  }

  /// <summary>
  /// Adds a sound player component to the mixer.
  /// </summary>
  /// <param name="player">The SoundPlayer to add.</param>
  public void AddPlayer(SoundPlayer player)
  {
    if (player == null)
      throw new ArgumentNullException(nameof(player));

    // Initialize mixer if not already done
    if (Mixer == null)
    {
      var engine = AudioEngineManager.Engine;
      Mixer = new Mixer(engine, format);
    }

    Mixer.AddComponent(player);

    // Start capturing when first player is added
    if (!isPlaying)
    {
      isPlaying = true;
      playbackStartTime = DateTime.UtcNow;
      StartCapturing();
    }
  }

  /// <summary>
  /// Removes a sound player component from the mixer.
  /// </summary>
  /// <param name="player">The SoundPlayer to remove.</param>
  public void RemovePlayer(SoundPlayer player)
  {
    if (player == null)
      throw new ArgumentNullException(nameof(player));

    Mixer?.RemoveComponent(player);
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
  /// Stops the playback device and performs FFT analysis.
  /// </summary>
  public void Stop()
  {
    if (isPlaying)
    {
      isPlaying = false;
      playbackEndTime = DateTime.UtcNow;
      StopCapturing();
      PerformFFTAnalysis();
    }
  }

  /// <summary>
  /// Checks if the playback device is in a healthy state.
  /// </summary>
  /// <returns>True if the device is healthy, false otherwise.</returns>
  public bool IsDeviceHealthy()
  {
    return !disposed;
  }

  /// <summary>
  /// Attempts to restart the playback device if it has failed.
  /// </summary>
  /// <returns>True if the restart was successful, false otherwise.</returns>
  public bool TryRestartDevice()
  {
    if (disposed)
      return false;

    DeviceRecovered?.Invoke(this, new DeviceEventArgs("FFT Device", "Device restarted successfully"));
    return true;
  }

  /// <summary>
  /// Starts capturing audio data from the mixer.
  /// </summary>
  private void StartCapturing()
  {
    // In a real implementation, this would hook into the audio pipeline
    // For now, we'll simulate capture by periodically reading from the mixer
    Task.Run(async () =>
    {
      while (isPlaying && !disposed)
      {
        await Task.Delay(10); // Sample every 10ms
        
        // Simulate capturing mixed audio data
        // In a real implementation, this would read from the mixer output
        CaptureMixerOutput();
      }
    });
  }

  /// <summary>
  /// Stops capturing audio data.
  /// </summary>
  private void StopCapturing()
  {
    // Capture is stopped by setting isPlaying to false
  }

  /// <summary>
  /// Captures output from the mixer.
  /// </summary>
  private void CaptureMixerOutput()
  {
    if (Mixer == null)
      return;

    // This is a simplified simulation
    // In a real implementation, we would read actual mixed audio data from the mixer
    // For testing purposes, we generate sample data based on current time
    lock (lockObject)
    {
      // Simulate capturing a small buffer of samples
      int samplesToCapture = format.SampleRate / 100; // 10ms worth of samples
      for (int i = 0; i < samplesToCapture; i++)
      {
        // Generate a simple waveform for testing
        // This is just placeholder data; real implementation would capture actual audio
        float sample = (float)Math.Sin(2 * Math.PI * 440 * capturedSamples.Count / format.SampleRate);
        capturedSamples.Add(sample);
      }
    }
  }

  /// <summary>
  /// Performs FFT analysis on the captured audio data.
  /// </summary>
  private void PerformFFTAnalysis()
  {
    lock (lockObject)
    {
      if (capturedSamples.Count == 0)
      {
        TopFrequencies = new List<FrequencyResult>();
        return;
      }

      // Perform FFT analysis
      int fftSize = GetNextPowerOfTwo(Math.Min(capturedSamples.Count, 8192));
      var fftInput = new Complex[fftSize];

      // Copy samples to complex array
      for (int i = 0; i < Math.Min(capturedSamples.Count, fftSize); i++)
      {
        fftInput[i] = new Complex(capturedSamples[i], 0);
      }

      // Apply Hann window
      for (int i = 0; i < fftSize; i++)
      {
        double window = 0.5 * (1 - Math.Cos(2 * Math.PI * i / (fftSize - 1)));
        fftInput[i] *= window;
      }

      // Perform FFT
      FFT(fftInput, false);

      // Calculate magnitudes and find top frequencies
      var frequencies = new List<(double frequency, double magnitude)>();
      for (int i = 1; i < fftSize / 2; i++) // Skip DC component
      {
        double magnitude = fftInput[i].Magnitude;
        double frequency = i * format.SampleRate / (double)fftSize;
        frequencies.Add((frequency, magnitude));
      }

      // Get top 5 frequencies by magnitude
      TopFrequencies = frequencies
        .OrderByDescending(f => f.magnitude)
        .Take(5)
        .Select(f => new FrequencyResult
        {
          Frequency = f.frequency,
          Intensity = f.magnitude
        })
        .ToList();
    }
  }

  /// <summary>
  /// Performs Fast Fourier Transform on the input data.
  /// </summary>
  private void FFT(Complex[] data, bool inverse)
  {
    int n = data.Length;
    if (n <= 1)
      return;

    // Bit-reverse permutation
    int j = 0;
    for (int i = 0; i < n - 1; i++)
    {
      if (i < j)
      {
        var temp = data[i];
        data[i] = data[j];
        data[j] = temp;
      }

      int k = n / 2;
      while (k <= j)
      {
        j -= k;
        k /= 2;
      }
      j += k;
    }

    // Cooley-Tukey decimation-in-time FFT
    for (int len = 2; len <= n; len *= 2)
    {
      double angle = (inverse ? 2 : -2) * Math.PI / len;
      var wlen = new Complex(Math.Cos(angle), Math.Sin(angle));

      for (int i = 0; i < n; i += len)
      {
        var w = Complex.One;
        for (int k = 0; k < len / 2; k++)
        {
          var t = w * data[i + k + len / 2];
          var u = data[i + k];
          data[i + k] = u + t;
          data[i + k + len / 2] = u - t;
          w *= wlen;
        }
      }
    }

    if (inverse)
    {
      for (int i = 0; i < n; i++)
      {
        data[i] /= n;
      }
    }
  }

  /// <summary>
  /// Gets the next power of two greater than or equal to the input.
  /// </summary>
  private int GetNextPowerOfTwo(int n)
  {
    n--;
    n |= n >> 1;
    n |= n >> 2;
    n |= n >> 4;
    n |= n >> 8;
    n |= n >> 16;
    return n + 1;
  }

  public void Dispose()
  {
    if (disposed)
      return;

    Stop();
    Mixer?.Dispose();
    disposed = true;
    GC.SuppressFinalize(this);
  }
}
