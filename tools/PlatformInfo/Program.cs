using StreamAudio.Core.Platform;
using StreamAudio.Core;

Console.WriteLine("StreamAudio Platform Information");
Console.WriteLine("=================================");
Console.WriteLine();

// Display platform information
Console.WriteLine("Platform Detection:");
Console.WriteLine($"  Platform Name: {PlatformInfo.PlatformName}");
Console.WriteLine($"  OS Description: {PlatformInfo.OSDescription}");
Console.WriteLine($"  Process Architecture: {PlatformInfo.ProcessArchitecture}");
Console.WriteLine($"  OS Architecture: {PlatformInfo.OSArchitecture}");
Console.WriteLine($"  Is Windows: {PlatformInfo.IsWindows}");
Console.WriteLine($"  Is Linux: {PlatformInfo.IsLinux}");
Console.WriteLine($"  Is macOS: {PlatformInfo.IsMacOS}");
Console.WriteLine($"  Is Raspberry Pi: {PlatformInfo.IsRaspberryPi}");
Console.WriteLine();

// Display audio configuration recommendations
Console.WriteLine("Recommended Audio Configurations:");
Console.WriteLine();

var defaultConfig = AudioConfiguration.CreateDefault();
Console.WriteLine("Default Configuration:");
Console.WriteLine($"  {defaultConfig.GetDescription()}");
Console.WriteLine();

var raspberryPiConfig = AudioConfiguration.CreateForRaspberryPi();
Console.WriteLine("Raspberry Pi Optimized Configuration:");
Console.WriteLine($"  {raspberryPiConfig.GetDescription()}");
Console.WriteLine();

var lowLatencyConfig = AudioConfiguration.CreateLowLatency();
Console.WriteLine("Low Latency Configuration:");
Console.WriteLine($"  {lowLatencyConfig.GetDescription()}");
Console.WriteLine();

// Enumerate audio devices
Console.WriteLine("Audio Devices:");
Console.WriteLine("==============");
Console.WriteLine();

try
{
  Console.WriteLine("Playback Devices:");
  var playbackDevices = AudioDeviceEnumerator.GetPlaybackDevices();
  if (playbackDevices.Count > 0)
  {
    foreach (var device in playbackDevices)
    {
      Console.WriteLine($"  - {device}");
    }
  }
  else
  {
    Console.WriteLine("  (No playback devices found)");
  }
  Console.WriteLine();

  Console.WriteLine("Capture Devices:");
  var captureDevices = AudioDeviceEnumerator.GetCaptureDevices();
  if (captureDevices.Count > 0)
  {
    foreach (var device in captureDevices)
    {
      Console.WriteLine($"  - {device}");
    }
  }
  else
  {
    Console.WriteLine("  (No capture devices found)");
  }
  Console.WriteLine();

  var defaultPlayback = AudioDeviceEnumerator.GetDefaultPlaybackDevice();
  if (defaultPlayback != null)
  {
    Console.WriteLine($"Default Playback Device: {defaultPlayback.Name}");
  }

  var defaultCapture = AudioDeviceEnumerator.GetDefaultCaptureDevice();
  if (defaultCapture != null)
  {
    Console.WriteLine($"Default Capture Device: {defaultCapture.Name}");
  }
}
catch (Exception ex)
{
  Console.WriteLine($"Error enumerating devices: {ex.Message}");
  Console.WriteLine("This may be expected in headless/containerized environments.");
}
Console.WriteLine();

// Cleanup
AudioEngineManager.Dispose();

Console.WriteLine("Platform information display complete!");
return 0;
