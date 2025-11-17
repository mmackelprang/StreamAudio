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

### Supported Engines
- **eSpeak** - Local, lightweight TTS engine (fully implemented)
- **Google Cloud TTS** - Cloud-based neural TTS (fully implemented)
- **Azure Speech** - Microsoft's cloud TTS with natural voices (fully implemented)
- **Piper** - Local neural TTS using ONNX models (fully implemented)

### Features
- Multiple TTS engine support with easy configuration
- Configurable rate, pitch, and volume
- Defaults to **Auto** source type (short-lived announcements)
- High-quality neural voices with cloud engines
- Local processing with eSpeak and Piper for privacy/offline use

### Installation Requirements

#### eSpeak
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

#### Google Cloud TTS
1. Create a Google Cloud project at https://console.cloud.google.com/
2. Enable the Text-to-Speech API
3. Create a service account and download the JSON key file
4. Set the GOOGLE_APPLICATION_CREDENTIALS environment variable or provide the path in config

**Linux/macOS:**
```bash
export GOOGLE_APPLICATION_CREDENTIALS="/path/to/your-service-account-key.json"
```

**Windows:**
```powershell
$env:GOOGLE_APPLICATION_CREDENTIALS="C:\path\to\your-service-account-key.json"
```

#### Azure Speech
1. Create an Azure account at https://portal.azure.com/
2. Create a Speech Services resource
3. Get your subscription key and region from the resource

#### Piper TTS
1. Download Piper from https://github.com/rhasspy/piper/releases
2. Download a voice model (.onnx file) from https://github.com/rhasspy/piper/releases
3. Extract and note the paths to the piper executable and model file

**Linux (Debian/Ubuntu/Raspberry Pi):**
```bash
# Download Piper (example for ARM64)
wget https://github.com/rhasspy/piper/releases/download/v1.2.0/piper_arm64.tar.gz
tar xzf piper_arm64.tar.gz

# Download a voice model
wget https://github.com/rhasspy/piper/releases/download/v1.2.0/en_US-lessac-medium.onnx
```

### Usage

#### eSpeak (Local, No Credentials Required)
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

#### Google Cloud TTS
```csharp
var config = new TtsConfiguration
{
  Engine = "google",
  GoogleApiKey = "/path/to/service-account-key.json",  // Or set GOOGLE_APPLICATION_CREDENTIALS
  Voice = "en-US-Neural2-A",  // Optional: specific voice
  Rate = 1.0,
  Pitch = 0.0,
  Volume = 1.0
};

var tts = new TtsAudioSource("Hello with Google's natural voice", config: config);
tts.Play();
```

#### Azure Speech
```csharp
var config = new TtsConfiguration
{
  Engine = "azure",
  AzureSpeechKey = "your-subscription-key",
  AzureSpeechRegion = "westus",  // Your region
  Voice = "en-US-JennyNeural",  // Optional: specific voice
  Rate = 1.0,
  Pitch = 0.0,
  Volume = 1.0
};

var tts = new TtsAudioSource("Hello with Azure's natural voice", config: config);
tts.Play();
```

#### Piper TTS (Local Neural TTS)
```csharp
var config = new TtsConfiguration
{
  Engine = "piper",
  PiperExecutablePath = "/path/to/piper",  // Or just "piper" if in PATH
  PiperModelPath = "/path/to/en_US-lessac-medium.onnx",
  Rate = 1.0,  // Speed control via length_scale
  Volume = 1.0
};

var tts = new TtsAudioSource("Hello with Piper's neural voice", config: config);
tts.Play();
```

### Configuration Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| Engine | string | "espeak" | TTS engine: "espeak", "google", "azure", or "piper" |
| Voice | string? | null | Voice name (engine-specific) |
| Rate | double | 1.0 | Speaking rate (0.5-2.0) |
| Pitch | double | 0.0 | Pitch adjustment (-1.0 to 1.0) |
| Volume | double | 1.0 | Volume level (0.0-1.0) |
| GoogleApiKey | string? | null | Google Cloud API key path |
| AzureSpeechKey | string? | null | Azure Speech subscription key |
| AzureSpeechRegion | string? | null | Azure Speech region |
| PiperModelPath | string? | null | Path to Piper .onnx model file |
| PiperExecutablePath | string | "piper" | Path to Piper executable |
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
- **Advanced Features:**
  - Track search across Spotify library
  - Playlist management (view and play user playlists)
  - Personalized recommendations based on listening history
  - Favorites/saved tracks management
  - Real-time playback control

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

### Advanced Features Usage

#### Search for Tracks
```csharp
var spotify = new SpotifyAudioSource(config);
await spotify.InitializeAsync();

// Search for tracks
var results = await spotify.SearchTracksAsync("imagine dragons", limit: 20);
foreach (var track in results)
{
  Console.WriteLine($"{track.Artist} - {track.Name} ({track.Album})");
  Console.WriteLine($"  URI: {track.Uri}");
}

// Play a search result
if (results.Any())
{
  await spotify.PlayTrackAsync(results[0].Uri);
}
```

#### Manage Playlists
```csharp
// Get user's playlists
var playlists = await spotify.GetUserPlaylistsAsync(limit: 50);
foreach (var playlist in playlists)
{
  Console.WriteLine($"{playlist.Name} ({playlist.TrackCount} tracks)");
  Console.WriteLine($"  URI: {playlist.Uri}");
}

// Play a playlist
if (playlists.Any())
{
  await spotify.PlayPlaylistAsync(playlists[0].Uri);
}
```

#### Get Recommendations
```csharp
// Get recommendations based on seed tracks
var seedTracks = new List<string> 
{ 
  "4cOdK2wGLETKBW3PvgPWqT",  // Track ID
  "0c6xIDDpzE81m2q797ordA"   // Another track ID
};

var recommendations = await spotify.GetRecommendationsAsync(seedTracks, limit: 20);
foreach (var track in recommendations)
{
  Console.WriteLine($"Recommended: {track.Artist} - {track.Name}");
}
```

#### Manage Favorites
```csharp
// Get saved/favorite tracks
var favorites = await spotify.GetSavedTracksAsync(limit: 50);
foreach (var track in favorites)
{
  Console.WriteLine($"♥ {track.Artist} - {track.Name}");
}

// Save a track to favorites
await spotify.SaveTrackAsync("4cOdK2wGLETKBW3PvgPWqT");

// Remove a track from favorites
await spotify.RemoveTrackAsync("4cOdK2wGLETKBW3PvgPWqT");
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

## Testing & Quality Assurance

### Test Coverage
Phase 8+ includes comprehensive testing:
- **202 total tests** (all passing)
- **100 unit tests** covering all audio source implementations
- **11 integration tests** for complex multi-source scenarios
- **2 TTS engine tests** for Google and Azure implementations

### Test Categories
1. **TtsAudioSource Tests** (27 tests)
   - Configuration validation
   - Engine selection (eSpeak, Google, Azure, Piper)
   - Error handling for missing credentials
   - Rate, pitch, and volume controls

2. **SpotifyAudioSource Tests** (32 tests)
   - Authentication and initialization
   - Playback control (play, pause, stop)
   - Search functionality
   - Playlist management
   - Recommendations API
   - Favorites management
   - Simulation mode testing

3. **UsbAudioSource Tests** (24 tests)
   - Device configuration
   - Sample rate and channel validation
   - Buffer configuration
   - State management

4. **Enhanced FileAudioSource Tests** (17 tests)
   - Single file mode (Auto type)
   - Multiple files mode (Manual type)
   - Directory mode
   - Metadata extraction
   - Repeat count handling

5. **Multi-Source Integration Tests** (11 tests)
   - TTS + background music mixing
   - Multiple file sources with priority management
   - Dynamic source addition/removal
   - Mute/unmute in multi-source scenarios
   - Fade transitions
   - Auto vs Manual source lifecycle

### Running Tests
```bash
# Run all tests
dotnet test

# Run specific test category
dotnet test --filter "FullyQualifiedName~SpotifyAudioSourceTests"

# Run with detailed output
dotnet test --verbosity normal
```

## Phase 8+ Completion

### Completed Features ✅
- [x] Comprehensive unit tests for all audio sources (100 tests)
- [x] Integration tests for multi-source scenarios (11 tests)
- [x] Full Google Cloud TTS implementation
- [x] Full Azure Speech implementation
- [x] Full Piper TTS implementation
- [x] Advanced Spotify features:
  - [x] Track search
  - [x] Playlist management (view and play)
  - [x] Personalized recommendations
  - [x] Favorites/saved tracks management
- [x] Complete documentation for all TTS engines
- [x] Complete documentation for Spotify advanced features

### Notes
- **Audiobooks**: Not implemented as it requires additional Spotify Premium features and separate authentication flows
- **Performance**: All tests pass in CI/CD environments (headless mode supported)
- **Cross-platform**: Tested on Linux (primary target for Raspberry Pi deployment)

## References

- [eSpeak Documentation](http://espeak.sourceforge.net/)
- [Google Cloud Text-to-Speech API](https://cloud.google.com/text-to-speech/docs)
- [Azure Speech Service Documentation](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/)
- [Piper TTS](https://github.com/rhasspy/piper)
- [Spotify Web API](https://developer.spotify.com/documentation/web-api/)
- [NAudio Documentation](https://github.com/naudio/NAudio)
- [TagLibSharp Documentation](https://github.com/mono/taglib-sharp)
- [SoundFlow Audio Engine](https://github.com/jmbeach/SoundFlow)
