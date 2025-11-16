# Troubleshooting Guide

## Overview

This guide provides solutions to common issues encountered when using StreamAudio. Issues are organized by category for easy reference.

## Table of Contents

1. [Audio Playback Issues](#audio-playback-issues)
2. [Device Detection Issues](#device-detection-issues)
3. [Performance Issues](#performance-issues)
4. [Build and Deployment Issues](#build-and-deployment-issues)
5. [Platform-Specific Issues](#platform-specific-issues)
6. [Error Messages](#error-messages)

---

## Audio Playback Issues

### No Audio Output

**Symptoms**: Application runs without errors but no sound is heard.

**Solutions**:

1. **Check Audio Device**
   ```bash
   # List available devices
   dotnet run --project tools/PlatformInfo/PlatformInfo.csproj
   
   # On Linux/Raspberry Pi
   aplay -l
   speaker-test -t wav -c 2
   ```

2. **Verify Volume Levels**
   - Check system volume is not muted
   - On Linux: `alsamixer`
   - On Windows: System tray volume control
   - Check StreamAudio stream volume is not set to 0.0

3. **Test with Simple Example**
   ```bash
   dotnet run --project tools/AudioDemo/AudioDemo.csproj
   ```

4. **Check Default Audio Device**
   ```csharp
   using StreamAudio.Core.Platform;
   
   var devices = AudioDeviceEnumerator.GetPlaybackDevices();
   var defaultDevice = AudioDeviceEnumerator.GetDefaultPlaybackDevice();
   
   Console.WriteLine($"Default: {defaultDevice}");
   foreach (var device in devices)
   {
       Console.WriteLine(device);
   }
   ```

### Audio Stuttering or Crackling

**Symptoms**: Audio plays but has interruptions, pops, or crackles.

**Solutions**:

1. **Increase Buffer Size** (Raspberry Pi)
   ```csharp
   var config = new AudioConfiguration
   {
       Format = AudioFormat.DvdHq,
       BufferSizeInFrames = 4096,  // Larger buffer
       PeriodSizeInFrames = 1024
   };
   using var playback = new AudioPlayback(config);
   ```

2. **Reduce CPU Load**
   - Close other applications
   - Reduce number of concurrent streams
   - Monitor with PerformanceDemo
   - Check CPU temperature on Raspberry Pi: `vcgencmd measure_temp`

3. **Use Raspberry Pi Optimized Settings**
   ```csharp
   var config = AudioConfiguration.CreateForRaspberryPi();
   using var playback = new AudioPlayback(config);
   ```

4. **Check System Resources**
   ```bash
   # Monitor CPU and memory
   htop
   
   # Check for swapping
   free -h
   ```

### Audio Plays Too Fast or Too Slow

**Symptoms**: Audio playback speed is incorrect.

**Solutions**:

1. **Check Sample Rate Mismatch**
   ```csharp
   using StreamAudio.Core.Audio;
   
   var sourceFormat = source.Format;
   var targetFormat = playback.Format;
   
   var validation = SampleRateConverter.ValidateForMixing(sourceFormat, targetFormat);
   if (validation.HasWarnings)
   {
       foreach (var warning in validation.Warnings)
       {
           Console.WriteLine(warning);
       }
   }
   ```

2. **Verify Audio File Format**
   - Ensure audio files are valid and not corrupted
   - Check sample rate matches expected format (44100 or 48000 Hz)

### Looping Doesn't Work

**Symptoms**: Audio doesn't repeat when Loop is set to true.

**Solutions**:

1. **Verify Loop is Set Before Playing**
   ```csharp
   var source = new FileAudioSource("audio.wav");
   source.Loop = true;  // Set BEFORE playing
   manager.AddSource("stream1", source);
   manager.Play("stream1");
   ```

2. **Check for Early Disposal**
   - Ensure source is not disposed while playing
   - Keep FileAudioSource in scope

---

## Device Detection Issues

### No Devices Found

**Symptoms**: `AudioDeviceEnumerator.GetPlaybackDevices()` returns empty list.

**Solutions**:

1. **Check Audio Drivers** (Windows)
   - Update audio drivers
   - Check Device Manager for issues
   - Restart audio services

2. **Check ALSA Configuration** (Linux)
   ```bash
   # Install ALSA utilities if missing
   sudo apt install alsa-utils
   
   # List devices
   aplay -l
   
   # Check configuration
   cat /proc/asound/cards
   ```

3. **Verify Permissions** (Linux)
   ```bash
   # Add user to audio group
   sudo usermod -a -G audio $USER
   
   # Reboot or re-login
   ```

### USB Audio Device Not Recognized

**Symptoms**: USB audio device doesn't appear in device list.

**Solutions**:

1. **Check Physical Connection**
   - Ensure USB device is properly connected
   - Try different USB port
   - Check device with system tools

2. **Linux USB Audio Setup**
   ```bash
   # List USB devices
   lsusb
   
   # Check USB audio modules
   lsmod | grep snd_usb_audio
   
   # Load module if needed
   sudo modprobe snd_usb_audio
   ```

3. **Set as Default Device** (Linux)
   ```bash
   # Create or edit ~/.asoundrc
   cat > ~/.asoundrc << EOF
   defaults.pcm.card 1
   defaults.ctl.card 1
   EOF
   ```

---

## Performance Issues

### High CPU Usage

**Symptoms**: Application uses excessive CPU resources.

**Solutions**:

1. **Monitor Performance**
   ```bash
   dotnet run --project tools/PerformanceDemo/PerformanceDemo.csproj
   ```

2. **Reduce Stream Count**
   - Limit number of concurrent streams
   - Remove unused streams:
   ```csharp
   manager.RemoveSource("streamId", fadeOut: true);
   ```

3. **Optimize Format Conversion**
   ```csharp
   // Use consistent formats to avoid resampling
   var format = AudioFormat.DvdHq;
   var source1 = new FileAudioSource("file1.wav", format);
   var source2 = new FileAudioSource("file2.wav", format);
   ```

4. **Check for Memory Leaks**
   - Ensure all IDisposable objects are disposed
   - Use `using` statements
   - Monitor memory with PerformanceMonitor

### High Memory Usage

**Symptoms**: Application consumes excessive memory.

**Solutions**:

1. **Check for Leaks**
   ```csharp
   using var monitor = new PerformanceMonitor();
   var baseline = monitor.GetMemoryUsageMB();
   
   // Your operations here
   
   var afterOps = monitor.GetMemoryUsageMB();
   Console.WriteLine($"Memory increase: {afterOps - baseline} MB");
   ```

2. **Dispose Resources Properly**
   ```csharp
   // Bad - potential leak
   var source = new FileAudioSource("file.wav");
   manager.AddSource("stream", source);
   manager.RemoveSource("stream");
   // source is still in memory!
   
   // Good - explicit disposal
   using var source = new FileAudioSource("file.wav");
   manager.AddSource("stream", source);
   manager.RemoveSource("stream");
   // source will be disposed
   ```

3. **Reduce Buffer Sizes**
   ```csharp
   var config = AudioConfiguration.CreateLowLatency();
   using var playback = new AudioPlayback(config);
   ```

### Slow Startup

**Symptoms**: Application takes long time to initialize.

**Solutions**:

1. **Pre-warm Audio Engine**
   - AudioEngineManager initializes lazily
   - First access takes time
   - Consider initializing early in startup

2. **Optimize File Loading**
   - Validate files exist before creating sources
   - Load files on demand rather than all at startup

3. **Profile Startup**
   ```csharp
   var sw = Stopwatch.StartNew();
   var engine = AudioEngineManager.Engine;
   Console.WriteLine($"Engine init: {sw.ElapsedMilliseconds}ms");
   
   sw.Restart();
   var playback = new AudioPlayback();
   Console.WriteLine($"Playback init: {sw.ElapsedMilliseconds}ms");
   ```

---

## Build and Deployment Issues

### Build Errors

**Symptoms**: `dotnet build` fails with errors.

**Solutions**:

1. **Clean and Rebuild**
   ```bash
   dotnet clean
   dotnet restore
   dotnet build
   ```

2. **Check .NET Version**
   ```bash
   dotnet --version
   # Should be 8.0 or later
   
   # Install if needed
   # Download from https://dot.net/
   ```

3. **Restore NuGet Packages**
   ```bash
   dotnet restore --force
   dotnet nuget locals all --clear
   dotnet restore
   ```

### Test Failures

**Symptoms**: `dotnet test` shows failing tests.

**Solutions**:

1. **Headless Environment**
   - Audio tests skip in CI environments
   - Set DISPLAY variable if running in Docker/WSL:
   ```bash
   export DISPLAY=:0
   ```

2. **Missing Test Files**
   ```bash
   # Ensure test data exists
   ls -la testdata/
   
   # Generate if needed
   dotnet run --project tools/ToneGenerator/ToneGenerator.csproj -- 100 1 wav testdata/100hz.wav
   ```

3. **Run Specific Tests**
   ```bash
   # Run specific test class
   dotnet test --filter FullyQualifiedName~AudioPlaybackTests
   
   # Run with verbose output
   dotnet test -v detailed
   ```

### Deployment Failures

**Symptoms**: Application doesn't run on target platform.

**Solutions**:

1. **Check Runtime Identifier**
   ```bash
   # Raspberry Pi 64-bit
   dotnet publish -r linux-arm64
   
   # Raspberry Pi 32-bit
   dotnet publish -r linux-arm
   
   # Linux x64
   dotnet publish -r linux-x64
   ```

2. **Include Runtime**
   ```bash
   dotnet publish --self-contained true
   ```

3. **Verify .NET Runtime on Target**
   ```bash
   # On Raspberry Pi
   dotnet --version
   
   # Install if missing
   wget https://dot.net/v1/dotnet-install.sh
   chmod +x dotnet-install.sh
   ./dotnet-install.sh --channel 8.0
   ```

---

## Platform-Specific Issues

### Windows Issues

**Issue**: Application can't find audio devices on Windows.

**Solutions**:
1. Check Windows Audio service is running
2. Update audio drivers
3. Run as Administrator if needed

**Issue**: DLL not found errors.

**Solutions**:
1. Install Visual C++ Redistributable
2. Use self-contained deployment
3. Check antivirus isn't blocking files

### Linux Issues

**Issue**: Permission denied accessing audio.

**Solutions**:
```bash
sudo usermod -a -G audio $USER
# Reboot or logout/login
```

**Issue**: ALSA errors.

**Solutions**:
```bash
# Install ALSA development packages
sudo apt install libasound2-dev

# Configure default device
nano ~/.asoundrc
```

### Raspberry Pi Issues

**Issue**: Crackling audio on Raspberry Pi.

**Solutions**:
1. Use Raspberry Pi optimized configuration
2. Increase buffer sizes
3. Reduce CPU load
4. Check power supply (low voltage causes issues)

**Issue**: USB audio device resets.

**Solutions**:
```bash
# Disable USB power management
echo 'on' | sudo tee /sys/bus/usb/devices/*/power/level

# Make permanent in /etc/rc.local
```

**Issue**: HDMI audio not working.

**Solutions**:
```bash
# Force HDMI audio
sudo raspi-config
# System Options -> Audio -> HDMI

# Or edit /boot/config.txt
sudo nano /boot/config.txt
# Add: hdmi_drive=2
```

---

## Error Messages

### "Audio file not found"

**Error**: `FileNotFoundException: Audio file not found: path/to/file.wav`

**Solutions**:
1. Check file path is correct
2. Use absolute paths
3. Verify file exists:
   ```csharp
   if (!File.Exists(filePath))
   {
       Console.WriteLine($"File not found: {filePath}");
       return;
   }
   ```

### "Stream with ID 'x' already exists"

**Error**: `InvalidOperationException: Stream with ID 'streamId' already exists.`

**Solutions**:
1. Use unique stream IDs
2. Remove stream before re-adding:
   ```csharp
   if (manager.StreamCount > 0)
   {
       manager.RemoveSource("streamId");
   }
   manager.AddSource("streamId", source);
   ```

### "Stream with ID 'x' not found"

**Error**: `InvalidOperationException: Stream with ID 'streamId' not found.`

**Solutions**:
1. Verify stream was added before use
2. Check for typos in stream ID
3. List active streams:
   ```csharp
   Console.WriteLine($"Active streams: {manager.StreamCount}");
   ```

### "Volume must be between 0.0 and 1.0"

**Error**: `ArgumentOutOfRangeException: Volume must be between 0.0 and 1.0.`

**Solutions**:
1. Validate volume values:
   ```csharp
   var volume = Math.Clamp(userVolume, 0.0f, 1.0f);
   manager.BackgroundVolume = volume;
   ```

### "Device initialization failed"

**Error**: SoundFlow device initialization errors.

**Solutions**:
1. Check audio device is available
2. Close other audio applications
3. Restart audio service
4. Try different audio device
5. Check PlatformInfo output:
   ```bash
   dotnet run --project tools/PlatformInfo/PlatformInfo.csproj
   ```

---

## Getting Help

### Diagnostic Information

When reporting issues, include:

1. **Platform Information**
   ```bash
   dotnet run --project tools/PlatformInfo/PlatformInfo.csproj > platform-info.txt
   ```

2. **Performance Metrics**
   ```bash
   dotnet run --project tools/PerformanceDemo/PerformanceDemo.csproj > perf-info.txt
   ```

3. **Build Information**
   ```bash
   dotnet --version
   dotnet --list-runtimes
   dotnet --list-sdks
   ```

4. **Error Stack Trace**
   - Full exception message
   - Stack trace
   - Steps to reproduce

### Debug Mode

Run with detailed logging:

```csharp
// Enable verbose output
Console.WriteLine($"Platform: {PlatformInfo.GetPlatformDescription()}");
Console.WriteLine($"Is Raspberry Pi: {PlatformInfo.IsRaspberryPi()}");

// Monitor performance
using var monitor = new PerformanceMonitor();
var snapshot = monitor.GetSnapshot();
Console.WriteLine(snapshot);
```

### Common Diagnostic Commands

```bash
# System information
uname -a
cat /proc/cpuinfo

# Audio devices
aplay -l
pactl list

# Processes and resources
ps aux | grep dotnet
top -p $(pgrep dotnet)

# Disk space
df -h

# Temperature (Raspberry Pi)
vcgencmd measure_temp

# Network (for remote issues)
ip addr
ping google.com
```

---

## Best Practices for Avoiding Issues

1. **Always dispose resources**
   ```csharp
   using var playback = new AudioPlayback();
   using var manager = new StreamManager(playback);
   ```

2. **Validate inputs**
   ```csharp
   if (!File.Exists(filePath))
       throw new FileNotFoundException(filePath);
   
   if (volume < 0.0f || volume > 1.0f)
       throw new ArgumentOutOfRangeException(nameof(volume));
   ```

3. **Handle errors gracefully**
   ```csharp
   manager.StreamFailed += (sender, args) =>
   {
       Console.WriteLine($"Stream {args.StreamId} failed: {args.Message}");
       // Attempt recovery
       manager.TryRecoverStream(args.StreamId, originalFilePath);
   };
   ```

4. **Monitor performance**
   ```csharp
   using var perfMonitor = new PerformanceMonitor();
   // Check periodically
   ```

5. **Use appropriate configurations**
   ```csharp
   var config = PlatformInfo.IsRaspberryPi() 
       ? AudioConfiguration.CreateForRaspberryPi()
       : AudioConfiguration.CreateDefault();
   ```

---

## Additional Resources

- [README.md](README.md) - Project overview
- [USAGE.md](USAGE.md) - Usage examples
- [PLATFORM.md](PLATFORM.md) - Platform-specific information
- [ARCHITECTURE.md](ARCHITECTURE.md) - System architecture
- [DEPLOYMENT.md](DEPLOYMENT.md) - Deployment guide
- [GitHub Issues](https://github.com/mmackelprang/StreamAudio/issues) - Report bugs
