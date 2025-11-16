# StreamAudio Usage Guide

Cross-platform audio streaming and mixing library built with SoundFlow for .NET 8+.

## Platform Support

StreamAudio works on Windows, Linux, macOS, and Raspberry Pi. See [PLATFORM.md](PLATFORM.md) for detailed platform-specific information.

## Quick Start

### Playing a Single Audio File

```csharp
using StreamAudio.Core;
using StreamAudio.Core.Sources;
using StreamAudio.Core.Playback;

// Create audio source from file
using var source = new FileAudioSource("music.wav") { Loop = false };

// Create playback device
using var playback = new AudioPlayback();

// Add source to mixer and play
playback.AddPlayer(source.Player);
source.Play();

// Wait for playback
Console.WriteLine("Press any key to stop...");
Console.ReadKey();

// Cleanup
AudioEngineManager.Dispose();
```

### Mixing Multiple Audio Sources

```csharp
using StreamAudio.Core;
using StreamAudio.Core.Sources;
using StreamAudio.Core.Playback;

// Create sources
using var primary = new FileAudioSource("speech.wav");
using var background = new FileAudioSource("music.wav") { Loop = true };

// Create playback device
using var playback = new AudioPlayback();

// Add sources to mixer
playback.AddPlayer(primary.Player);
playback.AddPlayer(background.Player);

// Set volumes: primary at 100%, background at 20%
playback.SetVolume(primary.Player, 1.0f);   // Primary audio
playback.SetVolume(background.Player, 0.2f); // Background music

// Play both sources
primary.Play();
background.Play();

Console.WriteLine("Playing mixed audio...");
Console.ReadKey();

// Cleanup
AudioEngineManager.Dispose();
```

### Looping Audio

```csharp
using var source = new FileAudioSource("ambient.wav")
{
    Loop = true  // Automatically restart when finished
};

using var playback = new AudioPlayback();
playback.AddPlayer(source.Player);
source.Play();

// Audio will loop indefinitely until stopped
Console.ReadKey();
source.Stop();
```

### Platform-Optimized Configuration

```csharp
using StreamAudio.Core.Platform;
using StreamAudio.Core.Playback;

// Use platform-optimized settings
var config = AudioConfiguration.CreateDefault();
using var playback = new AudioPlayback(config);

// Or use Raspberry Pi-optimized settings
var piConfig = AudioConfiguration.CreateForRaspberryPi();
using var piPlayback = new AudioPlayback(piConfig);

// Or use low-latency settings
var lowLatencyConfig = AudioConfiguration.CreateLowLatency();
using var llPlayback = new AudioPlayback(lowLatencyConfig);
```

### Enumerating Audio Devices

```csharp
using StreamAudio.Core.Platform;

// List all playback devices
var playbackDevices = AudioDeviceEnumerator.GetPlaybackDevices();
foreach (var device in playbackDevices)
{
    Console.WriteLine($"Playback: {device.Name} ({device.DeviceType})");
    if (device.IsDefault)
        Console.WriteLine("  ^ Default device");
}

// Get default playback device
var defaultDevice = AudioDeviceEnumerator.GetDefaultPlaybackDevice();
if (defaultDevice != null)
{
    Console.WriteLine($"Default: {defaultDevice.Name}");
}
```

### Platform Detection

```csharp
using StreamAudio.Core.Platform;

// Detect current platform
Console.WriteLine($"Platform: {PlatformInfo.PlatformName}");
Console.WriteLine($"OS: {PlatformInfo.OSDescription}");
Console.WriteLine($"Architecture: {PlatformInfo.ProcessArchitecture}");

// Adjust behavior for specific platforms
if (PlatformInfo.IsRaspberryPi)
{
    Console.WriteLine("Running on Raspberry Pi - using optimized settings");
    var config = AudioConfiguration.CreateForRaspberryPi();
    // ... use config
}
```

## API Reference

### FileAudioSource

Represents an audio file that can be played.

**Constructor:**
```csharp
var source = new FileAudioSource(string filePath, AudioFormat? format = null);
```

**Properties:**
- `string Name` - File name
- `int SampleRate` - Sample rate in Hz
- `int Channels` - Number of channels
- `bool Loop` - Enable/disable looping
- `AudioFormat Format` - Audio format information
- `PlaybackState State` - Current playback state

**Methods:**
- `void Play()` - Start playback
- `void Pause()` - Pause playback
- `void Stop()` - Stop playback

### AudioPlayback

Manages playback device and audio mixing.

**Constructors:**
```csharp
// Use default format (DVD HQ: 48kHz, 2ch, 32-bit float)
var playback = new AudioPlayback();

// Use custom format
var playback = new AudioPlayback(AudioFormat.DvdHq);

// Use platform-optimized configuration
var config = AudioConfiguration.CreateDefault();
var playback = new AudioPlayback(config);
```

**Properties:**
- `Mixer Mixer` - Access to SoundFlow's built-in mixer
- `AudioFormat Format` - Audio format being used

**Methods:**
- `void AddPlayer(SoundPlayer player)` - Add a player to the mixer
- `void RemovePlayer(SoundPlayer player)` - Remove a player from the mixer
- `void SetVolume(SoundPlayer player, float volume)` - Set volume (0.0 to 1.0)
- `float GetVolume(SoundPlayer player)` - Get current volume
- `void Stop()` - Stop playback device

### AudioEngineManager

Manages the global SoundFlow audio engine (singleton).

**Static Properties:**
- `AudioEngine Engine` - Gets the shared engine instance

**Static Methods:**
- `void Dispose()` - Dispose the engine (call on application shutdown)

### PlatformInfo

Provides platform detection capabilities.

**Static Properties:**
- `bool IsWindows` - True if running on Windows
- `bool IsLinux` - True if running on Linux
- `bool IsMacOS` - True if running on macOS
- `bool IsRaspberryPi` - True if running on Raspberry Pi (best-effort detection)
- `string PlatformName` - Friendly platform name
- `string OSDescription` - Operating system description
- `Architecture ProcessArchitecture` - Process architecture (X64, ARM, ARM64, etc.)
- `Architecture OSArchitecture` - OS architecture

### AudioConfiguration

Platform-optimized audio configuration settings.

**Static Factory Methods:**
- `AudioConfiguration CreateDefault()` - Platform-optimized defaults
- `AudioConfiguration CreateForRaspberryPi()` - Raspberry Pi optimized (2048 frames buffer)
- `AudioConfiguration CreateLowLatency()` - Low latency mode (256 frames buffer)

**Properties:**
- `AudioFormat Format` - Audio format to use
- `int BufferSizeInFrames` - Buffer size in frames
- `bool LowLatencyMode` - Low latency mode flag

**Methods:**
- `string GetDescription()` - Get formatted description of configuration

### AudioDeviceEnumerator

Cross-platform audio device enumeration.

**Static Methods:**
- `List<AudioDeviceInfo> GetPlaybackDevices()` - Get all playback devices
- `List<AudioDeviceInfo> GetCaptureDevices()` - Get all capture devices
- `List<AudioDeviceInfo> GetAllDevices()` - Get all devices (playback and capture)
- `AudioDeviceInfo? GetDefaultPlaybackDevice()` - Get default playback device
- `AudioDeviceInfo? GetDefaultCaptureDevice()` - Get default capture device

### AudioDeviceInfo

Information about an audio device.

**Properties:**
- `IntPtr Id` - Device ID
- `string Name` - Device name
- `bool IsPlayback` - True if playback device
- `bool IsCapture` - True if capture device
- `bool IsDefault` - True if default device
- `string DeviceType` - Device type (USB, HDMI, Bluetooth, etc.)

## Examples

See the project tools for complete working examples:

### AudioDemo
Demonstrates basic audio playback and mixing:
1. Single tone playback
2. Mixing two audio sources
3. Primary/background volume control

Run the demo:
```bash
dotnet run --project tools/AudioDemo/AudioDemo.csproj
```

### PlatformInfo
Displays platform detection and audio device information:
- Platform name, OS, architecture
- Recommended audio configurations
- Available playback and capture devices

Run the tool:
```bash
dotnet run --project tools/PlatformInfo/PlatformInfo.csproj
```

### ToneGenerator
Generates test audio files (sine waves):

```bash
# Generate a 100 Hz tone for 1 second as WAV
dotnet run --project tools/ToneGenerator/ToneGenerator.csproj -- 100 1 WAV output.wav

# Generate a 200 Hz tone for 2 seconds as WAV
dotnet run --project tools/ToneGenerator/ToneGenerator.csproj -- 200 2 WAV tone.wav
```

## Supported Audio Formats

SoundFlow supports various audio formats including:
- WAV (all common encodings)
- MP3
- FLAC
- And more...

The exact format support depends on the platform and SoundFlow version.
