using SoundFlow.Components;
using StreamAudio.Core.Sources;
using StreamAudio.Core.Events;
using StreamAudio.Core.Configuration;
using System.Timers;

namespace StreamAudio.Core.Playback;

/// <summary>
/// Manages multiple audio streams with dynamic volume control, prioritization, and transitions.
/// Supports designating a primary stream that plays at full volume while background streams play at reduced volume.
/// </summary>
public class StreamManager : IDisposable
{
  private readonly IAudioPlayback playback;
  private readonly Dictionary<string, ManagedStream> streams = new();
  private readonly System.Timers.Timer monitorTimer;
  private string? primaryStreamId;
  private float backgroundVolume = 0.3f;
  private bool disposed;
  private bool wasPlaying = false;
  private readonly Dictionary<string, DateTime> completedAutoStreams = new();

  /// <summary>
  /// Occurs when a stream fails during playback.
  /// </summary>
  public event EventHandler<AudioEventArgs>? StreamFailed;

  /// <summary>
  /// Occurs when a stream is successfully recovered after a failure.
  /// </summary>
  public event EventHandler<AudioEventArgs>? StreamRecovered;

  /// <summary>
  /// Occurs when audio playback begins from an idle state.
  /// </summary>
  public event EventHandler? AudioPlayBegin;

  /// <summary>
  /// Occurs when all audio sources have completed playback.
  /// </summary>
  public event EventHandler? AllAudioComplete;

  /// <summary>
  /// Creates a new StreamManager with the specified playback device.
  /// </summary>
  /// <param name="playback">The IAudioPlayback instance to use for mixing.</param>
  public StreamManager(IAudioPlayback playback)
  {
    this.playback = playback ?? throw new ArgumentNullException(nameof(playback));
    
    ConfigurationManager.Instance.Logger.Information(
      "StreamManager initialized with playback device");
    
    // Set up a timer to monitor Auto sources
    monitorTimer = new System.Timers.Timer(100); // Check every 100ms
    monitorTimer.Elapsed += MonitorAutoSources;
    monitorTimer.Start();
  }

  /// <summary>
  /// Gets or sets the volume level for background streams (0.0 to 1.0).
  /// Default is 0.3 (30%).
  /// </summary>
  public float BackgroundVolume
  {
    get => backgroundVolume;
    set
    {
      if (value < 0.0f || value > 1.0f)
        throw new ArgumentOutOfRangeException(nameof(value), "Background volume must be between 0.0 and 1.0.");
      
      backgroundVolume = value;
      UpdateVolumeForAllStreams();
    }
  }

  /// <summary>
  /// Gets or sets the maximum duration for Auto audio streams in seconds.
  /// Default is 30 seconds. Set to 0 to disable the limit.
  /// </summary>
  public int MaxStreamDuration { get; set; } = 30;

  /// <summary>
  /// Gets the ID of the current primary stream, or null if none is set.
  /// </summary>
  public string? PrimaryStreamId => primaryStreamId;

  /// <summary>
  /// Gets the count of active streams.
  /// </summary>
  public int StreamCount => streams.Count;

  /// <summary>
  /// Adds a new audio source to the manager.
  /// </summary>
  /// <param name="id">Unique identifier for this stream.</param>
  /// <param name="source">The audio source to add.</param>
  /// <param name="isPrimary">If true, this stream becomes the primary stream.</param>
  public void AddSource(string id, IAudioSource source, bool isPrimary = false)
  {
    if (string.IsNullOrWhiteSpace(id))
      throw new ArgumentException("Stream ID cannot be null or empty.", nameof(id));
    
    if (source == null)
      throw new ArgumentNullException(nameof(source));

    if (streams.ContainsKey(id))
      throw new InvalidOperationException($"Stream with ID '{id}' already exists.");

    bool wasIdle = streams.Count == 0 || !AnyStreamPlaying();

    var managedStream = new ManagedStream
    {
      Source = source,
      TargetVolume = isPrimary ? 1.0f : backgroundVolume,
      CurrentVolume = 0.0f,
      IsMuted = false,
      StartTime = DateTime.UtcNow
    };

    streams[id] = managedStream;
    playback.AddPlayer(source.Player);
    playback.SetVolume(source.Player, 0.0f); // Start at 0 for fade-in or manual control

    ConfigurationManager.Instance.Logger.Information(
      "Added audio source: {Id} ({SourceName}, Type: {SourceType}, Primary: {IsPrimary})",
      id, source.Name, source.SourceType, isPrimary);

    if (isPrimary)
    {
      // Set as primary but don't update volume yet (will be set when Play is called)
      primaryStreamId = id;
    }

    // If we were idle and this is an Auto source that starts playing, fire AudioPlayBegin
    if (wasIdle && source.SourceType == SourceType.Auto)
    {
      AudioPlayBegin?.Invoke(this, EventArgs.Empty);
      wasPlaying = true;
    }
  }

  /// <summary>
  /// Removes a stream from the manager.
  /// </summary>
  /// <param name="id">The ID of the stream to remove.</param>
  /// <param name="fadeOut">If true, fades out before removing. If false, removes immediately.</param>
  public void RemoveSource(string id, bool fadeOut = true)
  {
    if (!streams.TryGetValue(id, out var managedStream))
      throw new InvalidOperationException($"Stream with ID '{id}' not found.");

    if (fadeOut)
    {
      FadeOut(id, onComplete: () =>
      {
        RemoveSourceInternal(id, managedStream);
      });
    }
    else
    {
      RemoveSourceInternal(id, managedStream);
    }
  }

  private void RemoveSourceInternal(string id, ManagedStream managedStream)
  {
    playback.RemovePlayer(managedStream.Source.Player);
    streams.Remove(id);

    ConfigurationManager.Instance.Logger.Information(
      "Removed audio source: {Id} ({SourceName})", 
      id, managedStream.Source.Name);

    // If we removed the primary stream, clear the primary ID
    if (primaryStreamId == id)
    {
      primaryStreamId = null;
    }
  }

  /// <summary>
  /// Sets the primary stream. The primary stream plays at full volume while others play at background volume.
  /// </summary>
  /// <param name="id">The ID of the stream to make primary.</param>
  public void SetPrimaryStream(string id)
  {
    if (!streams.ContainsKey(id))
      throw new InvalidOperationException($"Stream with ID '{id}' not found.");

    primaryStreamId = id;
    UpdateVolumeForAllStreams();
  }

  /// <summary>
  /// Clears the primary stream designation. All streams will play at background volume.
  /// </summary>
  public void ClearPrimaryStream()
  {
    primaryStreamId = null;
    UpdateVolumeForAllStreams();
  }

  /// <summary>
  /// Mutes a specific stream.
  /// </summary>
  /// <param name="id">The ID of the stream to mute.</param>
  public void Mute(string id)
  {
    if (!streams.TryGetValue(id, out var managedStream))
      throw new InvalidOperationException($"Stream with ID '{id}' not found.");

    managedStream.IsMuted = true;
    playback.SetVolume(managedStream.Source.Player, 0.0f);
  }

  /// <summary>
  /// Unmutes a specific stream.
  /// </summary>
  /// <param name="id">The ID of the stream to unmute.</param>
  public void Unmute(string id)
  {
    if (!streams.TryGetValue(id, out var managedStream))
      throw new InvalidOperationException($"Stream with ID '{id}' not found.");

    managedStream.IsMuted = false;
    UpdateVolumeForStream(id, managedStream);
  }

  /// <summary>
  /// Checks if a stream is muted.
  /// </summary>
  /// <param name="id">The ID of the stream to check.</param>
  /// <returns>True if the stream is muted, false otherwise.</returns>
  public bool IsMuted(string id)
  {
    if (!streams.TryGetValue(id, out var managedStream))
      throw new InvalidOperationException($"Stream with ID '{id}' not found.");

    return managedStream.IsMuted;
  }

  /// <summary>
  /// Fades in a stream over the specified duration.
  /// </summary>
  /// <param name="id">The ID of the stream to fade in.</param>
  /// <param name="durationMs">Duration of the fade in milliseconds. Default is 1000ms.</param>
  /// <param name="onComplete">Optional callback when fade completes.</param>
  public void FadeIn(string id, int durationMs = 1000, Action? onComplete = null)
  {
    if (!streams.TryGetValue(id, out var managedStream))
      throw new InvalidOperationException($"Stream with ID '{id}' not found.");

    if (durationMs < 0)
      throw new ArgumentOutOfRangeException(nameof(durationMs), "Duration must be non-negative.");

    var targetVolume = managedStream.IsMuted ? 0.0f : managedStream.TargetVolume;
    PerformFade(id, managedStream, targetVolume, durationMs, onComplete);
  }

  /// <summary>
  /// Fades out a stream over the specified duration.
  /// </summary>
  /// <param name="id">The ID of the stream to fade out.</param>
  /// <param name="durationMs">Duration of the fade in milliseconds. Default is 1000ms.</param>
  /// <param name="onComplete">Optional callback when fade completes.</param>
  public void FadeOut(string id, int durationMs = 1000, Action? onComplete = null)
  {
    if (!streams.TryGetValue(id, out var managedStream))
      throw new InvalidOperationException($"Stream with ID '{id}' not found.");

    if (durationMs < 0)
      throw new ArgumentOutOfRangeException(nameof(durationMs), "Duration must be non-negative.");

    PerformFade(id, managedStream, 0.0f, durationMs, onComplete);
  }

  /// <summary>
  /// Plays a stream.
  /// </summary>
  /// <param name="id">The ID of the stream to play.</param>
  /// <param name="fadeIn">If true, fades in the stream. Default is false.</param>
  public void Play(string id, bool fadeIn = false)
  {
    if (!streams.TryGetValue(id, out var managedStream))
      throw new InvalidOperationException($"Stream with ID '{id}' not found.");

    managedStream.Source.Play();

    if (fadeIn)
    {
      FadeIn(id);
    }
    else
    {
      UpdateVolumeForStream(id, managedStream);
    }
  }

  /// <summary>
  /// Pauses a stream.
  /// </summary>
  /// <param name="id">The ID of the stream to pause.</param>
  public void Pause(string id)
  {
    if (!streams.TryGetValue(id, out var managedStream))
      throw new InvalidOperationException($"Stream with ID '{id}' not found.");

    managedStream.Source.Pause();
  }

  /// <summary>
  /// Stops a stream.
  /// </summary>
  /// <param name="id">The ID of the stream to stop.</param>
  /// <param name="fadeOut">If true, fades out before stopping. Default is false.</param>
  public void Stop(string id, bool fadeOut = false)
  {
    if (!streams.TryGetValue(id, out var managedStream))
      throw new InvalidOperationException($"Stream with ID '{id}' not found.");

    if (fadeOut)
    {
      FadeOut(id, onComplete: () =>
      {
        managedStream.Source.Stop();
      });
    }
    else
    {
      managedStream.Source.Stop();
    }
  }

  /// <summary>
  /// Gets the current volume for a stream (0.0 to 1.0).
  /// </summary>
  /// <param name="id">The ID of the stream.</param>
  /// <returns>The current volume level.</returns>
  public float GetVolume(string id)
  {
    if (!streams.TryGetValue(id, out var managedStream))
      throw new InvalidOperationException($"Stream with ID '{id}' not found.");

    return playback.GetVolume(managedStream.Source.Player);
  }

  private void UpdateVolumeForAllStreams()
  {
    foreach (var kvp in streams)
    {
      UpdateVolumeForStream(kvp.Key, kvp.Value);
    }
  }

  private void UpdateVolumeForStream(string id, ManagedStream managedStream)
  {
    if (managedStream.IsMuted)
    {
      playback.SetVolume(managedStream.Source.Player, 0.0f);
      return;
    }

    float targetVolume = (id == primaryStreamId) ? 1.0f : backgroundVolume;
    managedStream.TargetVolume = targetVolume;
    managedStream.CurrentVolume = targetVolume;
    playback.SetVolume(managedStream.Source.Player, targetVolume);
  }

  private void PerformFade(string id, ManagedStream managedStream, float targetVolume, int durationMs, Action? onComplete)
  {
    var startVolume = playback.GetVolume(managedStream.Source.Player);
    var startTime = DateTime.UtcNow;
    var timer = new System.Timers.Timer(20); // Update every 20ms for smooth fade

    timer.Elapsed += (sender, e) =>
    {
      var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
      var progress = Math.Min(1.0, elapsed / durationMs);
      var currentVolume = startVolume + (targetVolume - startVolume) * (float)progress;

      managedStream.CurrentVolume = currentVolume;
      playback.SetVolume(managedStream.Source.Player, currentVolume);

      if (progress >= 1.0)
      {
        timer.Stop();
        timer.Dispose();
        onComplete?.Invoke();
      }
    };

    timer.Start();
  }

  /// <summary>
  /// Attempts to recover a failed stream by recreating it.
  /// </summary>
  /// <param name="id">The ID of the stream to recover.</param>
  /// <param name="filePath">The file path to recreate the source from.</param>
  /// <returns>True if recovery was successful, false otherwise.</returns>
  public bool TryRecoverStream(string id, string filePath)
  {
    if (!streams.TryGetValue(id, out var managedStream))
    {
      return false;
    }

    try
    {
      // Remove the old source
      playback.RemovePlayer(managedStream.Source.Player);
      managedStream.Source.Dispose();

      // Create a new source - only FileAudioSource for now
      var newSource = new FileAudioSource(filePath, playback.Format);
      managedStream.Source = newSource;

      // Re-add to playback
      playback.AddPlayer(newSource.Player);
      UpdateVolumeForStream(id, managedStream);

      // Raise recovery event
      StreamRecovered?.Invoke(this, new AudioEventArgs(id, "Stream recovered successfully"));
      
      return true;
    }
    catch (Exception ex)
    {
      // Raise failure event
      StreamFailed?.Invoke(this, new AudioEventArgs(id, "Failed to recover stream", ex));
      return false;
    }
  }

  /// <summary>
  /// Monitors all streams for errors and attempts automatic recovery.
  /// This should be called periodically by the application.
  /// </summary>
  public void MonitorStreams()
  {
    foreach (var kvp in streams.ToList())
    {
      var id = kvp.Key;
      var managedStream = kvp.Value;

      try
      {
        // Check if the player is in an error state
        // Note: SoundFlow doesn't expose error states directly, so we check if we can access the state
        var state = managedStream.Source.State;
      }
      catch (Exception ex)
      {
        // Stream has failed, raise event
        StreamFailed?.Invoke(this, new AudioEventArgs(id, "Stream playback error detected", ex));
      }
    }
  }

  /// <summary>
  /// Monitors Auto sources for completion and enforces MaxStreamDuration.
  /// </summary>
  private void MonitorAutoSources(object? sender, ElapsedEventArgs e)
  {
    if (disposed)
      return;

    var streamsToRemove = new List<string>();
    var now = DateTime.UtcNow;

    foreach (var kvp in streams.ToList())
    {
      var id = kvp.Key;
      var managedStream = kvp.Value;

      // Only process Auto sources
      if (managedStream.Source.SourceType != SourceType.Auto)
        continue;

      try
      {
        var state = managedStream.Source.State;

        // Check if stream has stopped
        if (state == SoundFlow.Enums.PlaybackState.Stopped)
        {
          // Track when it completed and delay removal by 500ms to allow fade-outs to complete
          if (!completedAutoStreams.ContainsKey(id))
          {
            completedAutoStreams[id] = now;
          }
          else if ((now - completedAutoStreams[id]).TotalMilliseconds >= 500)
          {
            streamsToRemove.Add(id);
            completedAutoStreams.Remove(id);
          }
          continue;
        }
        else
        {
          // Stream is playing again, remove from completed tracking
          completedAutoStreams.Remove(id);
        }

        // Check if stream has exceeded max duration
        if (MaxStreamDuration > 0)
        {
          var elapsed = (DateTime.UtcNow - managedStream.StartTime).TotalSeconds;
          if (elapsed >= MaxStreamDuration)
          {
            managedStream.Source.Stop();
            streamsToRemove.Add(id);
          }
        }
      }
      catch
      {
        // If there's an error accessing the state, mark for removal
        streamsToRemove.Add(id);
      }
    }

    // Remove completed Auto sources
    foreach (var id in streamsToRemove)
    {
      RemoveSourceInternal(id, streams[id]);
    }

    // Check if all audio is now complete
    if (wasPlaying && streams.Count == 0)
    {
      AllAudioComplete?.Invoke(this, EventArgs.Empty);
      wasPlaying = false;
    }
  }

  /// <summary>
  /// Checks if any stream is currently playing.
  /// </summary>
  private bool AnyStreamPlaying()
  {
    return streams.Values.Any(s => s.Source.State == SoundFlow.Enums.PlaybackState.Playing);
  }

  public void Dispose()
  {
    if (disposed)
      return;

    disposed = true;

    // Stop the monitor timer
    monitorTimer?.Stop();
    monitorTimer?.Dispose();

    // Stop and remove all streams
    foreach (var kvp in streams.ToList())
    {
      RemoveSourceInternal(kvp.Key, kvp.Value);
    }

    streams.Clear();
    GC.SuppressFinalize(this);
  }

  private class ManagedStream
  {
    public IAudioSource Source { get; set; } = null!;
    public float TargetVolume { get; set; }
    public float CurrentVolume { get; set; }
    public bool IsMuted { get; set; }
    public DateTime StartTime { get; set; }
  }
}
