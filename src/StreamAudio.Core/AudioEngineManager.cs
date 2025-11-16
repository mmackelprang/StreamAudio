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

  /// <summary>
  /// Gets the shared AudioEngine instance, creating it if necessary.
  /// </summary>
  public static AudioEngine Engine
  {
    get
    {
      if (_engine == null)
      {
        lock (_lock)
        {
          _engine ??= new MiniAudioEngine();
        }
      }
      return _engine;
    }
  }

  /// <summary>
  /// Disposes the shared AudioEngine instance.
  /// Should be called when the application is shutting down.
  /// </summary>
  public static void Dispose()
  {
    lock (_lock)
    {
      _engine?.Dispose();
      _engine = null;
    }
  }
}
