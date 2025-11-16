# StreamAudio Usage Guide

Cross-platform audio streaming and mixing library built with SoundFlow for .NET 8+.

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

**Constructor:**
```csharp
var playback = new AudioPlayback(AudioFormat? format = null);
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

## Examples

See the `tools/AudioDemo` project for complete working examples demonstrating:
1. Single tone playback
2. Mixing two audio sources
3. Primary/background volume control

Run the demo:
```bash
dotnet run --project tools/AudioDemo/AudioDemo.csproj
```

## Supported Audio Formats

SoundFlow supports various audio formats including:
- WAV (all common encodings)
- MP3
- FLAC
- And more...

The exact format support depends on the platform and SoundFlow version.
