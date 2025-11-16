# Deployment Guide for Raspberry Pi

## Overview

This guide provides step-by-step instructions for deploying StreamAudio applications to Raspberry Pi devices. The deployment process includes building, packaging, transferring, and running the application on Raspberry Pi hardware.

## Prerequisites

### Development Machine (Windows/Linux/macOS)
- .NET 8.0 SDK or later
- Git (for cloning the repository)
- SSH client (for remote deployment)
- Optional: Visual Studio 2022 or VS Code

### Raspberry Pi Target
- Raspberry Pi 4 or 5 (recommended)
- Raspberry Pi OS (64-bit recommended) or Ubuntu 24.04 for ARM
- Minimum 2GB RAM (4GB+ recommended)
- Audio output device (USB audio, HDMI, or 3.5mm jack)
- Network connection (WiFi or Ethernet)
- SSH enabled

## Initial Raspberry Pi Setup

### 1. Install Operating System

Using Raspberry Pi Imager:
1. Download and install [Raspberry Pi Imager](https://www.raspberrypi.com/software/)
2. Select **Raspberry Pi OS Lite (64-bit)** for headless operation
3. Configure hostname, SSH, and WiFi in advanced options
4. Write to SD card

### 2. Install .NET Runtime on Raspberry Pi

SSH into your Raspberry Pi:
```bash
ssh pi@raspberrypi.local
```

Install .NET 8 Runtime:
```bash
# Update system
sudo apt update
sudo apt upgrade -y

# Install dependencies
sudo apt install -y curl wget apt-transport-https

# Download and install .NET 8 Runtime
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0 --runtime dotnet

# Add .NET to PATH
echo 'export PATH=$PATH:$HOME/.dotnet' >> ~/.bashrc
source ~/.bashrc

# Verify installation
dotnet --version
```

### 3. Install Audio Dependencies

```bash
# Install ALSA and audio tools
sudo apt install -y alsa-utils pulseaudio

# Test audio output (optional)
speaker-test -t wav -c 2
```

### 4. Configure Audio Output

List available audio devices:
```bash
aplay -l
```

Set default audio output (if needed):
```bash
# For HDMI output
sudo raspi-config
# Navigate to: System Options > Audio > HDMI

# For USB audio
# Configure in /etc/asound.conf or ~/.asoundrc
```

## Deployment Methods

### Method 1: Self-Contained Deployment (Recommended)

This method includes the .NET runtime with your application, making deployment simpler.

#### Build on Development Machine

```bash
# Navigate to your project directory
cd /path/to/StreamAudio

# Publish for Raspberry Pi (linux-arm64)
dotnet publish tools/AudioDemo/AudioDemo.csproj \
  -c Release \
  -r linux-arm64 \
  --self-contained true \
  -p:PublishSingleFile=false \
  -o ./publish/AudioDemo
```

For Raspberry Pi 4/5 with 64-bit OS, use `linux-arm64`.  
For older 32-bit systems, use `linux-arm`.

#### Transfer to Raspberry Pi

Using SCP:
```bash
# Transfer files
scp -r ./publish/AudioDemo pi@raspberrypi.local:~/StreamAudio/

# Also transfer test data
scp -r ./testdata pi@raspberrypi.local:~/StreamAudio/
```

#### Run on Raspberry Pi

```bash
ssh pi@raspberrypi.local

cd ~/StreamAudio/AudioDemo
chmod +x AudioDemo
./AudioDemo
```

### Method 2: Framework-Dependent Deployment

This method requires .NET runtime on the Raspberry Pi but produces smaller deployment packages.

#### Build on Development Machine

```bash
dotnet publish tools/AudioDemo/AudioDemo.csproj \
  -c Release \
  -r linux-arm64 \
  --self-contained false \
  -o ./publish/AudioDemo
```

#### Transfer and Run

```bash
# Transfer
scp -r ./publish/AudioDemo pi@raspberrypi.local:~/StreamAudio/

# Run
ssh pi@raspberrypi.local
cd ~/StreamAudio/AudioDemo
dotnet AudioDemo.dll
```

### Method 3: Deploy from Source

Clone and build directly on Raspberry Pi (slower but simpler for development).

```bash
# On Raspberry Pi
git clone https://github.com/mmackelprang/StreamAudio.git
cd StreamAudio

# Build
dotnet build -c Release

# Run
dotnet run --project tools/AudioDemo/AudioDemo.csproj
```

## Deploying Individual Tools

### AudioDemo
Basic playback and mixing demonstration:
```bash
dotnet publish tools/AudioDemo/AudioDemo.csproj \
  -c Release -r linux-arm64 --self-contained true \
  -o ./publish/AudioDemo
```

### StreamDemo
Advanced stream management demonstration:
```bash
dotnet publish tools/StreamDemo/StreamDemo.csproj \
  -c Release -r linux-arm64 --self-contained true \
  -o ./publish/StreamDemo
```

### PlatformInfo
System information and device enumeration:
```bash
dotnet publish tools/PlatformInfo/PlatformInfo.csproj \
  -c Release -r linux-arm64 --self-contained true \
  -o ./publish/PlatformInfo
```

### PerformanceDemo
Performance monitoring demonstration:
```bash
dotnet publish tools/PerformanceDemo/PerformanceDemo.csproj \
  -c Release -r linux-arm64 --self-contained true \
  -o ./publish/PerformanceDemo
```

### ToneGenerator
Generate test audio files:
```bash
dotnet publish tools/ToneGenerator/ToneGenerator.csproj \
  -c Release -r linux-arm64 --self-contained true \
  -o ./publish/ToneGenerator
```

## Automated Deployment Script

Create a deployment script on your development machine:

```bash
#!/bin/bash
# deploy-to-pi.sh

PI_HOST="pi@raspberrypi.local"
PI_PATH="~/StreamAudio"

echo "Building for Raspberry Pi..."
dotnet publish tools/AudioDemo/AudioDemo.csproj \
  -c Release -r linux-arm64 --self-contained true \
  -o ./publish/AudioDemo

echo "Transferring files..."
ssh $PI_HOST "mkdir -p $PI_PATH"
scp -r ./publish/AudioDemo $PI_HOST:$PI_PATH/
scp -r ./testdata $PI_HOST:$PI_PATH/

echo "Setting permissions..."
ssh $PI_HOST "chmod +x $PI_PATH/AudioDemo/AudioDemo"

echo "Deployment complete!"
echo "Run on Pi: ssh $PI_HOST 'cd $PI_PATH/AudioDemo && ./AudioDemo'"
```

Make it executable and use:
```bash
chmod +x deploy-to-pi.sh
./deploy-to-pi.sh
```

## Running as a Service

To run StreamAudio applications as a systemd service on Raspberry Pi:

### 1. Create Service File

```bash
sudo nano /etc/systemd/system/streamaudio.service
```

Add the following content:
```ini
[Unit]
Description=StreamAudio Application
After=network.target sound.target

[Service]
Type=simple
User=pi
WorkingDirectory=/home/pi/StreamAudio/AudioDemo
ExecStart=/home/pi/StreamAudio/AudioDemo/AudioDemo
Restart=on-failure
RestartSec=10
StandardOutput=journal
StandardError=journal

[Install]
WantedBy=multi-user.target
```

### 2. Enable and Start Service

```bash
# Reload systemd
sudo systemctl daemon-reload

# Enable service to start on boot
sudo systemctl enable streamaudio

# Start service now
sudo systemctl start streamaudio

# Check status
sudo systemctl status streamaudio

# View logs
sudo journalctl -u streamaudio -f
```

## Performance Optimization

### Audio Configuration for Raspberry Pi

Use the Raspberry Pi optimized configuration in your code:

```csharp
using StreamAudio.Core.Platform;
using StreamAudio.Core.Playback;

// Use Raspberry Pi optimized settings
var config = AudioConfiguration.CreateForRaspberryPi();
using var playback = new AudioPlayback(config);
```

### System Tweaks

Improve audio performance on Raspberry Pi:

```bash
# Increase audio priority
sudo nano /etc/security/limits.conf
```

Add:
```
@audio - rtprio 95
@audio - memlock unlimited
```

Enable real-time scheduling:
```bash
sudo usermod -a -G audio pi
```

Reboot to apply changes:
```bash
sudo reboot
```

## Troubleshooting

### No Audio Output

Check audio device:
```bash
# List playback devices
aplay -l

# Test audio
speaker-test -t wav -c 2

# Check ALSA mixer
alsamixer
```

### Permission Denied Errors

```bash
# Add user to audio group
sudo usermod -a -G audio $USER

# Reboot to apply
sudo reboot
```

### High CPU Usage

- Use Raspberry Pi optimized configuration (larger buffers)
- Reduce number of concurrent streams
- Use lower sample rates if quality permits
- Monitor with PerformanceDemo tool

### Audio Stuttering or Dropouts

Increase buffer sizes in `AudioConfiguration`:
```csharp
var config = new AudioConfiguration
{
    Format = AudioFormat.DvdHq,
    BufferSizeInFrames = 4096,  // Increase this
    PeriodSizeInFrames = 1024   // And this
};
```

### Library Not Found Errors

Ensure all dependencies are installed:
```bash
sudo apt install -y libasound2-dev libpulse-dev
```

### Network Deployment Issues

Check SSH connection:
```bash
ssh pi@raspberrypi.local

# If hostname doesn't work, use IP address:
ssh pi@192.168.1.xxx
```

## File Structure on Raspberry Pi

After deployment, your directory structure should look like:

```
/home/pi/StreamAudio/
├── AudioDemo/
│   ├── AudioDemo           (executable)
│   ├── AudioDemo.dll
│   ├── StreamAudio.Core.dll
│   ├── SoundFlow.dll
│   └── ... (other dependencies)
├── testdata/
│   ├── 50hz.wav
│   ├── 100hz.wav
│   └── 200hz.wav
└── logs/                   (optional, for logging)
```

## Monitoring Deployment

### Check Resource Usage

```bash
# CPU and memory
htop

# Disk usage
df -h

# Running processes
ps aux | grep AudioDemo

# System temperature
vcgencmd measure_temp
```

### Performance Testing

Run the PerformanceDemo tool:
```bash
cd ~/StreamAudio/PerformanceDemo
./PerformanceDemo
```

Monitor output for CPU and memory usage during playback.

## Multiple Device Deployment

To deploy to multiple Raspberry Pi devices:

```bash
#!/bin/bash
# deploy-to-multiple-pi.sh

DEVICES=(
    "pi@raspberrypi1.local"
    "pi@raspberrypi2.local"
    "pi@raspberrypi3.local"
)

for DEVICE in "${DEVICES[@]}"; do
    echo "Deploying to $DEVICE..."
    
    # Build once, deploy to all
    if [ ! -d "./publish/AudioDemo" ]; then
        dotnet publish tools/AudioDemo/AudioDemo.csproj \
          -c Release -r linux-arm64 --self-contained true \
          -o ./publish/AudioDemo
    fi
    
    # Deploy
    ssh $DEVICE "mkdir -p ~/StreamAudio"
    scp -r ./publish/AudioDemo $DEVICE:~/StreamAudio/
    ssh $DEVICE "chmod +x ~/StreamAudio/AudioDemo/AudioDemo"
    
    echo "Deployed to $DEVICE successfully!"
done
```

## Updates and Maintenance

### Updating Application

```bash
# On development machine
git pull
dotnet publish -c Release -r linux-arm64 --self-contained true -o ./publish/AudioDemo

# Deploy update
scp -r ./publish/AudioDemo pi@raspberrypi.local:~/StreamAudio/

# On Raspberry Pi (if running as service)
sudo systemctl restart streamaudio
```

### Backup Configuration

```bash
# Backup on Raspberry Pi
tar -czf streamaudio-backup-$(date +%Y%m%d).tar.gz ~/StreamAudio
```

### Remote Monitoring

Set up remote logging:
```bash
# On development machine, monitor logs
ssh pi@raspberrypi.local "sudo journalctl -u streamaudio -f"
```

## Security Considerations

### Network Security
- Change default passwords
- Use SSH keys instead of passwords
- Configure firewall if needed:
  ```bash
  sudo apt install ufw
  sudo ufw allow ssh
  sudo ufw enable
  ```

### Application Security
- Run as non-root user (default pi user is fine)
- Limit file permissions:
  ```bash
  chmod 755 ~/StreamAudio/AudioDemo/AudioDemo
  chmod 644 ~/StreamAudio/AudioDemo/*.dll
  ```

## Next Steps

After successful deployment:

1. Test all audio outputs
2. Verify performance with PerformanceDemo
3. Set up monitoring and logging
4. Configure service for automatic startup
5. Document any device-specific configurations
6. Create backup procedures

For advanced usage and integration, see [USAGE.md](USAGE.md) and [ARCHITECTURE.md](ARCHITECTURE.md).
