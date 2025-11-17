# Phase 8 Audio Sources

This document describes the new audio sources added in Phase 8 of the StreamAudio project.

## Overview

Phase 8 introduces four major audio source types to support a complete audio management system:

1. **TtsAudioSource** - Text-to-Speech announcements
2. **SpotifyAudioSource** - Spotify music streaming integration
3. **UsbAudioSource** - USB audio device capture (radio, turntables, etc.)
4. **Enhanced FileAudioSource** - Single files, playlists, and directories

## SongMetadata

All audio sources now support a `CurrentlyPlaying` property that returns `SongMetadata` when available:

```csharp
public class SongMetadata
{
  public string? Title { get; set; }
  public string? Artist { get; set; }
  public string? Album { get; set; }
  public string? Station { get; set; }
  public string? Genre { get; set; }
  public TimeSpan? Duration { get; set; }
  public TimeSpan? Position { get; set; }
  public string? AlbumArtUrl { get; set; }
  public Dictionary<string, string> AdditionalInfo { get; set; }
}
```

## TtsAudioSource

### Overview
Text-to-Speech audio source for voice announcements and notifications.

### Features
- eSpeak engine (fully implemented, requires installation)
- Google Cloud TTS (stub, ready for implementation)
- Azure Speech (stub, ready for implementation)
- Configurable rate, pitch, and volume
- Defaults to **Auto** source type (short-lived announcements)

### Installation Requirements

**Linux (Debian/Ubuntu/Raspberry Pi):**
```bash
sudo apt-get update
sudo apt-get install espeak
```

**macOS:**
```bash
brew install espeak
```

**Windows:**
Download and install from http://espeak.sourceforge.net/

### Usage

```csharp
// Simple usage with defaults (eSpeak)
var tts = new TtsAudioSource("Hello from StreamAudio");

// Advanced configuration
var config = new TtsConfiguration
{
  Engine = "espeak",
  Voice = "en-us",
  Rate = 1.2,    // 20% faster
  Pitch = 0.1,   // Slightly higher pitch
  Volume = 1.0
};

var tts = new TtsAudioSource("Custom voice announcement", config: config);

// Use with StreamManager
using var playback = new AudioPlayback();
using var manager = new StreamManager(playback);
manager.AddSource("announcement", tts, isPrimary: true);
tts.Play();
```

### Configuration Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| Engine | string | "espeak" | TTS engine: "espeak", "google", or "azure" |
| Voice | string? | null | Voice name (engine-specific) |
| Rate | double | 1.0 | Speaking rate (0.5-2.0) |
| Pitch | double | 0.0 | Pitch adjustment (-1.0 to 1.0) |
| Volume | double | 1.0 | Volume level (0.0-1.0) |
| GoogleApiKey | string? | null | Google Cloud API key |
| AzureSpeechKey | string? | null | Azure Speech key |
| AzureSpeechRegion | string? | null | Azure Speech region |

## SpotifyAudioSource

### Overview
Spotify streaming integration using Spotify Web API and Connect API.

### Features
- Full Spotify Web API integration
- User authentication via PKCE flow
- Real-time metadata updates
- Simulation mode for testing
- Defaults to **Manual** source type (long-running playback)

### Important Notes
- **Does NOT provide direct audio streaming** - uses Spotify Connect API
- Requires active Spotify Premium account
- Audio playback happens through Spotify app/device
- This source provides playback control and metadata only

### Setup

1. **Create Spotify App:**
   - Go to https://developer.spotify.com/dashboard
   - Create a new app
   - Note your Client ID and Client Secret
   - Add redirect URI: `http://localhost:5000/callback`

2. **Get Refresh Token:**
   Follow Spotify's PKCE authorization flow to obtain a refresh token.

3. **Set Environment Variables:**
   ```bash
   export SPOTIFY_CLIENT_ID="your_client_id"
   export SPOTIFY_CLIENT_SECRET="your_client_secret"  # Optional for client credentials
   export SPOTIFY_REFRESH_TOKEN="your_refresh_token"  # For user authentication
   ```

### Usage

```csharp
// Configuration from environment variables
var config = new SpotifyConfiguration
{
  ClientId = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID"),
  ClientSecret = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_SECRET"),
  RefreshToken = Environment.GetEnvironmentVariable("SPOTIFY_REFRESH_TOKEN")
};

// Or simulation mode for testing
var config = new SpotifyConfiguration { UseSimulation = true };

// Create and initialize source
var spotify = new SpotifyAudioSource(config);
await spotify.InitializeAsync();

// Check currently playing
if (spotify.CurrentlyPlaying != null)
{
  Console.WriteLine($"Now playing: {spotify.CurrentlyPlaying.Title}");
  Console.WriteLine($"Artist: {spotify.CurrentlyPlaying.Artist}");
  Console.WriteLine($"Album: {spotify.CurrentlyPlaying.Album}");
}

// Control playback
spotify.Play();      // Resume
spotify.Pause();     // Pause
await spotify.PlayTrackAsync("spotify:track:xxx");  // Play specific track
```

### Configuration Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| ClientId | string? | null | Spotify Client ID |
| ClientSecret | string? | null | Spotify Client Secret |
| RefreshToken | string? | null | User refresh token |
| RedirectUri | string | "http://localhost:5000/callback" | OAuth redirect URI |
| Market | string | "US" | Market/country code |
| MaxItems | int | 50 | Max items in API responses |
| UseSimulation | bool | false | Enable simulation mode |

## UsbAudioSource

### Overview
Captures audio from USB audio devices (radios, turntables, microphones, etc.).

### Features
- NAudio-based real-time capture
- Configurable device selection
- Adjustable sample rate and channels
- Circular buffer for continuous streaming
- Defaults to **Manual** source type (continuous capture)

### Usage

```csharp
// Configuration
var config = new UsbAudioConfiguration
{
  DeviceNumber = -1,     // -1 for default device
  DeviceName = "USB Radio",
  SampleRate = 44100,
  Channels = 2,
  BitsPerSample = 16,
  BufferMilliseconds = 100
};

// Create source
var usb = new UsbAudioSource(config);

// Start capture
usb.Play();  // Begins capturing from device

// Use with StreamManager
using var playback = new AudioPlayback();
using var manager = new StreamManager(playback);
manager.AddSource("radio", usb, isPrimary: true);

// Stop capture
usb.Stop();
```

### Configuration Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| DeviceNumber | int | -1 | Device index (-1 for default) |
| DeviceName | string | "USB Audio Device" | Display name |
| SampleRate | int | 44100 | Sample rate in Hz |
| Channels | int | 2 | Number of channels |
| BitsPerSample | int | 16 | Bits per sample |
| BufferMilliseconds | int | 100 | Buffer size in ms |

### Device Detection

To list available devices on your system:

```bash
# Linux
arecord -l

# Windows
# Use Windows Sound settings or NAudio enumeration

# macOS
system_profiler SPAudioDataType
```

## Enhanced FileAudioSource

### Overview
Enhanced to support single files, playlists, and directories with metadata extraction.

### Features
- Single file mode (Auto source type)
- Multiple files mode (Manual source type)
- Directory mode (Manual source type)
- TagLib-based metadata extraction
- Automatic file progression
- Supports: MP3, WAV, FLAC, OGG, M4A, WMA, AAC

### Usage

#### Single File (Auto)
```csharp
var source = new FileAudioSource("song.mp3");
// SourceType = Auto (defaults)
// RepeatCount = 1
```

#### Multiple Files (Manual)
```csharp
var files = new List<string>
{
  "song1.mp3",
  "song2.mp3",
  "song3.mp3"
};
var source = new FileAudioSource(files);
// SourceType = Manual (defaults)
// Files play in sequence
```

#### Directory (Manual)
```csharp
var source = FileAudioSource.FromDirectory("/path/to/music");
// SourceType = Manual (defaults)
// All audio files in directory
// Sorted alphabetically
```

#### Metadata Access
```csharp
var source = new FileAudioSource("song.mp3");

if (source.CurrentlyPlaying != null)
{
  var meta = source.CurrentlyPlaying;
  Console.WriteLine($"Title: {meta.Title}");
  Console.WriteLine($"Artist: {meta.Artist}");
  Console.WriteLine($"Album: {meta.Album}");
  Console.WriteLine($"Genre: {meta.Genre}");
  Console.WriteLine($"Duration: {meta.Duration}");
  
  // Additional file info
  Console.WriteLine($"File: {meta.AdditionalInfo["FileName"]}");
  Console.WriteLine($"BitRate: {meta.AdditionalInfo["BitRate"]}");
}
```

## Demo Application

Run the NewSourceDemo application to try all audio sources:

```bash
cd tools/NewSourceDemo
dotnet run
```

The demo provides interactive menus for:
1. TTS Audio Source Demo
2. File Audio Source Demo (Single File)
3. File Audio Source Demo (Multiple Files)
4. File Audio Source Demo (Directory)
5. Spotify Audio Source Demo
6. USB Audio Source Demo

## Testing

### Manual Testing
Use NewSourceDemo for interactive testing of all sources.

### Headless Testing
The demo detects headless environments and simulates audio output:
```bash
# CI/CD-friendly testing
DISPLAY= dotnet run --project tools/NewSourceDemo
```

### Unit Tests
(To be implemented - see Phase 8 pending items)

## Troubleshooting

### TTS Issues
- **Error: "espeak not found"**
  - Install espeak: `sudo apt-get install espeak`
  
- **Poor quality output**
  - Adjust rate and pitch in configuration
  - Try different voices with `-v` parameter

### Spotify Issues
- **Authentication failed**
  - Verify Client ID and Secret
  - Ensure refresh token is valid
  - Check redirect URI matches app settings
  
- **No currently playing data**
  - Start playback in Spotify app
  - Ensure Spotify Connect device is active

### USB Audio Issues
- **Device not found**
  - List devices with `arecord -l` (Linux)
  - Check device number in configuration
  
- **Audio quality issues**
  - Adjust sample rate to match device
  - Increase buffer size for stability
  - Verify device supports requested format

## Raspberry Pi Deployment

### TTS Setup
```bash
sudo apt-get update
sudo apt-get install espeak espeak-data
echo "Testing TTS" | espeak
```

### Spotify Setup
1. Install dependencies:
   ```bash
   sudo apt-get install libsecret-1-0
   ```

2. Configure credentials:
   ```bash
   mkdir -p ~/.config/streamaudio
   cat > ~/.config/streamaudio/spotify.env << EOF
   SPOTIFY_CLIENT_ID=your_client_id
   SPOTIFY_REFRESH_TOKEN=your_refresh_token
   EOF
   ```

3. Load in application:
   ```bash
   source ~/.config/streamaudio/spotify.env
   dotnet run
   ```

### USB Audio Setup
1. List available devices:
   ```bash
   arecord -l
   ```

2. Test device:
   ```bash
   arecord -D hw:1,0 -f cd -d 5 test.wav
   aplay test.wav
   ```

3. Configure permissions:
   ```bash
   sudo usermod -a -G audio $USER
   ```

## Next Steps

Phase 8 is nearing completion. Remaining tasks:
- [ ] Comprehensive unit tests for new audio sources
- [ ] Integration tests for multi-source scenarios  
- [ ] Performance testing with multiple concurrent sources
- [ ] Full Google Cloud TTS implementation
- [ ] Full Azure Speech implementation
- [ ] Advanced Spotify features (search, playlists, recommendations)

## References

- [eSpeak Documentation](http://espeak.sourceforge.net/)
- [Spotify Web API](https://developer.spotify.com/documentation/web-api/)
- [NAudio Documentation](https://github.com/naudio/NAudio)
- [TagLibSharp Documentation](https://github.com/mono/taglib-sharp)
