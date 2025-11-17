# StreamAudio
## This project is a testbed for audio management software that needs to:
* Manage one or more incoming audio streams.  This can be audio of various formats, but should be a real-time stream of data that could be from:
  - Radio audio stream arriving via a USB audio device
  - TTS audio stream arriving from an TTS server
  - Song data streaming from an MP3, WAV or other song data file
  - Vinyl record audio streaming from another USB audio device
  - Spotify Audio data streaming from Spotify, etc.
* This software needs to mix these audio streams (which means the audio formats will need to be normalized).
* One of the streams will be "primary" which will play at the "Primary" volume while the other streams will be played at a much lower volume the "background".
* Streams can be set to play once or repeat when they are finished.  This will be a setting on the stream, and will honored by the audio controller.

## Development Guidlines:
* This project will be developed on a Windows computer and much of the testing will work there, but that the project will be deployed on a Raspberry Pi.
* As work is done, update this README file with progress through the phase completed and what to do next.  Maintain enough state to make it easy to see where to pick up next.
* Include the generation of testing data and unit/integration tests as part of this project plan.
* Use the NAudio library to simplify audio streaming and mixing.
* Look for existing libraries for new functionality whenever possible.

## Best Practices for Audio Streaming and Mixing
* Use 32-bit float audio processing for mixing to avoid clipping and maintain quality.
* Synchronize audio streams' sample rates and formats where possible, or perform sample rate conversion.
* Implement volume control as gain multipliers on float samples before mixing to prioritize “primary” audio and attenuate background streams.
* Maintain low-latency, buffer management, and thread safety for real-time audio input/output.
* Use a modular pipeline design: Input Sources → Codec/Decoder → Mixer (with volume control) → Output Device/Stream.
* Minimize memory allocations during streaming to avoid glitches and GC pauses.
* Provide error handling and fallback for device disconnections or corrupted streams.
* Design interfaces to abstract audio sources and sinks so new device types or stream formats can be plugged in easily.

## Project Plan

## Current Status: Phase 7 COMPLETED ✅

**Last Updated**: November 17, 2025

### Phase 1: Requirements & Tooling Setup - **COMPLETED** ✅
* Goals
  - Create the directory layout for the project using best practices for modern c# projects.  Use .net 8+ for the version.
  - Define functional and non-functional requirements.
    - Use SoundFlow libraries for cross-platform audio support. 
  - Establish xUnit and FluentAssertions for both unit and integration tests.
  - Create some test utilities to make audio files for unit and integration tests later:
    - Generate a simple command line app to generate small audio files.  The app should take the following parameters:
      - sine wave frequency
      - tone duration
      - audio encoding type
      - output file name - discern which type of encoding for the file based on the extension - MP3 or WAV
    - Generate the following 1 second files: 50 Hz sine wave, 100 Hz sine wave, 200 Hz sine wave.
    - Use these sample files for testing and verifying the various audio components as the build progresses.
* Tasks
  - Detail audio input/output sources: USB audio devices, TTS server streams, file formats (MP3/WAV/etc.).
  - Choose cross-platform audio library: SoundFlow (.NET audio engine) for its cross-OS support and C# friendliness.
  - Set up .NET 8+ development environment on Windows.
  - Define architecture aligned with SoundFlow's component model.
  - Plan automated testing framework: xUnit for unit and integration tests.
  - Define test audio data generation: scripted sine tones (e.g., 100Hz, 200Hz) and sample WAV clips.
* **Completed Deliverables**:
  - ✅ Modern .NET 8 solution structure with src/, tests/, and tools/ directories
  - ✅ SoundFlow-based architecture:
    - `AudioEngineManager`: Centralized SoundFlow engine management
    - `FileAudioSource`: Wrapper for SoundFlow's SoundPlayer
    - `AudioPlayback`: Playback device and mixer management
  - ✅ ToneGenerator CLI tool for creating test audio files
  - ✅ Generated test files: 50hz.wav, 100hz.wav, 200hz.wav
  - ✅ SoundFlow 1.2.1 integration
  - ✅ xUnit + FluentAssertions test framework
  
### Phase 2: Prototype Audio Pipeline on Windows - **COMPLETED** ✅
* Goals
  - Build baseline audio streaming pipeline: input sources → mixer → output device.
    - For the initial test, use a file stream from one of the generated tone files, and assume the output device is the main output device on the development computer.
  - Implement basic volume control and mixing using SoundFlow.
  - Implement repeat functionality for the input audio.
  - Verify output device selection on Windows.
* Tasks
  - Migrate from NAudio to SoundFlow for better cross-platform support.
  - Refactor architecture to align with SoundFlow's component model.
  - Implement FileAudioSource wrapper for SoundFlow's SoundPlayer.
  - Use SoundFlow's built-in Mixer component directly.
  - Create AudioPlayback manager for playback device and mixer.
  - Add unit tests:
    - File audio source initialization and playback.
    - Mixer operations (add/remove, volume control).
  - Integration tests with actual audio playback.
  - Update AudioDemo to showcase new architecture.
* **Completed Deliverables**:
  - ✅ **SoundFlow Migration**: Complete refactor from NAudio to SoundFlow 1.2.1
  - ✅ **AudioEngineManager**: Singleton pattern for SoundFlow engine management
  - ✅ **FileAudioSource**: Clean wrapper for file playback with loop support
  - ✅ **AudioPlayback**: Simplified playback device and mixer management
  - ✅ **Built-in Mixer**: Direct use of SoundFlow's high-performance Mixer
  - ✅ Unit tests: 10 passing tests covering all functionality
  - ✅ **AudioDemo** tool: Interactive demo with three scenarios:
    - Single tone playback
    - Dual-source mixing with equal volume
    - Primary/background volume control
  - ✅ Cross-platform ready (Windows/Linux/macOS/Raspberry Pi support)
  - ✅ CI-friendly tests (skip audio in headless environments)
### Phase 3: Cross-Platform Abstraction & Raspberry Pi Port - **COMPLETED** ✅
* Goals
  - Abstract platform-specific code behind interfaces.
  - Ensure cross-platform compatibility for Windows, Linux, macOS, and Raspberry Pi.
  - Build and test on Linux (Raspberry Pi compatible).
* Tasks
  - Implement cross-platform audio input/output layers for Raspberry Pi audio APIs (ALSA/PulseAudio) via SoundFlow.
  - Create platform detection and configuration utilities.
  - Implement cross-platform device enumeration.
  - Add platform-optimized audio buffer configurations.
  - Create comprehensive tests for all platforms.
  - Document platform-specific setup and usage.
* **Completed Deliverables**:
  - ✅ **Platform Detection**: `PlatformInfo` class for OS and Raspberry Pi detection
  - ✅ **Audio Configuration**: Platform-optimized settings with three presets:
    - Default (auto-detects platform)
    - Raspberry Pi optimized (large buffers for stability)
    - Low latency (small buffers for real-time)
  - ✅ **Device Enumeration**: `AudioDeviceEnumerator` for cross-platform device listing
    - Lists playback and capture devices
    - Identifies default devices
    - Categorizes device types (USB, HDMI, Bluetooth, etc.)
  - ✅ **PlatformInfo Tool**: CLI utility to display system capabilities
  - ✅ **Extended AudioPlayback**: Support for AudioConfiguration
  - ✅ **Comprehensive Testing**: 30 tests (10 original + 20 platform tests), all passing
  - ✅ **Documentation**: Complete PLATFORM.md guide covering:
    - Platform detection
    - Device enumeration
    - Configuration options
    - Raspberry Pi setup
    - Troubleshooting
  - ✅ **Tested on Linux**: Verified on Ubuntu 24.04 (Raspberry Pi compatible)
  - ✅ **Cross-platform ready**: SoundFlow backend supports all platforms via MiniAudio

### Phase 4: Dynamic Stream Management - **COMPLETED** ✅
* Goals
  - Implement dynamic source volume control and prioritization.
  - Enhance runtime API for managing active sources.
  - Add advanced stream control features.
* Tasks
  - Extend mixer to dynamically change primary stream and adjust background volumes accordingly.
  - Add runtime APIs to add/remove sources and change primary stream.
  - Implement mute/unmute controls.
  - Add fade-in/fade-out capabilities for smooth transitions.
  - Extend tests to cover volume prioritization and stream management scenarios.
* **Completed Deliverables**:
  - ✅ **StreamManager**: Comprehensive stream management with primary/background prioritization
    - Dynamic add/remove sources at runtime
    - Primary stream designation with automatic volume control
    - Background streams play at configurable reduced volume (default 30%)
    - Switch primary stream seamlessly during playback
  - ✅ **Mute/Unmute Controls**: Per-stream mute/unmute functionality
    - Independent mute state for each stream
    - Volume preservation when unmuting
  - ✅ **Fade Transitions**: Smooth volume transitions
    - Fade-in capability for gradual stream introduction
    - Fade-out capability for graceful stream removal
    - Configurable fade duration (default 1 second)
  - ✅ **Runtime API**: Complete stream management interface
    - `AddSource()`: Add new audio sources dynamically
    - `RemoveSource()`: Remove sources with optional fade-out
    - `SetPrimaryStream()`: Designate primary stream
    - `ClearPrimaryStream()`: Reset to all-background mode
    - `Play()`, `Pause()`, `Stop()`: Individual stream control
    - `Mute()`, `Unmute()`: Per-stream mute controls
    - `FadeIn()`, `FadeOut()`: Smooth transitions
  - ✅ **Comprehensive Testing**: 14 new tests (44 total tests), all passing
    - Primary/background volume prioritization
    - Dynamic stream addition/removal
    - Mute/unmute functionality
    - Fade transitions
    - Primary stream switching
    - Volume adjustment validation
  - ✅ **StreamDemo Tool**: Interactive demonstration of all Phase 4 features
    - Primary/background control demonstration
    - Mute/unmute demonstration
    - Fade transition demonstration
    - Dynamic stream management demonstration
### Phase 5: Advanced Features & Robustness - **COMPLETED** ✅
* Goals
  - Introduce error handling, reconnection logic.
  - Support multiple audio formats and resampling if needed.
  - Continuous testing and profiling.
* Tasks
  - Implement fallback recovery on device loss or stream failure.
  - Add sample rate conversion if input streams have different sample rates.
  - Profile performance on Windows and Raspberry Pi for CPU and memory usage.
  - Expand automated tests with recorded real device streams.
  - Generate detailed test reports.
* **Completed Deliverables**:
  - ✅ **Error Handling & Recovery System**: Event-based error handling
    - `AudioEventArgs` and `DeviceEventArgs` for error reporting
    - Stream failure and recovery events in StreamManager
    - Device error and recovery events in AudioPlayback
    - `TryRecoverStream()`: Attempt stream recovery after failure
    - `MonitorStreams()`: Proactive error detection
    - `IsDeviceHealthy()` and `TryRestartDevice()`: Device health management
  - ✅ **Performance Monitoring**: Complete performance tracking infrastructure
    - `PerformanceMonitor` class with CPU, memory, and thread monitoring
    - Real-time metrics collection
    - Async monitoring with `MonitorAsync()`
    - Performance snapshots with formatted output
    - PerformanceDemo tool for live monitoring demonstration
  - ✅ **Sample Rate Conversion**: Format validation and compatibility
    - Sample rate mismatch detection via `HasMatchingSampleRate()`
    - Format compatibility validation via `AreFormatsCompatible()`
    - Recommended mixing format selection via `GetRecommendedMixingFormat()`
    - Validation with detailed warnings via `ValidateForMixing()`
    - Note: SoundFlow handles automatic resampling internally
  - ✅ **Expanded Test Coverage**: 20 new tests, 64 total tests (all passing)
    - Error handling tests (8 tests)
    - Performance monitoring tests (8 tests)
    - Sample rate converter tests (6 tests)
    - Full coverage of new Phase 5 features

### Phase 6: Documentation, Packaging & Deployment - **COMPLETED** ✅
* Goals
  - Finalize documentation and usage guides.
  - Package software for easy deployment on Raspberry Pi.
  - Prepare CI/CD pipelines for build/test/deploy.
* Tasks
  - Document architecture, interfaces, deployment steps.
  - Package app with dependencies for Raspberry Pi (including native binaries).
  - Automated build pipelines with Windows and Linux targets.
  - Conduct user acceptance testing (UAT) and fix discovered issues.
* **Completed Deliverables**:
  - ✅ **Comprehensive Documentation**:
    - `ARCHITECTURE.md`: Complete system architecture documentation
      - Component descriptions and interactions
      - Design patterns and principles
      - Audio pipeline flow diagrams
      - Threading and memory management
      - Performance characteristics
      - Extension points for future features
    - `DEPLOYMENT.md`: Raspberry Pi deployment guide
      - Step-by-step deployment instructions
      - Multiple deployment methods (self-contained, framework-dependent, from source)
      - Systemd service configuration
      - Performance optimization tips
      - Multi-device deployment scripts
    - `TROUBLESHOOTING.md`: Comprehensive troubleshooting guide
      - Audio playback issues
      - Device detection issues
      - Performance issues
      - Platform-specific solutions
      - Error message reference
      - Diagnostic commands
  - ✅ **CI/CD Pipeline**: GitHub Actions workflow
    - Multi-platform builds (Windows, Linux, macOS)
    - Automated testing on all platforms
    - Raspberry Pi ARM64 and ARM builds
    - Artifact publishing for releases
    - Test result upload and reporting
  - ✅ **Build Configurations**: Ready-to-deploy packages
    - Self-contained deployment (includes runtime)
    - Framework-dependent deployment (smaller size)
    - All tools packaged for Raspberry Pi
    - Automated build scripts included

### Phase 7: Audio Stream Improvements - **COMPLETED** ✅
* Goals and Tasks
  - Create an interface for AudioPlayback - IAudioPlayback so we can easily mock actual audio streams and create other types of playback devices like a Chromcast device.
  - Create an FFTAudioPlayback device that will accept incoming audio, and once the audio stream is complete, perform an FFT on the audio data.  It should make the top 5 frequencies and intensity and total audio duration avaiable.  This will be a tool we use to do integration tests using the sample data already created.
  - Create an interface for AudioSources, and make FileAudioSource an implementation of that interface.  Realize that we will want to create at least the following AudioSources eventually (out of scope for this phase):
    - TtsAudioSource - Sends text to a TTS engine and recives an audio stream from this.
    - CompositeAudioSource - Has a set of both text and audio files that are played in a particular order (to have a phone ring while announcing who is calling for example).
  - For the AudioSouce interface, introduce a new variable called SourceType.  This can have two values:
    - Manual - this type of audio source is set by the user and is considered to be a long-running audio source.  This would include sources like Spotify, Audio File Selector that allows selecting a playlist or directory, USB Radio, USB Phonograph.  These AudioSource types normally have a user interface with two parts - the device configuration UI and the playback management UI.  Creation of these devices is out of scope for this phase, but will be addressed in a later phase.
    - Auto - this type of audio source is set by the stytem and is considered to be a short-running audio event.  This would include things like a Phone Ring, Doorbell Ring, System Notification, etc. Once the audio from Auto sources completes, they remove are automatically removed from the StreamManager.
 - The StreamManager is updated to provide the following notifications:
    - Audio Play Begin - This happens when there is no audio streaming and any auto audio source begins streaming.
    - All Audio Complete. This happens whenever no AudioSources are finished streaming.
  - Auto AudioSources should have a repeat count that is honored by AudioManager.  This should default to 1. If the repeat count is 0, it loops forever. 
  - AudioManager doesn't actully allow an AudioSource to loop forever - it has a configurable max duration for Auto AudioStreams. Set the default value of the MaxStreamDuration to 30 sec. 
  - Create new system tests to verify the following:
    - Verify the StreamManager events work as expected.
    - Using the FFT AudioPlayback type, verify that mixing works with various tone input files.
    - Using the FFT AudioPlayback type, verify that audio duration is correct for various repeat lengths.
    - Verify that Auto and Manual AudioSources are managed by StreamManager as expected.
    - Verify that StreamManager honors the MaxStreamLength parameter for Auto AudioSource
    - Any other relevant tests.
* **Completed Deliverables**:
  - ✅ **IAudioPlayback Interface**: Abstraction for all playback devices
    - Enables mocking for testing
    - Foundation for future Chromecast or network streaming devices
    - Implemented by AudioPlayback and FFTAudioPlayback
  - ✅ **FFTAudioPlayback**: Testing playback device with FFT analysis
    - Captures audio data during playback
    - Performs FFT analysis on completion
    - Exposes top 5 frequencies with intensities
    - Tracks total audio duration
    - Used for integration testing of mixing and duration
  - ✅ **IAudioSource Interface**: Abstraction for all audio sources
    - Common interface for FileAudioSource and future sources (TTS, Composite, etc.)
    - Exposes Name, Format, SampleRate, Channels, Player
    - Includes SourceType and RepeatCount properties
  - ✅ **SourceType Enum**: Manual vs Auto source classification
    - Manual: User-controlled, long-running (Spotify, playlists, USB devices)
    - Auto: System-controlled, short-running (notifications, doorbells, phone rings)
  - ✅ **FileAudioSource Enhancement**: Implements IAudioSource
    - Supports SourceType configuration
    - RepeatCount property (default 1, 0 for infinite)
    - Backward compatible with existing code
  - ✅ **StreamManager Enhancements**: Auto source lifecycle management
    - Accepts IAudioSource and IAudioPlayback interfaces
    - MaxStreamDuration property (default 30 seconds)
    - Automatic monitoring of Auto sources every 100ms
    - Auto-removal of completed Auto sources
    - Manual sources remain until explicitly removed
  - ✅ **StreamManager Events**: Lifecycle notifications
    - AudioPlayBegin: Fires when audio starts from idle state
    - AllAudioComplete: Fires when all audio sources finish
  - ✅ **Comprehensive Testing**: 89 tests total, all passing
    - 64 original tests from Phases 1-6
    - 19 audio source lifecycle tests (AudioSourceLifecycleTests.cs)
    - 6 mixer and format conversion tests (AudioMixerTests.cs)
    - StreamManager event verification
    - FFT analysis functionality
    - Auto/Manual source management
    - MaxStreamDuration enforcement
    - RepeatCount behavior
    - Interface contract validation
    - Mixed source type scenarios
    - Mono to stereo conversion verification
  - ✅ **Example Applications**: Three new demo applications
    - **FFTDemo**: Demonstrates FFT analysis capabilities
      - Single tone frequency detection
      - Mixed tone analysis
      - Duration tracking and memory usage reporting
    - **AutoSourceDemo**: Demonstrates Auto vs Manual source lifecycle
      - Auto source automatic removal
      - Manual source persistence
      - MaxStreamDuration enforcement
      - Mixed source type management
      - Stream lifecycle events
    - Updated existing demos with Phase 7 features
  - ✅ **Documentation Updates**:
    - FFTAudioPlayback memory usage warnings and guidelines
    - Improved audio capture documentation
    - Test file naming conventions (descriptive names vs phase numbers)

### Phase 8: System Integration
* Goals
  - Integrate external audio stream sources into the system.
  - Add TTS audio stream input support.
  - Integrate additional streaming sources (USB audio, network streams, etc.).
  - Validate end-to-end system functionality.
* Tasks
  - Integrate TTS streams (network stream decoding to PCM).
  - Add USB audio device input stream support.
  - Implement network audio stream handling.
  - Add radio stream input capabilities.
  - Extend tests to cover TTS stream scenarios and external source integration.
  - Perform comprehensive integration testing with all audio sources.
  - Validate system performance with multiple concurrent streams.
### Testing Data & Automation
* Synthetic Test Tones: Generate short WAV files with 1-second 100Hz and 200Hz sine wave tones included in unit tests.
* Mixed Output Validation: Automated tests confirm amplitude, sample counts, wave shapes of mixed output match expected composite tone.
* Integration Tests: Simulate multiple concurrent streams from files and USB devices.
* Latency & Performance Benchmarks: Measure real-time latency and processor load.
* Regression Tests: On each release, run full test suite on both Windows dev machine and Raspberry Pi.
* Proposed Tests
#### Test Mixing Two Tones at Equal Volume
* Input: 1-second 100 Hz tone + 1-second 200 Hz tone WAV files.
* Action: Mix streams with equal volume, no priority.
* Expected: Output waveform amplitude matches sum of two tones without clipping (clamped at ±1.0 float).
* Verify via sample max amplitude and FFT frequency peaks at 100Hz and 200Hz.
#### Test Primary/Background Volume Priority
* Input: Same two tones as above.
* Action: Set 100 Hz tone as primary with full volume; 200 Hz tone as background at 30% volume.
* Expected: Output amplitude dominated by 100 Hz; 200 Hz present but attenuated accordingly.
* Verify relative amplitude ratios roughly match volume settings.
####Test Muting a Background Stream
* Input: Same two tones.
* Action: Mute 200 Hz (background) stream while playing 100 Hz primary.
* Expected: Output waveform matches 100 Hz tone only.
* Confirm FFT peak only at 100Hz.
#### Test Switching Primary Stream
* Input: Same two tones.
* Action: Change primary stream from 100 Hz to 200 Hz during playback.
* Expected: Output amplitude and dominant frequency switches correspondingly.
* Verify seamless transition and continuous playback.
#### Test Different Formats Mixing
* Input: One WAV tone + one MP3 tone decoded to float PCM.
* Action: Mix with specified volume levels.
* Expected: Mixed output coherent without artifacts.
* Verify audio quality and amplitude correctness.
#### Latency & Continuity Test
* Play mixed audio for several seconds, verify no audio glitches or buffer underrun errors occur.
#### Automated Signal Verification
* For all tests, compare mixed output PCM waveform samples against expected floating-point reference computed mathematically.

This phased plan provides a clear path from Windows development to cross-platform deployment on Raspberry Pi, utilizes AI where helpful, and emphasizes test-driven progress with high-quality, maintainable code. SoundFlow (or NAudio fallback on Windows) and abstraction interfaces will smooth cross-platform operation.

