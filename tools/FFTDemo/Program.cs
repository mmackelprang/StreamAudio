using System.Diagnostics.Contracts;
using StreamAudio.Core;
using StreamAudio.Core.Playback;
using StreamAudio.Core.Sources;

namespace FFTDemo;

/// <summary>
/// Demonstrates FFTAudioPlayback device for audio analysis and testing.
/// Shows how to use FFT analysis to verify audio mixing and frequency content.
/// </summary>
class Program
{
  private const string TestDataPath = "./testdata";

  static void Main(string[] args)
  {
  Console.WriteLine("=== StreamAudio - FFT Analysis Demo ===\n");
  Console.WriteLine("This demo showcases:");
  Console.WriteLine("- FFTAudioPlayback real-time audio capture");
  Console.WriteLine("- FFT frequency analysis");
  Console.WriteLine("- Duration and sample tracking");
  Console.WriteLine("- Audio mixing verification\n");    if(args.Length > 0 && args[0] == "1")
    {
      Console.WriteLine("Running Demo1_SingleToneAnalysis...");
      Demo1_SingleToneAnalysis(Path.Combine(TestDataPath, "100hz.wav"));
      return;
    }
    if(args.Length > 0 && args[0] == "2")
    {
      Console.WriteLine("Running Demo2_MixedTonesAnalysis...");
      Demo2_MixedTonesAnalysis(Path.Combine(TestDataPath, "100hz.wav"), Path.Combine(TestDataPath, "200hz.wav"));
      return;
    }
    if(args.Length > 0 && args[0] == "3")
    {
      Console.WriteLine("Running Demo3_DurationTracking...");
      Demo3_DurationTracking(Path.Combine(TestDataPath, "100hz.wav"));
      return;
    }

    try
    {
      RunDemos();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"\nError: {ex.Message}");
      Console.WriteLine("\nNote: Make sure test audio files exist in testdata/ directory.");
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
      Console.WriteLine("Please run ToneGenerator to create test audio files first:");
      Console.WriteLine("  dotnet run --project tools/ToneGenerator -- 100 1 WAV testdata/100hz.wav");
      Console.WriteLine("  dotnet run --project tools/ToneGenerator -- 200 1 WAV testdata/200hz.wav");
      Console.WriteLine("  dotnet run --project tools/ToneGenerator -- 440 1 WAV testdata/440hz.wav");
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

    Demo1_SingleToneAnalysis(file100Hz);
    Demo2_MixedTonesAnalysis(file100Hz, file200Hz);
    Demo3_DurationTracking(file100Hz);
  }

  static void Demo1_SingleToneAnalysis(string file100Hz)
  {
    Console.WriteLine("\n--- Demo 1: Single Tone FFT Analysis ---");
    Console.WriteLine("Loading 100 Hz tone file...\n");

    using var fftPlayback = new FFTAudioPlayback();
    using var source = new FileAudioSource(file100Hz);

    fftPlayback.AddPlayer(source.Player);
    source.Play();
    
    Console.WriteLine("Playing and capturing audio for 1 second...");
    Thread.Sleep(1000);
    
    fftPlayback.Stop();
    source.Stop();

  Console.WriteLine($"\n--- FFT Analysis Results ---");
    Console.WriteLine($"Samples captured: {fftPlayback.SampleCount:N0}");
    Console.WriteLine($"Audio duration: {fftPlayback.AudioDuration?.TotalSeconds:F2} seconds");
    
    if (fftPlayback.TopFrequencies != null && fftPlayback.TopFrequencies.Count > 0)
    {
      Console.WriteLine("\nTop 5 frequencies detected:");
      for (int i = 0; i < Math.Min(5, fftPlayback.TopFrequencies.Count); i++)
      {
        var freq = fftPlayback.TopFrequencies[i];
        Console.WriteLine($"  {i + 1}. {freq.Frequency:F1} Hz - Intensity: {freq.Intensity:F2}");
      }
      
      var dominantFreq = fftPlayback.TopFrequencies[0];
      Console.WriteLine($"\nDominant frequency: {dominantFreq.Frequency:F1} Hz");
    }
  }

  static void Demo2_MixedTonesAnalysis(string file100Hz, string file200Hz)
  {
    Console.WriteLine("\n\n--- Demo 2: Mixed Tones FFT Analysis ---");
    Console.WriteLine("Loading 100 Hz and 200 Hz files...\n");

    using var fftPlayback = new FFTAudioPlayback();
    using var source1 = new FileAudioSource(file100Hz);
    using var source2 = new FileAudioSource(file200Hz);

    fftPlayback.AddPlayer(source1.Player);
    fftPlayback.AddPlayer(source2.Player);
    fftPlayback.SetVolume(source1.Player, 0.5f);
    fftPlayback.SetVolume(source2.Player, 0.5f);
    
    source1.Play();
    source2.Play();
    
    Console.WriteLine("Playing and capturing mixed audio for 1 second...");
    Thread.Sleep(1000);
    
    fftPlayback.Stop();
    source1.Stop();
    source2.Stop();

    Console.WriteLine("\n--- FFT Analysis Results ---");
    Console.WriteLine($"Samples captured: {fftPlayback.SampleCount:N0}");
    Console.WriteLine($"Audio duration: {fftPlayback.AudioDuration?.TotalSeconds:F2} seconds");
    
    if (fftPlayback.TopFrequencies != null && fftPlayback.TopFrequencies.Count > 0)
    {
      Console.WriteLine("\nTop 5 frequencies detected:");
      for (int i = 0; i < Math.Min(5, fftPlayback.TopFrequencies.Count); i++)
      {
        var freq = fftPlayback.TopFrequencies[i];
        Console.WriteLine($"  {i + 1}. {freq.Frequency:F1} Hz - Intensity: {freq.Intensity:F2}");
      }
      
  Console.WriteLine("\nBoth frequencies successfully detected in mixed audio!");
    }
  }

  static void Demo3_DurationTracking(string file100Hz)
  {
    Console.WriteLine("\n\n--- Demo 3: Duration Tracking ---");
    Console.WriteLine("Testing audio duration measurement...\n");

    using var fftPlayback = new FFTAudioPlayback();
    using var source = new FileAudioSource(file100Hz);

    fftPlayback.AddPlayer(source.Player);
    source.Play();
    
    int captureTimeMs = 500;
    Console.WriteLine($"Capturing for {captureTimeMs}ms...");
    Thread.Sleep(captureTimeMs);
    
    fftPlayback.Stop();
    source.Stop();

    Console.WriteLine("\n--- Duration Results ---");
    Console.WriteLine($"Requested capture time: {captureTimeMs}ms");
    Console.WriteLine($"Measured duration: {fftPlayback.AudioDuration?.TotalMilliseconds:F0}ms");
    Console.WriteLine($"Samples captured: {fftPlayback.SampleCount:N0}");
    
    var format = fftPlayback.Format;
    Console.WriteLine($"\nAudio format: {format.SampleRate} Hz, {format.Channels} channel(s)");
    
    if (fftPlayback.AudioDuration.HasValue)
    {
      double expectedSamples = format.SampleRate * format.Channels * 
                               fftPlayback.AudioDuration.Value.TotalSeconds;
      Console.WriteLine($"Expected samples (approximate): {expectedSamples:N0}");
    }

    Console.WriteLine("\n--- Memory Usage Note ---");
    Console.WriteLine("FFTAudioPlayback stores all samples in memory.");
    Console.WriteLine("For this demo:");
    var memoryMB = (fftPlayback.SampleCount * sizeof(float)) / (1024.0 * 1024.0);
    Console.WriteLine($"  Memory used: ~{memoryMB:F2} MB");
    Console.WriteLine($"  For 1 minute @ {format.SampleRate} Hz: ~{(format.SampleRate * format.Channels * 60 * sizeof(float)) / (1024.0 * 1024.0):F1} MB");
    Console.WriteLine($"  For 10 minutes: ~{(format.SampleRate * format.Channels * 600 * sizeof(float)) / (1024.0 * 1024.0):F1} MB");
  }
}
