using StreamAudio.Core;
using StreamAudio.Core.Playback;
using StreamAudio.Core.Sources;
using StreamAudio.Core.Monitoring;

namespace PerformanceDemo;

class Program
{
  static async Task Main(string[] args)
  {
    Console.WriteLine("=== StreamAudio Performance Monitoring Demo ===");
    Console.WriteLine();

    // Check for test data
    var testDataPath = Path.Combine(".", "testdata");
    var file100Hz = Path.Combine(testDataPath, "100hz.wav");
    var file200Hz = Path.Combine(testDataPath, "200hz.wav");

    if (!File.Exists(file100Hz) || !File.Exists(file200Hz))
    {
      Console.WriteLine("Error: Test audio files not found.");
      Console.WriteLine("Please ensure 100hz.wav and 200hz.wav exist in the testdata directory.");
      return;
    }

    Console.WriteLine("This demo monitors CPU and memory usage during audio playback.");
    Console.WriteLine("Press 'q' to quit at any time.\n");

    using var perfMonitor = new PerformanceMonitor();
    using var cts = new CancellationTokenSource();

    // Start monitoring in background
    var monitorTask = Task.Run(async () =>
    {
      await foreach (var snapshot in perfMonitor.MonitorAsync(1000, cts.Token))
      {
        Console.WriteLine($"Performance: {snapshot}");
      }
    });

    // Wait for initial baseline
    await Task.Delay(2000);

    Console.WriteLine("\nStarting audio playback scenario...");
    Console.WriteLine("Scenario: Playing and mixing multiple audio streams\n");

    try
    {
      // Initialize audio engine
      var engine = AudioEngineManager.Engine;
      
      using var playback = new AudioPlayback();
      using var manager = new StreamManager(playback);

      // Scenario 1: Single stream
      Console.WriteLine("[Scenario 1] Playing single 100 Hz tone...");
      var source1 = new FileAudioSource(file100Hz);
      source1.Loop = true;
      manager.AddSource("tone100", source1, isPrimary: true);
      manager.Play("tone100", fadeIn: true);
      
      await Task.Delay(5000);

      // Scenario 2: Add second stream
      Console.WriteLine("[Scenario 2] Adding 200 Hz tone as background...");
      var source2 = new FileAudioSource(file200Hz);
      source2.Loop = true;
      manager.AddSource("tone200", source2);
      manager.Play("tone200", fadeIn: true);

      await Task.Delay(5000);

      // Scenario 3: Switch primary
      Console.WriteLine("[Scenario 3] Switching primary stream...");
      manager.SetPrimaryStream("tone200");

      await Task.Delay(5000);

      // Scenario 4: Fade out and clean up
      Console.WriteLine("[Scenario 4] Fading out all streams...");
      manager.Stop("tone100", fadeOut: true);
      manager.Stop("tone200", fadeOut: true);

      await Task.Delay(2000);

      Console.WriteLine("\nDemo completed successfully!");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"\nError during demo: {ex.Message}");
    }

    Console.WriteLine("\nPress any key to exit...");
    Console.ReadKey();
    
    cts.Cancel();
    await monitorTask;
  }
}
