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

## Current Status: Phase 2 COMPLETED ✅

**Last Updated**: November 16, 2024

### Phase 1: Requirements & Tooling Setup - **COMPLETED** ✅
* Goals
  - Create the directory layout for the project using best practices for modern c# projects.  Use .net 8+ for the version.
  - Define functional and non-functional requirements.
    - Use NAudio libraries as much as possible to simplify development. 
  - Establish xUnit and FluentAssertions for both unit and integration tests.
  - Create some test utilities to make audio files for unit and integration tests later:
    - Generate a simple command line app to generate small audio files.  The app should take the following parameters:
      - sine wave frequency
      - tone duration
      - audio encoding type
      - output file name - discern which type of encoding for the file based on the extension - MP3 or WAV
    - Generate the following 1 second files: 50 Hz sine wave, 100 Hz sine wave, 200 Hz sine wave.
    - Use these sample files for testing and verifying the various audio components as the build progresses.  The simple
* Tasks
  - Detail audio input/output sources: USB audio devices, TTS server streams, file formats (MP3/WAV/etc.).
  - Choose cross-platform audio library: SoundFlow (.NET Core audio engine) recommended for its cross-OS support and C# friendliness, with fallback to NAudio on Windows for faster dev.
  - Set up .NET 7+ development environment on Windows.
  - Define interfaces for audio input, mixing, and output abstracted from platform dependencies.
  - Plan automated testing framework: xUnit for unit and integration tests.
  - Define test audio data generation: scripted sine tones (e.g., 100Hz, 200Hz) and sample WAV clips.
* **Completed Deliverables**:
  - ✅ Modern .NET 8 solution structure with src/, tests/, and tools/ directories
  - ✅ Core interfaces: `IAudioSource`, `IAudioOutput`, `IMixer`
  - ✅ ToneGenerator CLI tool for creating test audio files
  - ✅ Generated test files: 50hz.wav, 100hz.wav, 200hz.wav
  - ✅ NAudio 2.2.1 integration
  - ✅ xUnit + FluentAssertions test framework
  
### Phase 2: Prototype Audio Pipeline on Windows - **COMPLETED** ✅
* Goals
  - Build baseline audio streaming pipeline: input sources → mixer → output device.
    - For the initial test, use a file stream from one of the generated tone files, and assume the output device is the main output device on the development computer.
  - Implement basic volume control and mixing.
  - Implement repeat functionality for the input audio.
  - Verify output device selection on Windows.
* Tasks
  - Implement IAudioSource and concrete classes for file playback and USB audio input (using NAudio).
  - Develop floating-point mixing engine with gain control for primary/background streams.
  - Build IAudioOutput with device enumeration and selection (using NAudio's WASAPI and WaveOut APIs).
  - Add unit tests:
    - Mixing 100Hz + 200Hz sine wave test for audio correctness.
    - Device enumeration and selection test.
  - Integration tests simulating multiple active audio streams.
  - Use AI code generation tools to scaffold and customize prototype components – continuously review and refine AI outputs.
* **Completed Deliverables**:
  - ✅ `FileAudioSource`: Reads audio from files with repeat/loop support
  - ✅ `BasicMixer`: 32-bit float mixing with volume control and primary/background prioritization
  - ✅ `WaveOutAudioOutput`: Cross-platform audio output using NAudio's WaveOutEvent
  - ✅ Unit tests: 13 passing tests covering all functionality
  - ✅ `AudioDemo` tool: Interactive demo showing single playback, mixing, and volume control
  - ✅ Thread-safe implementation with proper resource disposal
  - ✅ Verified mixing correctness with test tones
### Phase 3: Cross-Platform Abstraction & Raspberry Pi Port
* Goals
  - Abstract platform-specific code behind interfaces.
  - Replace Windows-only code with cross-platform SoundFlow or PortAudio bindings for Linux.
  - Build and test on Raspberry Pi OS.
* Tasks
  - Implement cross-platform audio input/output layers for Raspberry Pi audio APIs (ALSA/PulseAudio) via SoundFlow.
  - Adapt USB audio input reading for Raspberry Pi specifics.
  - Verify audio output device selection works on Raspberry Pi.
  - Automate test runs on Raspberry Pi via remote access, including unit and integration tests from Phase 2.
  - Adjust buffer sizes, latency settings for best real-time audio performance.
### Phase 4: TTS Integration & Stream Management
* Goals
  - Add TTS audio stream input support.
  - Implement dynamic source volume control and prioritization.
  - Enhance runtime API for managing active sources.
* Tasks
  - Integrate TTS streams (network stream decoding to PCM).
  - Extend mixer to dynamically change primary stream and adjust background volumes accordingly.
  - Add runtime APIs to add/remove sources and change primary stream.
  - Implement mute/unmute controls.
  - Extend tests to cover TTS stream scenarios and volume prioritization logic.
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

