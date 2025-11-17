using StreamAudio.Core;
using StreamAudio.Core.Sources;
using StreamAudio.Core.Playback;

Console.WriteLine("StreamAudio Demo - Audio Pipeline Test");
Console.WriteLine("========================================");
Console.WriteLine();

// Check if test files exist
string testDataPath = Path.Combine(".", "testdata");
string tone100Hz = Path.Combine(testDataPath, "100hz.wav");
string tone200Hz = Path.Combine(testDataPath, "200hz.wav");

if (!File.Exists(tone100Hz) || !File.Exists(tone200Hz))
{
  Console.WriteLine("ERROR: Test files not found in testdata/ directory.");
  Console.WriteLine("Please ensure the following files exist:");
  Console.WriteLine($"  {tone100Hz}");
  Console.WriteLine($"  {tone200Hz}");
  Console.WriteLine("Please run the ToneGenerator tool first to create test files:");
  Console.WriteLine("  dotnet run --project tools/ToneGenerator/ToneGenerator.csproj -- 100 1 WAV testdata/100hz.wav");
  Console.WriteLine("  dotnet run --project tools/ToneGenerator/ToneGenerator.csproj -- 200 1 WAV testdata/200hz.wav");
  return 1;
}

Console.WriteLine("Demo 1: Playing a single tone (100 Hz)");
Console.WriteLine("---------------------------------------");
try
{
  using var source = new FileAudioSource(tone100Hz) { Loop = false };
  using var playback = new AudioPlayback();
  
  playback.AddPlayer(source.Player);
  source.Play();
  
  Console.WriteLine($"Playing {source.Name} at {source.SampleRate} Hz, {source.Channels} channel(s)");
  Console.WriteLine("Press any key to stop playback...");
  Console.ReadKey(true);
  
  source.Stop();
  Console.WriteLine("Playback stopped.");
}
catch (Exception ex)
{
  Console.WriteLine($"ERROR during playback: {ex.Message}");
  Console.WriteLine("Note: Audio playback may not work in headless environments.");
}

Console.WriteLine();
Console.WriteLine("Demo 2: Mixing two tones (100 Hz + 200 Hz)");
Console.WriteLine("-------------------------------------------");
try
{
  using var source1 = new FileAudioSource(tone100Hz) { Loop = true };
  using var source2 = new FileAudioSource(tone200Hz) { Loop = true };
  using var playback = new AudioPlayback();
  
  // Add both sources to the mixer with equal volume
  playback.AddPlayer(source1.Player);
  playback.AddPlayer(source2.Player);
  playback.SetVolume(source1.Player, 0.5f);
  playback.SetVolume(source2.Player, 0.5f);
  
  source1.Play();
  source2.Play();
  
  Console.WriteLine($"Playing mixed audio: 100 Hz + 200 Hz");
  Console.WriteLine("Both tones at 50% volume, repeating");
  Console.WriteLine("Press any key to stop playback...");
  Console.ReadKey(true);
  
  source1.Stop();
  source2.Stop();
  Console.WriteLine("Playback stopped.");
}
catch (Exception ex)
{
  Console.WriteLine($"ERROR during playback: {ex.Message}");
  Console.WriteLine("Note: Audio playback may not work in headless environments.");
}

Console.WriteLine();
Console.WriteLine("Demo 3: Primary/Background Volume Control");
Console.WriteLine("-----------------------------------------");
try
{
  using var primarySource = new FileAudioSource(tone100Hz) { Loop = true };
  using var backgroundSource = new FileAudioSource(tone200Hz) { Loop = true };
  using var playback = new AudioPlayback();
  
  // Add sources - 100Hz at full volume (primary), 200Hz at low volume (background)
  playback.AddPlayer(primarySource.Player);
  playback.AddPlayer(backgroundSource.Player);
  playback.SetVolume(primarySource.Player, 1.0f);   // Primary at 100%
  playback.SetVolume(backgroundSource.Player, 0.2f); // Background at 20%
  
  primarySource.Play();
  backgroundSource.Play();
  
  Console.WriteLine($"Playing mixed audio with priority:");
  Console.WriteLine($"  Primary (100 Hz): {playback.GetVolume(primarySource.Player) * 100}%");
  Console.WriteLine($"  Background (200 Hz): {playback.GetVolume(backgroundSource.Player) * 100}%");
  Console.WriteLine("You should hear 100 Hz prominently with 200 Hz quietly in the background.");
  Console.WriteLine("Press any key to stop playback...");
  Console.ReadKey(true);
  
  primarySource.Stop();
  backgroundSource.Stop();
  Console.WriteLine("Playback stopped.");
}
catch (Exception ex)
{
  Console.WriteLine($"ERROR during playback: {ex.Message}");
  Console.WriteLine("Note: Audio playback may not work in headless environments.");
}

Console.WriteLine();
Console.WriteLine("Demo complete!");

// Cleanup the audio engine
AudioEngineManager.Dispose();

return 0;
