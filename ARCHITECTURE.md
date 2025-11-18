# StreamAudio Architecture

## Overview

StreamAudio is a cross-platform audio management system built on .NET 8+ that provides sophisticated audio mixing, streaming, and playback capabilities. The system is designed for embedded and IoT scenarios, particularly Raspberry Pi deployments, while maintaining full compatibility with Windows, Linux, and macOS development environments.

## Architectural Principles

### Core Principles
1. **Cross-Platform First**: All components are designed to work across Windows, Linux, macOS, and ARM platforms
2. **Separation of Concerns**: Clear boundaries between audio sources, processing, mixing, and output
3. **Dependency Injection**: Components are loosely coupled and testable
4. **Event-Driven**: Error handling and state changes use events for loose coupling
5. **Performance Conscious**: Optimized for real-time audio with minimal latency and memory allocations

### Design Patterns
- **Singleton**: `AudioEngineManager` provides centralized engine management
- **Facade**: `AudioPlayback` and `StreamManager` simplify complex SoundFlow APIs
- **Observer**: Event-based error handling and state notifications
- **Wrapper**: `FileAudioSource` wraps SoundFlow's SoundPlayer for easier use
- **Strategy**: Platform-specific configurations via `AudioConfiguration`

## System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Application Layer                        │
│  (AudioDemo, StreamDemo, PlatformInfo, PerformanceDemo)     │
└────────────────────────┬────────────────────────────────────┘
                         │
┌────────────────────────┴────────────────────────────────────┐
│                  StreamAudio.Core Library                    │
│                                                               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │   Sources    │  │   Playback   │  │   Platform   │      │
│  │              │  │              │  │              │      │
│  │ FileAudio    │  │ Audio        │  │ PlatformInfo │      │
│  │ Source       │  │ Playback     │  │ AudioDevice  │      │
│  │              │  │              │  │ Enumerator   │      │
│  │              │  │ Stream       │  │ AudioConfig  │      │
│  │              │  │ Manager      │  │              │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│                                                               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │    Audio     │  │ Monitoring   │  │    Events    │      │
│  │              │  │              │  │              │      │
│  │ SampleRate   │  │ Performance  │  │ AudioEvent   │      │
│  │ Converter    │  │ Monitor      │  │ Args         │      │
│  │              │  │              │  │              │      │
│  │              │  │              │  │ DeviceEvent  │      │
│  │              │  │              │  │ Args         │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│                                                               │
│  ┌──────────────────────────────────────────────────────┐   │
│  │           AudioEngineManager (Singleton)             │   │
│  └──────────────────────────────────────────────────────┘   │
└────────────────────────┬────────────────────────────────────┘
                         │
┌────────────────────────┴────────────────────────────────────┐
│                   SoundFlow Library                          │
│  (Cross-platform audio engine via MiniAudio)                │
└──────────────────────────────────────────────────────────────┘
```

## Component Descriptions

### Core Components

#### AudioEngineManager
- **Purpose**: Centralized management of the SoundFlow audio engine
- **Pattern**: Singleton
- **Responsibilities**:
  - Initialize and manage the global SoundFlow engine instance
  - Ensure thread-safe access to the engine
  - Provide cleanup on application shutdown

#### AudioPlayback
- **Purpose**: Simplified interface for audio playback and mixing
- **Key Features**:
  - Initialize playback devices
  - Manage the master mixer
  - Add/remove sound players
  - Control volume for individual players
  - Device health monitoring and recovery
- **Events**:
  - `DeviceError`: Raised when playback device encounters errors
  - `DeviceRecovered`: Raised when device is successfully recovered

#### StreamManager
- **Purpose**: High-level stream management with prioritization
- **Key Features**:
  - Manage multiple audio streams
  - Primary/background volume prioritization
  - Dynamic stream addition/removal
  - Fade-in/fade-out transitions
  - Mute/unmute controls
  - Stream monitoring and recovery
- **Events**:
  - `StreamFailed`: Raised when a stream encounters errors
  - `StreamRecovered`: Raised when a stream is recovered

### Source Components

#### FileAudioSource
- **Purpose**: Play audio files with looping support
- **Supported Formats**: WAV, MP3, and other formats supported by SoundFlow
- **Key Features**:
  - Automatic format detection
  - Loop playback support
  - Play/pause/stop controls
  - Format information access

### Platform Components

#### PlatformInfo
- **Purpose**: Platform detection and information
- **Key Features**:
  - Detect operating system
  - Identify Raspberry Pi hardware
  - Provide platform-specific recommendations

#### AudioDeviceEnumerator
- **Purpose**: Enumerate and identify audio devices
- **Key Features**:
  - List playback and capture devices
  - Identify default devices
  - Categorize device types (USB, HDMI, Bluetooth, etc.)
  - Cross-platform device enumeration

#### AudioConfiguration
- **Purpose**: Platform-optimized audio settings
- **Presets**:
  - **Default**: Auto-detects platform and applies optimal settings
  - **Raspberry Pi**: Large buffers for stability on ARM platforms
  - **Low Latency**: Small buffers for real-time applications
- **Configurable Parameters**:
  - Audio format (sample rate, channels, format)
  - Buffer size
  - Period size

### Monitoring Components

#### PerformanceMonitor
- **Purpose**: Real-time performance monitoring
- **Metrics**:
  - CPU usage percentage
  - Memory usage (bytes and MB)
  - Thread count
- **Features**:
  - Snapshot-based metrics collection
  - Continuous async monitoring
  - Formatted output for logging

### Audio Utilities

#### SampleRateConverter
- **Purpose**: Format validation and compatibility checking
- **Key Features**:
  - Detect sample rate mismatches
  - Validate format compatibility
  - Provide warnings for quality issues
  - Recommend optimal mixing formats
- **Note**: SoundFlow handles automatic resampling internally

### Event Components

#### AudioEventArgs
- **Purpose**: Event arguments for stream-related events
- **Properties**:
  - StreamId: Identifier of the affected stream
  - Message: Human-readable error or status message
  - Exception: Original exception if applicable

#### DeviceEventArgs
- **Purpose**: Event arguments for device-related events
- **Properties**:
  - DeviceName: Name of the affected device
  - Message: Human-readable error or status message
  - Exception: Original exception if applicable

## Audio Pipeline Flow

```
Audio File
    │
    ▼
FileAudioSource (Decoding)
    │
    ▼
StreamManager (Volume Control, Prioritization)
    │
    ▼
AudioPlayback (Mixer)
    │
    ▼
SoundFlow Playback Device
    │
    ▼
Audio Output (Speakers, Headphones, etc.)
```

## Volume Management

### Primary/Background Prioritization
- **Primary Stream**: Plays at 100% volume (1.0)
- **Background Streams**: Play at 30% volume (0.3) by default
- **Configurable**: Background volume can be adjusted (0.0 to 1.0)
- **Dynamic**: Primary stream can be changed at runtime
- **Smooth Transitions**: Fade-in/fade-out for seamless volume changes

### Volume Control Hierarchy
1. **Mute State**: Overrides all volume settings (sets to 0.0)
2. **Fade Operations**: Temporary volume transitions
3. **Primary/Background**: Determines target volume
4. **Manual Volume**: Applied via SetVolume on the player

## Error Handling Strategy

### Levels of Error Handling

#### 1. Detection
- Device health checks via `IsDeviceHealthy()`
- Stream monitoring via `MonitorStreams()`
- Automatic error detection during playback

#### 2. Notification
- Events raised for all errors
- Detailed error information in event arguments
- Preserves original exceptions for debugging

#### 3. Recovery
- `TryRecoverStream()`: Attempts to recreate failed streams
- `TryRestartDevice()`: Attempts to restart failed devices
- Automatic retry logic where appropriate

#### 4. Graceful Degradation
- Continue playing other streams if one fails
- Fallback to background volume if primary fails
- Clear error messages for user notification

## Threading Model

### Thread Safety
- **AudioEngineManager**: Thread-safe singleton with lazy initialization
- **StreamManager**: Not thread-safe; use from single thread or add synchronization
- **AudioPlayback**: Device operations are thread-safe via SoundFlow
- **PerformanceMonitor**: Thread-safe for concurrent metric collection

### Async Operations
- **Performance Monitoring**: Async enumerable for continuous monitoring
- **Fade Operations**: Timer-based async volume transitions
- **Stream Monitoring**: Can be called periodically on background thread

## Memory Management

### Resource Lifetime
- **AudioEngine**: Global singleton, disposed on application exit
- **AudioPlayback**: Disposable; stops device on disposal
- **StreamManager**: Disposable; removes all streams and stops sources
- **FileAudioSource**: Disposable; releases file handles and players
- **PerformanceMonitor**: Disposable; releases process handle

### Best Practices
1. Always use `using` statements or dispose explicitly
2. Remove streams from manager before disposing them
3. Stop playback before disposing resources
4. Avoid creating multiple AudioPlayback instances (use singleton pattern)

## Platform Considerations

### Windows
- Full development and testing support
- All features available
- Visual Studio integration

### Linux (Including Raspberry Pi)
- Primary deployment target
- Optimized buffer configurations for ARM
- ALSA/PulseAudio support via SoundFlow
- USB audio device support

### macOS
- Full compatibility via SoundFlow
- Development and testing supported

### Raspberry Pi Specific
- Large buffer sizes for stability
- USB audio device detection
- HDMI audio support
- Optimized for ARM architecture

## Testing Strategy

### Unit Tests
- Test individual components in isolation
- Mock SoundFlow dependencies where needed
- Focus on business logic and edge cases
- 64+ tests covering all components

### Integration Tests
- Test component interactions
- Use real audio files from testdata/
- Verify actual playback when not in headless mode
- Skip audio tests in CI environments

### Performance Tests
- Measure CPU and memory usage
- Validate real-time performance
- Check for memory leaks
- Monitor thread count

### Platform Tests
- Test on Windows, Linux, and Raspberry Pi
- Verify device enumeration on each platform
- Check audio output quality
- Validate cross-platform compatibility

## Future Extensibility

### Planned Extensions
1. **Additional Sources**: Network streams, TTS, USB audio input
2. **Advanced Effects**: Equalizer, filters, audio processing
3. **Recording**: Capture mixed output to file
4. **Remote Control**: Network API for remote management
5. **Visualization**: Real-time audio visualization

### Extension Points
- `FileAudioSource` can be used as template for other source types
- `StreamManager` can be extended for advanced routing
- `AudioConfiguration` can add new platform-specific presets
- Event system allows pluggable monitoring and logging

## Performance Characteristics

### Latency
- **Windows**: ~10-50ms depending on buffer size
- **Linux**: ~20-100ms depending on ALSA/PulseAudio configuration
- **Raspberry Pi**: ~50-200ms with stability-optimized buffers

### CPU Usage
- **Idle**: <1% CPU
- **Single Stream**: 1-5% CPU
- **Multiple Streams**: 2-10% CPU depending on format and sample rate
- **Format Conversion**: +2-5% CPU per stream requiring conversion

### Memory Usage
- **Base**: ~10-20 MB
- **Per Stream**: ~1-5 MB depending on buffer configuration
- **Peak**: <100 MB for typical usage (5-10 streams)

## Phase 9: REST API and Advanced Features

Phase 9 introduces production-ready infrastructure including storage, REST API, device management, and Google Cast support.

### Storage System

**Purpose**: Persistent storage for configuration, metadata, and application state

**Implementations:**
- **JsonFileStorage**: Human-readable JSON files, one per table
- **SqliteStorage**: Relational database for complex queries and larger datasets

**Key Features:**
- Unified `IStorage` interface for both backends
- Table-based organization with key-value storage
- Built-in backup and restore functionality
- Migration support between storage types
- Simple secrets management system
- Automatic secret resolution in loaded data

**Architecture:**
```
┌─────────────────────────────────────────────────────┐
│              StorageManager (Singleton)             │
│                      IStorage                       │
└──────────────────────┬──────────────────────────────┘
                       │
        ┌──────────────┴──────────────┐
        │                             │
┌───────▼───────┐             ┌───────▼──────┐
│ JsonFile      │             │   Sqlite     │
│ Storage       │             │   Storage    │
└───────────────┘             └──────────────┘
```

**Storage Tables:**
- `devices`: Device configurations (ChromeCast, audio sources, playback devices)
- `[SECRETS]`: Secure storage for API keys and tokens
- `metadata_history`: Song metadata history for Manual sources
- `settings`: Application preferences and configuration
- Custom tables for application-specific data

### Device Management

**Purpose**: Unified management of audio sources and playback devices

**DeviceManager Features:**
- Enumerate available hardware devices
- Create and configure software sources (TTS, File, Spotify)
- Manage ChromeCast playback devices
- Store and retrieve device configurations from storage
- Auto-create "Auto" type sources for alerts and notifications
- Filter device visibility for UI presentation

**Device Categories:**
- **Audio Sources**: File, Spotify, USB (Vinyl/Radio), TTS
- **Playback Devices**: Hardware outputs, ChromeCast, FFT (analysis)
- **Auto Sources**: Short-lived alerts, TTS notifications, doorbells

### ChromeCast Integration

**Purpose**: Stream audio to Google Cast devices with metadata support

**ChromeCastAudioPlayback:**
- Implements `IAudioPlayback` interface
- Device discovery and connection management
- Audio streaming with metadata
- Configuration storage with secrets support
- Device health monitoring and recovery
- Extensible framework for full GoogleCast SDK integration

**Metadata Support:**
- Song title, artist, album
- Album artwork URL
- Radio station and frequency information
- Duration and position tracking

**Configuration:**
```csharp
var config = new ChromeCastConfiguration
{
    DeviceName = "Living Room Speaker",
    DeviceId = "device-unique-id"
};
await storage.SaveAsync("ChromeCast", "living-room", config);
```

### REST API (StreamAudio.Api)

**Purpose**: HTTP API for remote control and integration

**Controllers:**

1. **StorageController** (`/api/storage`)
   - Full CRUD operations for all storage tables
   - Backup and restore operations
   - Table and key enumeration

2. **DevicesController** (`/api/devices`)
   - List available audio sources and playback devices
   - Manage device configurations
   - Create Auto sources (TTS, file alerts)

3. **StreamsController** (`/api/streams`)
   - Initialize/shutdown StreamManager
   - Add/remove audio sources
   - Control playback (play, pause, stop)
   - Manage primary stream and background volume
   - Mute/unmute operations
   - Fade in/out transitions
   - Get stream status and metrics

**API Features:**
- Swagger/OpenAPI documentation at root (`/`)
- JSON request/response format
- Consistent error handling
- Comprehensive logging
- RESTful design patterns

**Example Usage:**
```bash
# Initialize stream manager
curl -X POST http://localhost:5000/api/streams/initialize

# Add a file source
curl -X POST http://localhost:5000/api/streams/sources/file \
  -H "Content-Type: application/json" \
  -d '{"streamId":"music1","filePath":"song.mp3","isPrimary":true}'

# Control playback
curl -X POST http://localhost:5000/api/streams/sources/music1/play?fadeIn=true

# Get status
curl http://localhost:5000/api/streams/status
```

### Metadata Enhancements

**New SongMetadata Properties:**
- `Band`: Radio band identifier (AM, FM, SW)
- `FrequencyHz`: Station frequency for radio sources

**Metadata History:**
- Automatic tracking for Manual source type
- Timestamp-based storage
- Queryable history via storage system
- Used for "recently played" features

### Configuration System

**ConfigurationManager Features:**
- Structured logging with Serilog
- Environment variable support
- `appsettings.json` configuration
- Root directory management
- Storage type selection
- Backup directory configuration

**Configuration Structure:**
```json
{
  "RootDir": "./",
  "StorageType": "Json",
  "Logging": {
    "MinimumLevel": "Information",
    "Directory": "logs"
  },
  "Storage": {
    "Directory": "storage",
    "BackupDirectory": "backup"
  }
}
```

### Architecture Update

Phase 9 expands the system architecture:

```
┌──────────────────────────────────────────────────────┐
│          Web Applications & Clients                   │
│    (Browsers, Mobile Apps, REST API Clients)         │
└─────────────────────┬────────────────────────────────┘
                      │ HTTP/REST
┌─────────────────────▼────────────────────────────────┐
│              StreamAudio.Api (ASP.NET)               │
│  ┌──────────────┐ ┌──────────┐ ┌──────────────┐     │
│  │   Storage    │ │ Devices  │ │   Streams    │     │
│  │  Controller  │ │Controller│ │  Controller  │     │
│  └──────────────┘ └──────────┘ └──────────────┘     │
└─────────────────────┬────────────────────────────────┘
                      │
┌─────────────────────▼────────────────────────────────┐
│              StreamAudio.Core Library                 │
│  ┌────────────┐ ┌──────────┐ ┌────────────────┐     │
│  │  Storage   │ │  Device  │ │  ChromeCast    │     │
│  │  Manager   │ │  Manager │ │  Playback      │     │
│  └────────────┘ └──────────┘ └────────────────┘     │
│  ┌────────────┐ ┌──────────┐ ┌────────────────┐     │
│  │ Sources    │ │ Playback │ │ Configuration  │     │
│  │ (existing) │ │(existing)│ │   Manager      │     │
│  └────────────┘ └──────────┘ └────────────────┘     │
└──────────────────────────────────────────────────────┘
```

### Testing

**Phase 9 Test Coverage:**
- 266 total tests (13 new in Phase 9)
- ChromeCastAudioPlayback: 13 tests
- Storage system: 26 tests (from previous phase)
- Device Manager: 15 tests (from previous phase)
- Full API integration testing (via Swagger)
- 100% pass rate on all platforms

### Future Enhancements

**Planned for ChromeCast:**
- Full GoogleCast SDK integration
- Device discovery protocol
- Multi-room audio synchronization
- Queue management
- Media session control

**Planned for REST API:**
- WebSocket support for real-time events
- Authentication and authorization
- Rate limiting and throttling
- API versioning
- GraphQL endpoint option

**Planned for Storage:**
- Encryption at rest
- Azure/AWS cloud storage backends
- Redis cache integration
- Replication and clustering

## Dependencies

### Direct Dependencies
- **.NET 8.0+**: Runtime platform
- **SoundFlow 1.2.1**: Audio engine and playback
- **xUnit**: Testing framework (test projects only)
- **FluentAssertions**: Assertion library (test projects only)

### Transitive Dependencies
- **MiniAudio**: Via SoundFlow for low-level audio
- Platform-specific audio APIs (ALSA, PulseAudio, CoreAudio, WASAPI)

## Deployment Considerations

### Package Size
- Typical deployment: ~5-10 MB
- Includes .NET runtime trimming where possible
- Native libraries included automatically by SoundFlow

### Configuration
- No configuration files required for basic usage
- Environment-based configuration supported
- Platform auto-detection at runtime

### Updates
- NuGet packages for dependencies
- Self-contained deployment option available
- Framework-dependent deployment for smaller size
