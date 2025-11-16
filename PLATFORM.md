# StreamAudio Platform Support

StreamAudio is designed to work across multiple platforms with optimized configurations for each environment.

## Supported Platforms

- **Windows** - Full support with DirectSound/WASAPI
- **Linux** - Full support with ALSA/PulseAudio
- **macOS** - Full support with CoreAudio
- **Raspberry Pi** - Optimized support for ARM-based single-board computers

## Platform Detection

The `PlatformInfo` class provides runtime platform detection:

```csharp
using StreamAudio.Core.Platform;

// Detect the current platform
Console.WriteLine($"Platform: {PlatformInfo.PlatformName}");
Console.WriteLine($"OS: {PlatformInfo.OSDescription}");
Console.WriteLine($"Architecture: {PlatformInfo.ProcessArchitecture}");

// Check for specific platforms
if (PlatformInfo.IsRaspberryPi)
{
    Console.WriteLine("Running on Raspberry Pi!");
}
```

### Raspberry Pi Detection

The library attempts to detect Raspberry Pi hardware by checking:
1. `/proc/device-tree/model` for "Raspberry Pi" string
2. `/proc/cpuinfo` for BCM processor indicators

This detection is best-effort and may not work in all environments (e.g., containers).

## Audio Configuration

StreamAudio provides platform-optimized audio configurations through the `AudioConfiguration` class:

### Default Configuration

Automatically selects optimal settings based on the current platform:

```csharp
using StreamAudio.Core.Platform;
using StreamAudio.Core.Playback;

var config = AudioConfiguration.CreateDefault();
var playback = new AudioPlayback(config);
```

**Platform-specific defaults:**
- **Windows/macOS**: 512 frames buffer (low latency)
- **Linux**: 1024 frames buffer (balanced)
- **Raspberry Pi**: 2048 frames buffer (stable)

### Raspberry Pi Configuration

Optimized for embedded ARM systems with larger buffers for stability:

```csharp
var config = AudioConfiguration.CreateForRaspberryPi();
// Buffer: 2048 frames, Low Latency: No
```

### Low Latency Configuration

For real-time applications requiring minimal delay:

```csharp
var config = AudioConfiguration.CreateLowLatency();
// Buffer: 256 frames, Low Latency: Yes
```

### Custom Configuration

Create custom configurations for specific requirements:

```csharp
var config = new AudioConfiguration
{
    Format = AudioFormat.DvdHq,      // 48kHz, 2 channels, 32-bit float
    BufferSizeInFrames = 1024,       // Custom buffer size
    LowLatencyMode = false
};
```

## Device Enumeration

The `AudioDeviceEnumerator` class provides cross-platform device discovery:

### Listing Devices

```csharp
using StreamAudio.Core.Platform;

// Get all playback devices
var playbackDevices = AudioDeviceEnumerator.GetPlaybackDevices();
foreach (var device in playbackDevices)
{
    Console.WriteLine($"Playback: {device.Name} ({device.DeviceType})");
    if (device.IsDefault)
        Console.WriteLine("  ^ This is the default device");
}

// Get all capture devices
var captureDevices = AudioDeviceEnumerator.GetCaptureDevices();
foreach (var device in captureDevices)
{
    Console.WriteLine($"Capture: {device.Name} ({device.DeviceType})");
}

// Get all devices (both playback and capture)
var allDevices = AudioDeviceEnumerator.GetAllDevices();
```

### Finding Default Devices

```csharp
var defaultPlayback = AudioDeviceEnumerator.GetDefaultPlaybackDevice();
if (defaultPlayback != null)
{
    Console.WriteLine($"Default playback: {defaultPlayback.Name}");
}

var defaultCapture = AudioDeviceEnumerator.GetDefaultCaptureDevice();
if (defaultCapture != null)
{
    Console.WriteLine($"Default capture: {defaultCapture.Name}");
}
```

### Device Types

The enumerator automatically categorizes devices:
- **USB** - USB audio devices
- **HDMI** - HDMI audio output
- **Bluetooth** - Bluetooth audio devices
- **Internal** - Built-in audio (speakers, headphones)
- **Raspberry Pi Audio** - Raspberry Pi audio jack
- **System** - System audio services (ALSA, PulseAudio)
- **Audio Device** - Generic audio device

## Platform-Specific Considerations

### Windows

- Uses DirectSound or WASAPI backend via SoundFlow
- Supports all standard Windows audio devices
- Best performance with WASAPI-compatible devices

### Linux

- Uses ALSA or PulseAudio backend via SoundFlow
- May require proper audio permissions (audio group membership)
- Some containerized/headless environments may not have audio devices

### macOS

- Uses CoreAudio backend via SoundFlow
- Full support for macOS audio devices
- Works on both Intel and Apple Silicon

### Raspberry Pi

- Supports built-in audio jack (3.5mm)
- Supports USB audio devices
- Supports HDMI audio output
- Recommended to use larger buffer sizes (2048+ frames) for stability
- May need to configure ALSA settings for optimal performance

**Raspberry Pi Audio Configuration:**

1. Check available audio devices:
   ```bash
   aplay -l
   ```

2. Set default audio device (if needed):
   ```bash
   # For 3.5mm jack
   sudo raspi-config
   # Navigate to: Advanced Options -> Audio -> Force 3.5mm jack
   
   # Or edit ALSA config
   sudo nano /usr/share/alsa/alsa.conf
   ```

3. Adjust buffer settings (if needed):
   ```bash
   # Edit ALSA configuration
   sudo nano /etc/asound.conf
   ```

## Testing Platform Features

Use the `PlatformInfo` tool to check platform capabilities:

```bash
dotnet run --project tools/PlatformInfo/PlatformInfo.csproj
```

This will display:
- Platform detection information
- Recommended audio configurations
- Available audio devices
- Default devices

## Cross-Platform Development

### Best Practices

1. **Use platform detection** when platform-specific code is needed
2. **Use AudioConfiguration.CreateDefault()** for automatic optimization
3. **Test on target platforms** - especially Raspberry Pi if deploying there
4. **Handle device enumeration gracefully** - may return empty in headless environments
5. **Use appropriate buffer sizes** - smaller for low latency, larger for stability

### Example: Platform-Aware Configuration

```csharp
using StreamAudio.Core.Platform;
using StreamAudio.Core.Playback;

AudioConfiguration config;

if (PlatformInfo.IsRaspberryPi)
{
    // Use stable configuration for Raspberry Pi
    config = AudioConfiguration.CreateForRaspberryPi();
}
else if (args.Contains("--low-latency"))
{
    // User requested low latency
    config = AudioConfiguration.CreateLowLatency();
}
else
{
    // Use platform defaults
    config = AudioConfiguration.CreateDefault();
}

using var playback = new AudioPlayback(config);
Console.WriteLine($"Using: {config.GetDescription()}");
```

## Troubleshooting

### No Audio Devices Found

On Linux/Raspberry Pi, ensure:
1. Audio drivers are installed and loaded
2. User has proper permissions (member of `audio` group)
3. Audio services are running (PulseAudio/ALSA)

```bash
# Add user to audio group
sudo usermod -a -G audio $USER

# Check ALSA devices
aplay -l
```

### Audio Glitches/Dropouts

Try increasing buffer size:
```csharp
var config = new AudioConfiguration
{
    BufferSizeInFrames = 2048  // Increase from default
};
```

### High Latency

Try decreasing buffer size (if platform can handle it):
```csharp
var config = AudioConfiguration.CreateLowLatency();
```

## Architecture

StreamAudio uses [SoundFlow](https://github.com/jmbeach/SoundFlow) library (v1.2.1) which provides:
- Cross-platform audio abstraction via MiniAudio backend
- Support for multiple audio backends per platform
- Consistent API across all platforms
- Native performance through P/Invoke

The MiniAudio backend is:
- Cross-platform (Windows, Linux, macOS, BSD, etc.)
- Low-level audio library in C
- Supports multiple audio APIs per platform
- Optimized for real-time audio

This architecture ensures StreamAudio works consistently across all supported platforms without platform-specific code in the core library.
