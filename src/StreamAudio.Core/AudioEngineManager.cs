using SoundFlow.Abstracts;
using SoundFlow.Backends.MiniAudio;

namespace StreamAudio.Core;

/// <summary>
/// Manages the global AudioEngine instance for the application.
/// SoundFlow requires a single engine instance that manages all audio operations.
/// </summary>
public static class AudioEngineManager
{
  private static AudioEngine? _engine;
  private static readonly object _lock = new();
  private static bool _isDisposed = false;

  /// <summary>
  /// Gets the shared AudioEngine instance, creating it if necessary.
  /// </summary>
  public static AudioEngine Engine
  {
    get
    {
      lock (_lock)
      {
        if (_engine == null && !_isDisposed)
        {
          _engine = new MiniAudioEngine();
          _isDisposed = false;
        }
      }
      return _engine ?? throw new ObjectDisposedException(nameof(AudioEngineManager));
    }
  }

  /// <summary>
  /// Disposes the shared AudioEngine instance.
  /// Should be called when the application is shutting down.
  /// Safe to call multiple times - subsequent calls are no-ops.
  /// </summary>
  public static void Dispose()
  {
    lock (_lock)
    {
      if (_engine != null && !_isDisposed)
      {
        try
        {
          _engine.Dispose();
        }
        catch
        {
          // Swallow exceptions during disposal to prevent crashes in cleanup code
          // This is especially important for tests running in parallel
        }
        finally
        {
          _engine = null;
          _isDisposed = true;
        }
      }
    }
  }

  /// <summary>
  /// Resets the disposed state, allowing a new engine to be created.
  /// This is primarily for testing scenarios.
  /// </summary>
  public static void Reset()
  {
    lock (_lock)
    {
      _isDisposed = false;
    }
  }
}
