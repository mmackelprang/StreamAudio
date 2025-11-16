using StreamAudio.Core.Sources;
using StreamAudio.Core.Mixing;
using StreamAudio.Core.Outputs;

Console.WriteLine("StreamAudio Demo - Audio Pipeline Test");
Console.WriteLine("========================================");
Console.WriteLine();

// Check if test files exist
string testDataPath = Path.Combine("..", "..", "..", "..", "..", "testdata");
string tone100Hz = Path.Combine(testDataPath, "100hz.wav");
string tone200Hz = Path.Combine(testDataPath, "200hz.wav");

if (!File.Exists(tone100Hz) || !File.Exists(tone200Hz))
{
  Console.WriteLine("ERROR: Test files not found in testdata/ directory.");
  Console.WriteLine("Please run the ToneGenerator tool first to create test files:");
  Console.WriteLine("  dotnet run --project tools/ToneGenerator/ToneGenerator.csproj -- 100 1 WAV testdata/100hz.wav");
  Console.WriteLine("  dotnet run --project tools/ToneGenerator/ToneGenerator.csproj -- 200 1 WAV testdata/200hz.wav");
  return 1;
}

Console.WriteLine("Demo 1: Playing a single tone (100 Hz)");
Console.WriteLine("---------------------------------------");
try
{
  using var source = new FileAudioSource(tone100Hz);
  using var output = new WaveOutAudioOutput(source.SampleRate, source.Channels);
  
  output.Initialize(source);
  output.Play();
  
  Console.WriteLine($"Playing {source.Name} at {source.SampleRate} Hz, {source.Channels} channel(s)");
  Console.WriteLine("Press any key to stop playback...");
  Console.ReadKey(true);
  
  output.Stop();
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
  using var source1 = new FileAudioSource(tone100Hz) { Repeat = true };
  using var source2 = new FileAudioSource(tone200Hz) { Repeat = true };
  using var mixer = new BasicMixer(44100, 1);
  
  // Add both sources to mixer - equal volume
  mixer.AddSource(source1);
  mixer.AddSource(source2);
  
  // Set equal volumes for both
  mixer.SetVolume(source1, 0.5f);
  mixer.SetVolume(source2, 0.5f);
  
  using var output = new WaveOutAudioOutput(mixer.SampleRate, mixer.Channels);
  output.Initialize(mixer);
  output.Play();
  
  Console.WriteLine($"Playing mixed audio: 100 Hz + 200 Hz");
  Console.WriteLine("Both tones at 50% volume, repeating");
  Console.WriteLine("Press any key to stop playback...");
  Console.ReadKey(true);
  
  output.Stop();
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
  using var primarySource = new FileAudioSource(tone100Hz) { Repeat = true };
  using var backgroundSource = new FileAudioSource(tone200Hz) { Repeat = true };
  using var mixer = new BasicMixer(44100, 1)
  {
    PrimaryVolume = 1.0f,
    BackgroundVolume = 0.2f
  };
  
  // Add sources - 100Hz as primary, 200Hz as background
  mixer.AddSource(primarySource, isPrimary: true);
  mixer.AddSource(backgroundSource, isPrimary: false);
  
  using var output = new WaveOutAudioOutput(mixer.SampleRate, mixer.Channels);
  output.Initialize(mixer);
  output.Play();
  
  Console.WriteLine($"Playing mixed audio with priority:");
  Console.WriteLine($"  Primary (100 Hz): {mixer.GetVolume(primarySource) * 100}%");
  Console.WriteLine($"  Background (200 Hz): {mixer.GetVolume(backgroundSource) * 100}%");
  Console.WriteLine("You should hear 100 Hz prominently with 200 Hz quietly in the background.");
  Console.WriteLine("Press any key to stop playback...");
  Console.ReadKey(true);
  
  output.Stop();
  Console.WriteLine("Playback stopped.");
}
catch (Exception ex)
{
  Console.WriteLine($"ERROR during playback: {ex.Message}");
  Console.WriteLine("Note: Audio playback may not work in headless environments.");
}

Console.WriteLine();
Console.WriteLine("Demo complete!");
return 0;
