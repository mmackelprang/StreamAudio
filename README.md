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

## Current Status: Phase 4 IN PROGRESS

**Last Updated**: November 16, 2025

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

### Phase 4: Dynamic Stream Management
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
### Phase 5: Advanced Features & Robustness
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
### Phase 6: Documentation, Packaging & Deployment
* Goals
  - Finalize documentation and usage guides.
  - Package software for easy deployment on Raspberry Pi.
  - Prepare CI/CD pipelines for build/test/deploy.
* Tasks
  - Document architecture, interfaces, deployment steps.
  - Package app with dependencies for Raspberry Pi (including native binaries).
  - Automated build pipelines with Windows and Linux targets.
  - Conduct user acceptance testing (UAT) and fix discovered issues.

### Phase 7: System Integration
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

