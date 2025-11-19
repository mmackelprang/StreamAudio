using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Interfaces;
using SoundFlow.Providers;
using SoundFlow.Structs;
using StreamAudio.Core.Audio;
using System.Diagnostics;

namespace StreamAudio.Core.Sources;

/// <summary>
/// WIP!!!
/// An abstract base class for audio sources that provides common functionality for playback,
/// looping, and repeating. It uses a timer to handle the playback lifecycle.
/// </summary>
public abstract class BaseAudioSource : IAudioSource
{
  protected SoundPlayer? player;
  protected ISoundDataProvider? dataProvider;
  private bool disposed;
  private int repeatCount = 1;
  private bool loop = false;
  private int currentPlayCount = 0;
  private readonly System.Timers.Timer? loopTimer;

  protected BaseAudioSource()
  {
    loopTimer = new System.Timers.Timer(50); // Check frequently
    loopTimer.Elapsed += HandlePlaybackEnd;
    loopTimer.Start();
  }

  /// <summary>
  /// Initializes the audio source by creating the data provider and player.
  /// </summary>
  /// <param name="provider">The sound data provider for the audio source.</param>
  /// <param name="format">The audio format for the player.</param>
  protected void Initialize(ISoundDataProvider provider, AudioFormat format)
  {
    dataProvider = provider;
    player = new SoundPlayer(AudioEngineManager.Engine, format, dataProvider);
  }

  private void HandlePlaybackEnd(object? sender, System.Timers.ElapsedEventArgs e)
  {
    if (disposed || player == null || player.State != PlaybackState.Stopped)
    {
      return;
    }

    bool isInfinite = Loop || RepeatCount == 0;

    if (!isInfinite && currentPlayCount >= RepeatCount)
    {
      return; // All repeats are done
    }

    if (!isInfinite)
    {
      currentPlayCount++;
    }

    try
    {
      // For sources that need to be re-initialized (like TTS), this can be overridden.
      // For file streams, we can just seek to the beginning.
      RestartPlayback();
    }
    catch (Exception ex)
    {
      Debug.WriteLine($"Error restarting source: {ex.Message}");
    }
  }

  /// <summary>
  /// Restarts the playback. The default implementation simply calls Play again.
  /// This should be overridden by derived classes if more complex restart logic is needed
  /// (e.g., re-initializing a provider).
  /// </summary>
  protected virtual void RestartPlayback()
  {
    player?.Play();
  }

  public abstract string Name { get; }
  public AudioFormat Format => player?.Format ?? default;
  public int SampleRate => player?.Format.SampleRate ?? 0;
  public int Channels => player?.Format.Channels ?? 0;
  public abstract SourceType SourceType { get; }
  public abstract SongMetadata? CurrentlyPlaying { get; }
  
  public SoundPlayer Player => player ?? throw new InvalidOperationException("Player not initialized");

  public int RepeatCount
  {
    get => repeatCount;
    set
    {
      if (value < 0)
        throw new ArgumentOutOfRangeException(nameof(value), "RepeatCount must be non-negative.");
      repeatCount = value;
    }
  }

  public bool Loop
  {
    get => loop;
    set => loop = value;
  }

  public PlaybackState State => player?.State ?? PlaybackState.Stopped;

  public void Play()
  {
    currentPlayCount = 1;
    player?.Play();
  }

  public void Pause() => player?.Pause();

  public void Stop() => player?.Stop();

  public virtual void Dispose()
  {
    if (disposed)
      return;

    disposed = true;
    loopTimer?.Stop();
    loopTimer?.Dispose();
    
    player?.Stop();
    player?.Dispose();
    
    dataProvider?.Dispose();
    GC.SuppressFinalize(this);
  }
}
