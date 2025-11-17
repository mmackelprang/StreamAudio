using StreamAudio.Core;
using StreamAudio.Core.Playback;
using StreamAudio.Core.Sources;

namespace AutoSourceDemo;

/// <summary>
/// Demonstrates Auto vs Manual source lifecycle management (Phase 7 feature).
/// Shows how Auto sources are automatically removed when complete, while Manual sources persist.
/// </summary>
class Program
{
  private const string TestDataPath = "../../testdata";

  static void Main(string[] args)
  {
    Console.WriteLine("=== StreamAudio - Auto Source Lifecycle Demo ===\n");
    Console.WriteLine("This demo showcases Phase 7 auto source management:");
    Console.WriteLine("- Auto sources: System-initiated, short-lived (notifications, alerts)");
    Console.WriteLine("- Manual sources: User-controlled, persistent (music, radio)");
    Console.WriteLine("- MaxStreamDuration enforcement for Auto sources");
    Console.WriteLine("- AudioPlayBegin and AllAudioComplete events\n");

    try
    {
      RunDemos();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"\nError: {ex.Message}");
      Console.WriteLine("\nNote: This demo requires audio hardware and test files.");
    }
    finally
    {
      AudioEngineManager.Dispose();
    }

    Console.WriteLine("\nPress any key to exit...");
    Console.ReadKey();
  }

  static void RunDemos()
  {
    // Check if test files exist
    if (!Directory.Exists(TestDataPath))
    {
      Console.WriteLine($"Error: Test data directory not found at {TestDataPath}");
      Console.WriteLine("Please run ToneGenerator to create test audio files first.");
      return;
    }

    var file100Hz = Path.Combine(TestDataPath, "100hz.wav");
    var file200Hz = Path.Combine(TestDataPath, "200hz.wav");

    if (!File.Exists(file100Hz) || !File.Exists(file200Hz))
    {
      Console.WriteLine("Error: Test audio files not found.");
      Console.WriteLine("Please run ToneGenerator to create test files first.");
      return;
    }

    Console.WriteLine("Press any key to start demos...");
    Console.ReadKey();
    Console.WriteLine();

    Demo1_AutoSourceLifecycle(file100Hz);
    Demo2_ManualSourcePersistence(file100Hz);
    Demo3_MixedSourceTypes(file100Hz, file200Hz);
    Demo4_MaxStreamDuration(file100Hz);
    Demo5_StreamManagerEvents(file100Hz);
  }

  static void Demo1_AutoSourceLifecycle(string file100Hz)
  {
    Console.WriteLine("\n--- Demo 1: Auto Source Lifecycle ---");
    Console.WriteLine("Auto sources are automatically removed when playback completes.\n");

    using var playback = new AudioPlayback();
    using var manager = new StreamManager(playback);
    using var autoSource = new FileAudioSource(file100Hz, sourceType: SourceType.Auto);
    
    autoSource.Loop = false; // Play once and stop

    Console.WriteLine($"Initial stream count: {manager.StreamCount}");
    
    manager.AddSource("notification", autoSource);
    Console.WriteLine($"After adding Auto source: {manager.StreamCount} stream(s)");
    
    manager.Play("notification");
    Console.WriteLine("Playing Auto source (notification sound)...");
    
    // Wait for audio to complete
    Thread.Sleep(1500);
    
    Console.WriteLine($"After completion: {manager.StreamCount} stream(s)");
    Console.WriteLine("Auto source was automatically removed!");
  }

  static void Demo2_ManualSourcePersistence(string file100Hz)
  {
    Console.WriteLine("\n\n--- Demo 2: Manual Source Persistence ---");
    Console.WriteLine("Manual sources persist until explicitly removed.\n");

    using var playback = new AudioPlayback();
    using var manager = new StreamManager(playback);
    using var manualSource = new FileAudioSource(file100Hz, sourceType: SourceType.Manual);
    
    manualSource.Loop = false; // Even if it stops playing

    Console.WriteLine($"Initial stream count: {manager.StreamCount}");
    
    manager.AddSource("music", manualSource);
    Console.WriteLine($"After adding Manual source: {manager.StreamCount} stream(s)");
    
    manager.Play("music");
    Console.WriteLine("Playing Manual source (background music)...");
    
    Thread.Sleep(1500);
    
    Console.WriteLine($"After playback ends: {manager.StreamCount} stream(s)");
    Console.WriteLine("Manual source still exists! Must be explicitly removed.");
    
    manager.RemoveSource("music", fadeOut: false);
    Console.WriteLine($"After explicit removal: {manager.StreamCount} stream(s)");
  }

  static void Demo3_MixedSourceTypes(string file100Hz, string file200Hz)
  {
    Console.WriteLine("\n\n--- Demo 3: Mixed Source Types ---");
    Console.WriteLine("Combining Manual and Auto sources.\n");

    using var playback = new AudioPlayback();
    using var manager = new StreamManager(playback);
    using var manualSource = new FileAudioSource(file100Hz, sourceType: SourceType.Manual);
    using var autoSource = new FileAudioSource(file200Hz, sourceType: SourceType.Auto);
    
    manualSource.Loop = true;
    autoSource.Loop = false;

    manager.AddSource("background_music", manualSource, isPrimary: true);
    manager.AddSource("alert", autoSource);
    
    Console.WriteLine($"Added sources: {manager.StreamCount} stream(s)");
    Console.WriteLine("  - background_music (Manual)");
    Console.WriteLine("  - alert (Auto)");
    
    manager.Play("background_music");
    manager.Play("alert");
    
    Console.WriteLine("\nPlaying both sources...");
    Console.WriteLine("Background music is Manual (will persist)");
    Console.WriteLine("Alert is Auto (will auto-remove when done)");
    
    Thread.Sleep(1500);
    
    Console.WriteLine($"\nAfter alert completes: {manager.StreamCount} stream(s)");
    Console.WriteLine("Only background_music remains!");
  }

  static void Demo4_MaxStreamDuration(string file100Hz)
  {
    Console.WriteLine("\n\n--- Demo 4: MaxStreamDuration Enforcement ---");
    Console.WriteLine("Auto sources are limited by MaxStreamDuration.\n");

    using var playback = new AudioPlayback();
    using var manager = new StreamManager(playback);
    
    // Set a short max duration for demo
    manager.MaxStreamDuration = 2; // 2 seconds
    Console.WriteLine($"MaxStreamDuration set to: {manager.MaxStreamDuration} seconds");
    
    using var autoSource = new FileAudioSource(file100Hz, sourceType: SourceType.Auto);
    autoSource.Loop = true; // Try to loop forever
    autoSource.RepeatCount = 0; // Infinite repeats

    manager.AddSource("long_alert", autoSource);
    manager.Play("long_alert");
    
    Console.WriteLine("\nAuto source set to loop infinitely...");
    Console.WriteLine("But MaxStreamDuration will stop it!");
    
    Thread.Sleep(2500); // Wait longer than MaxStreamDuration
    
    Console.WriteLine($"\nAfter {manager.MaxStreamDuration} seconds:");
    Console.WriteLine($"Stream count: {manager.StreamCount}");
    Console.WriteLine("Auto source was stopped and removed by MaxStreamDuration!");
  }

  static void Demo5_StreamManagerEvents(string file100Hz)
  {
    Console.WriteLine("\n\n--- Demo 5: Stream Manager Events ---");
    Console.WriteLine("Monitoring AudioPlayBegin and AllAudioComplete events.\n");

    using var playback = new AudioPlayback();
    using var manager = new StreamManager(playback);
    
    bool audioPlayBeginFired = false;
    bool allAudioCompleteFired = false;
    
    manager.AudioPlayBegin += (sender, e) =>
    {
      Console.WriteLine("[EVENT] AudioPlayBegin - Audio started from idle");
      audioPlayBeginFired = true;
    };
    
    manager.AllAudioComplete += (sender, e) =>
    {
      Console.WriteLine("[EVENT] AllAudioComplete - All audio finished");
      allAudioCompleteFired = true;
    };

    using var autoSource = new FileAudioSource(file100Hz, sourceType: SourceType.Auto);
    autoSource.Loop = false;

    Console.WriteLine("No audio playing yet...");
    Console.WriteLine($"Stream count: {manager.StreamCount}\n");
    
    manager.AddSource("beep", autoSource);
    manager.Play("beep");
    
    Thread.Sleep(100); // Give event time to fire
    Console.WriteLine($"\nAudioPlayBegin fired: {audioPlayBeginFired}");
    
    Console.WriteLine("\nWaiting for audio to complete...");
    Thread.Sleep(1500);
    
    Console.WriteLine($"AllAudioComplete fired: {allAudioCompleteFired}");
    Console.WriteLine($"Final stream count: {manager.StreamCount}");
  }
}
