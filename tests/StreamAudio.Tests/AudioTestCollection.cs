using Xunit;

namespace StreamAudio.Tests;

/// <summary>
/// Defines a collection for audio tests that share the AudioEngine.
/// Tests in this collection will run sequentially to avoid conflicts
/// with the shared AudioEngineManager singleton.
/// </summary>
[CollectionDefinition("AudioTests")]
public class AudioTestCollection : ICollectionFixture<AudioEngineFixture>
{
  // This class has no code, and is never created. Its purpose is simply
  // to be the place to apply [CollectionDefinition] and all the
  // ICollectionFixture<> interfaces.
}

/// <summary>
/// Fixture for managing the shared AudioEngine across tests.
/// This ensures proper initialization and cleanup of the audio engine.
/// </summary>
public class AudioEngineFixture : IDisposable
{
  public AudioEngineFixture()
  {
    // AudioEngine is lazily initialized on first access
  }

  public void Dispose()
  {
    // Clean up the audio engine when all tests in the collection are done
    StreamAudio.Core.AudioEngineManager.Dispose();
  }
}
