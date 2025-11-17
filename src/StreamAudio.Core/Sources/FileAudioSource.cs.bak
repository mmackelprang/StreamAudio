using SoundFlow.Components;
using SoundFlow.Interfaces;
using SoundFlow.Providers;
using SoundFlow.Structs;
using StreamAudio.Core.Audio;

namespace StreamAudio.Core.Sources;

/// <summary>
/// Audio source that reads from an audio file using SoundFlow.
/// Wraps a SoundPlayer for easy file playback with looping support.
/// </summary>
public class FileAudioSource : IAudioSource
{
  private readonly SoundPlayer player;
  private readonly ISoundDataProvider dataProvider;
  private readonly FileStream fileStream;
  private readonly string filePath;
  private readonly System.Timers.Timer? loopTimer;
  private bool disposed;
  private int repeatCount = 1;
  private int currentPlayCount = 0;
  private SongMetadata? currentlyPlaying;

  public FileAudioSource(string filePath, AudioFormat? format = null, SourceType sourceType = SourceType.Manual)
  {
    if (string.IsNullOrWhiteSpace(filePath))
      throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

    if (!File.Exists(filePath))
      throw new FileNotFoundException($"Audio file not found: {filePath}");

    this.filePath = filePath;
    this.SourceType = sourceType;

    // Use provided format or default to DVD HQ quality
    format ??= AudioFormat.DvdHq;

    // Open file stream
    fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

    // Create data provider from stream
    var engine = AudioEngineManager.Engine;
    dataProvider = new StreamDataProvider(engine, format.Value, fileStream);

    // Create player
    player = new SoundPlayer(engine, format.Value, dataProvider);

    // Set up a timer to check for playback end and loop if needed
    loopTimer = new System.Timers.Timer(100); // Check every 100ms
    loopTimer.Elapsed += (sender, e) =>
    {
      if (Loop && player.State == SoundFlow.Enums.PlaybackState.Stopped && !disposed)
      {
        // Check if we should repeat based on RepeatCount
        currentPlayCount++;
        if (RepeatCount == 0 || currentPlayCount < RepeatCount)
        {
          // Seek back to beginning
          try
          {
            fileStream.Seek(0, SeekOrigin.Begin);
            player.Play();
          }
          catch
          {
            // Ignore errors during loop
          }
        }
      }
    };
    loopTimer.Start();
  }

  /// <summary>
  /// Gets the file name.
  /// </summary>
  public string Name => Path.GetFileName(filePath);

  /// <summary>
  /// Gets the audio format.
  /// </summary>
  public AudioFormat Format => player.Format;

  /// <summary>
  /// Gets the sample rate.
  /// </summary>
  public int SampleRate => player.Format.SampleRate;

  /// <summary>
  /// Gets the number of channels.
  /// </summary>
  public int Channels => player.Format.Channels;

  /// <summary>
  /// Gets the type of this source (Manual or Auto).
  /// </summary>
  public SourceType SourceType { get; }

  /// <summary>
  /// Gets or sets the number of times to repeat this source.
  /// 0 means infinite loop (only applies to Auto sources).
  /// Default is 1 (play once).
  /// </summary>
  public int RepeatCount
  {
    get => repeatCount;
    set
    {
      if (value < 0)
        throw new ArgumentOutOfRangeException(nameof(value), "RepeatCount must be non-negative.");
      repeatCount = value;
      currentPlayCount = 0; // Reset the play count when setting new repeat count
    }
  }

  /// <summary>
  /// Gets or sets whether the audio should loop.
  /// Note: Looping is handled by monitoring playback state and restarting when finished.
  /// </summary>
  public bool Loop { get; set; }

  /// <summary>
  /// Gets metadata for the currently playing file, if available.
  /// For file sources, this will be null initially but can be populated
  /// with file metadata extraction in the future.
  /// </summary>
  public SongMetadata? CurrentlyPlaying => currentlyPlaying;

  /// <summary>
  /// Gets the underlying SoundPlayer for advanced operations.
  /// </summary>
  public SoundPlayer Player => player;

  /// <summary>
  /// Plays the audio.
  /// </summary>
  public void Play()
  {
    currentPlayCount = 0; // Reset count when explicitly played
    player.Play();
  }

  /// <summary>
  /// Pauses the audio.
  /// </summary>
  public void Pause() => player.Pause();

  /// <summary>
  /// Stops the audio.
  /// </summary>
  public void Stop() => player.Stop();

  /// <summary>
  /// Gets the current playback state.
  /// </summary>
  public SoundFlow.Enums.PlaybackState State => player.State;

  public void Dispose()
  {
    if (disposed)
      return;

    disposed = true;
    loopTimer?.Stop();
    loopTimer?.Dispose();
    player?.Stop();
    player?.Dispose();
    dataProvider?.Dispose();
    fileStream?.Dispose();
    GC.SuppressFinalize(this);
  }
}
