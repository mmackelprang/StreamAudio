using StreamAudio.Core;
using StreamAudio.Core.Playback;
using StreamAudio.Core.Sources;

namespace StreamDemo;

/// <summary>
/// Demonstrates the StreamManager's dynamic stream management capabilities.
/// Shows primary/background volume control, mute/unmute, and fade transitions.
/// </summary>
class Program
{
  private const string TestDataPath = "./testdata";

  static void Main(string[] args)
  {
    Console.WriteLine("=== StreamAudio - Dynamic Stream Management Demo ===\n");
    Console.WriteLine("This demo showcases Phase 4 features:");
    Console.WriteLine("- Dynamic stream management");
    Console.WriteLine("- Primary/background volume prioritization");
    Console.WriteLine("- Mute/unmute controls");
    Console.WriteLine("- Fade-in/fade-out transitions\n");

    try
    {
      RunDemos();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"\nError: {ex.Message}");
      Console.WriteLine("\nNote: This demo requires audio hardware. It may not work in headless environments.");
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

    var file1 = Path.Combine(TestDataPath, "100hz.wav");
    var file2 = Path.Combine(TestDataPath, "200hz.wav");

    if (!File.Exists(file1) || !File.Exists(file2))
    {
      Console.WriteLine("Error: Test audio files not found.");
      Console.WriteLine("Please run ToneGenerator to create 100hz.wav and 200hz.wav first.");
      return;
    }

    Console.WriteLine("Press any key to start demos...");
    Console.ReadKey();
    Console.WriteLine();

    Demo1_PrimaryBackgroundControl(file1, file2);
    Demo2_MuteUnmute(file1, file2);
    Demo3_FadeTransitions(file1, file2);
    Demo4_DynamicStreamManagement(file1, file2);
  }

  static void Demo1_PrimaryBackgroundControl(string file1, string file2)
  {
    Console.WriteLine("\n--- Demo 1: Primary/Background Volume Control ---");
    Console.WriteLine("Playing two tones: 100Hz (primary) and 200Hz (background at 30%)");

    using var playback = new AudioPlayback();
    using var manager = new StreamManager(playback);
    using var source1 = new FileAudioSource(file1) { Loop = true };
    using var source2 = new FileAudioSource(file2) { Loop = true };

    manager.AddSource("tone100", source1, isPrimary: true);
    manager.AddSource("tone200", source2);

    manager.Play("tone100");
    manager.Play("tone200");

    Console.WriteLine($"Primary stream: {manager.PrimaryStreamId}");
    Console.WriteLine($"100Hz volume: {manager.GetVolume("tone100"):F2}");
    Console.WriteLine($"200Hz volume: {manager.GetVolume("tone200"):F2}");
    Console.WriteLine("Listen for 3 seconds...");
    Thread.Sleep(3000);

    Console.WriteLine("\nSwitching primary to 200Hz...");
    manager.SetPrimaryStream("tone200");
    Console.WriteLine($"Primary stream: {manager.PrimaryStreamId}");
    Console.WriteLine($"100Hz volume: {manager.GetVolume("tone100"):F2}");
    Console.WriteLine($"200Hz volume: {manager.GetVolume("tone200"):F2}");
    Console.WriteLine("Listen for 3 seconds...");
    Thread.Sleep(3000);

    Console.WriteLine("\nClearing primary (all at background volume)...");
    manager.ClearPrimaryStream();
    Console.WriteLine($"Primary stream: {(manager.PrimaryStreamId ?? "none")}");
    Console.WriteLine($"100Hz volume: {manager.GetVolume("tone100"):F2}");
    Console.WriteLine($"200Hz volume: {manager.GetVolume("tone200"):F2}");
    Console.WriteLine("Listen for 2 seconds...");
    Thread.Sleep(2000);

    manager.Stop("tone100");
    manager.Stop("tone200");
  }

  static void Demo2_MuteUnmute(string file1, string file2)
  {
    Console.WriteLine("\n--- Demo 2: Mute/Unmute Controls ---");
    Console.WriteLine("Playing both tones, then demonstrating mute/unmute");

    using var playback = new AudioPlayback();
    using var manager = new StreamManager(playback);
    using var source1 = new FileAudioSource(file1) { Loop = true };
    using var source2 = new FileAudioSource(file2) { Loop = true };

    manager.AddSource("tone100", source1, isPrimary: true);
    manager.AddSource("tone200", source2);

    manager.Play("tone100");
    manager.Play("tone200");

    Console.WriteLine("Both tones playing...");
    Thread.Sleep(2000);

    Console.WriteLine("\nMuting 100Hz tone...");
    manager.Mute("tone100");
    Console.WriteLine($"100Hz muted: {manager.IsMuted("tone100")}");
    Console.WriteLine($"100Hz volume: {manager.GetVolume("tone100"):F2}");
    Thread.Sleep(2000);

    Console.WriteLine("\nUnmuting 100Hz tone...");
    manager.Unmute("tone100");
    Console.WriteLine($"100Hz muted: {manager.IsMuted("tone100")}");
    Console.WriteLine($"100Hz volume: {manager.GetVolume("tone100"):F2}");
    Thread.Sleep(2000);

    manager.Stop("tone100");
    manager.Stop("tone200");
  }

  static void Demo3_FadeTransitions(string file1, string file2)
  {
    Console.WriteLine("\n--- Demo 3: Fade-In/Fade-Out Transitions ---");
    Console.WriteLine("Demonstrating smooth fade transitions");

    using var playback = new AudioPlayback();
    using var manager = new StreamManager(playback);
    using var source1 = new FileAudioSource(file1) { Loop = true };

    manager.AddSource("tone100", source1, isPrimary: true);

    Console.WriteLine("\nFading in 100Hz tone over 2 seconds...");
    manager.Play("tone100", fadeIn: true);
    Thread.Sleep(2500);

    Console.WriteLine("\nFading out 100Hz tone over 2 seconds...");
    manager.Stop("tone100", fadeOut: true);
    Thread.Sleep(2500);
  }

  static void Demo4_DynamicStreamManagement(string file1, string file2)
  {
    Console.WriteLine("\n--- Demo 4: Dynamic Stream Management ---");
    Console.WriteLine("Adding/removing streams dynamically");

    using var playback = new AudioPlayback();
    using var manager = new StreamManager(playback);

    Console.WriteLine($"\nInitial stream count: {manager.StreamCount}");

    using var source1 = new FileAudioSource(file1) { Loop = true };
    Console.WriteLine("\nAdding 100Hz tone...");
    manager.AddSource("tone100", source1, isPrimary: true);
    manager.Play("tone100");
    Console.WriteLine($"Stream count: {manager.StreamCount}");
    Thread.Sleep(2000);

    using var source2 = new FileAudioSource(file2) { Loop = true };
    Console.WriteLine("\nAdding 200Hz tone...");
    manager.AddSource("tone200", source2);
    manager.Play("tone200");
    Console.WriteLine($"Stream count: {manager.StreamCount}");
    Thread.Sleep(2000);

    Console.WriteLine("\nRemoving 100Hz tone with fade-out...");
    manager.RemoveSource("tone100", fadeOut: true);
    Thread.Sleep(1500);
    Console.WriteLine($"Stream count: {manager.StreamCount}");
    Thread.Sleep(1500);

    Console.WriteLine("\nRemoving 200Hz tone...");
    manager.RemoveSource("tone200", fadeOut: false);
    Console.WriteLine($"Stream count: {manager.StreamCount}");
  }
}
