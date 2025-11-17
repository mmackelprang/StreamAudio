using StreamAudio.Core;
using StreamAudio.Core.Audio;
using StreamAudio.Core.Playback;
using StreamAudio.Core.Sources;

namespace NewSourceDemo;

/// <summary>
/// Demonstration of new Phase 8 audio sources:
/// - TtsAudioSource (Text-to-Speech)
/// - SpotifyAudioSource (Spotify streaming)
/// - UsbAudioSource (USB device capture)
/// - Enhanced FileAudioSource (single file, list, directory)
/// </summary>
class Program
{
  static async Task Main(string[] args)
  {
    Console.WriteLine("StreamAudio - Phase 8 New Audio Sources Demo");
    Console.WriteLine("=============================================");
    Console.WriteLine();

    // Check if we're in a headless environment
    var isHeadless = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")) &&
                     !OperatingSystem.IsWindows();

    if (isHeadless)
    {
      Console.WriteLine("Running in headless mode - audio playback will be simulated");
      Console.WriteLine();
    }

    bool exit = false;
    while (!exit)
    {
      Console.WriteLine("\nSelect a demo:");
      Console.WriteLine("1. TTS Audio Source Demo");
      Console.WriteLine("2. File Audio Source Demo (Single File)");
      Console.WriteLine("3. File Audio Source Demo (Multiple Files)");
      Console.WriteLine("4. File Audio Source Demo (Directory)");
      Console.WriteLine("5. Spotify Audio Source Demo (requires credentials)");
      Console.WriteLine("6. USB Audio Source Demo");
      Console.WriteLine("0. Exit");
      Console.Write("\nChoice: ");

      var choice = Console.ReadLine();
      Console.WriteLine();

      switch (choice)
      {
        case "1":
          await DemoTtsAudioSource(isHeadless);
          break;
        case "2":
          DemoSingleFileAudioSource(isHeadless);
          break;
        case "3":
          DemoMultipleFilesAudioSource(isHeadless);
          break;
        case "4":
          DemoDirectoryAudioSource(isHeadless);
          break;
        case "5":
          await DemoSpotifyAudioSource();
          break;
        case "6":
          DemoUsbAudioSource(isHeadless);
          break;
        case "0":
          exit = true;
          break;
        default:
          Console.WriteLine("Invalid choice");
          break;
      }
    }

    Console.WriteLine("\nDemo completed!");
  }

  static async Task DemoTtsAudioSource(bool isHeadless)
  {
    Console.WriteLine("=== Text-to-Speech Audio Source Demo ===");
    Console.WriteLine();

    try
    {
      Console.Write("Enter text to speak (or press Enter for default): ");
      var text = Console.ReadLine();
      if (string.IsNullOrWhiteSpace(text))
      {
        text = "Hello from StreamAudio. This is a text to speech demonstration.";
      }

      Console.WriteLine($"\nGenerating TTS for: {text}");

      // Configure TTS (using eSpeak as default)
      var config = new TtsConfiguration
      {
        Engine = "espeak",
        Rate = 1.0,
        Pitch = 0.0,
        Volume = 1.0
      };

      // Create TTS source
      using var ttsSource = new TtsAudioSource(text);

      Console.WriteLine($"Source: {ttsSource.Name}");
      Console.WriteLine($"Type: {ttsSource.SourceType}");
      Console.WriteLine($"Repeat Count: {ttsSource.RepeatCount}");
      Console.WriteLine($"Sample Rate: {ttsSource.SampleRate} Hz");
      Console.WriteLine($"Channels: {ttsSource.Channels}");
      Console.WriteLine();

      if (!isHeadless)
      {
        Console.WriteLine("Playing TTS audio...");
        ttsSource.Play();

        // Wait for playback
        await Task.Delay(5000);

        ttsSource.Stop();
        Console.WriteLine("Playback stopped.");
      }
      else
      {
        Console.WriteLine("(Audio playback simulated in headless mode)");
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error: {ex.Message}");
      Console.WriteLine("Note: TTS requires eSpeak to be installed (apt install espeak on Linux)");
    }
  }

  static void DemoSingleFileAudioSource(bool isHeadless)
  {
    Console.WriteLine("=== Single File Audio Source Demo ===");
    Console.WriteLine();

    // Use test files from testdata directory
    var testFile = "../../../testdata/50hz.wav";
    if (!File.Exists(testFile))
    {
      Console.WriteLine($"Test file not found: {testFile}");
      Console.WriteLine("Please run ToneGenerator to create test files first.");
      return;
    }

    try
    {
      // Create source from single file (defaults to Auto type)
      using var source = new FileAudioSource(testFile);

      Console.WriteLine($"Source: {source.Name}");
      Console.WriteLine($"Type: {source.SourceType} (single file defaults to Auto)");
      Console.WriteLine($"Repeat Count: {source.RepeatCount}");
      Console.WriteLine($"Sample Rate: {source.SampleRate} Hz");
      Console.WriteLine($"Channels: {source.Channels}");

      // Display metadata
      if (source.CurrentlyPlaying != null)
      {
        var metadata = source.CurrentlyPlaying;
        Console.WriteLine($"\nMetadata:");
        Console.WriteLine($"  Title: {metadata.Title ?? "N/A"}");
        Console.WriteLine($"  Artist: {metadata.Artist ?? "N/A"}");
        Console.WriteLine($"  Album: {metadata.Album ?? "N/A"}");
        Console.WriteLine($"  Duration: {metadata.Duration?.ToString(@"mm\:ss") ?? "N/A"}");
        foreach (var kvp in metadata.AdditionalInfo)
        {
          Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
        }
      }

      if (!isHeadless)
      {
        Console.WriteLine("\nPlaying audio for 2 seconds...");
        source.Play();
        Thread.Sleep(2000);
        source.Stop();
        Console.WriteLine("Playback stopped.");
      }
      else
      {
        Console.WriteLine("\n(Audio playback simulated in headless mode)");
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error: {ex.Message}");
    }
  }

  static void DemoMultipleFilesAudioSource(bool isHeadless)
  {
    Console.WriteLine("=== Multiple Files Audio Source Demo ===");
    Console.WriteLine();

    // Use test files from testdata directory
    var testFiles = new[]
    {
      "../../../testdata/50hz.wav",
      "../../../testdata/100hz.wav",
      "../../../testdata/200hz.wav"
    };

    // Check if files exist
    var existingFiles = testFiles.Where(File.Exists).ToList();
    if (existingFiles.Count == 0)
    {
      Console.WriteLine("No test files found. Please run ToneGenerator to create test files first.");
      return;
    }

    try
    {
      // Create source from multiple files (defaults to Manual type)
      using var source = new FileAudioSource(existingFiles);

      Console.WriteLine($"Source: {source.Name}");
      Console.WriteLine($"Type: {source.SourceType} (multiple files default to Manual)");
      Console.WriteLine($"Files: {existingFiles.Count}");
      foreach (var file in existingFiles)
      {
        Console.WriteLine($"  - {Path.GetFileName(file)}");
      }

      if (!isHeadless)
      {
        Console.WriteLine("\nPlaying first file for 2 seconds...");
        source.Play();
        Thread.Sleep(2000);
        source.Stop();
        Console.WriteLine("Playback stopped.");
      }
      else
      {
        Console.WriteLine("\n(Audio playback simulated in headless mode)");
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error: {ex.Message}");
    }
  }

  static void DemoDirectoryAudioSource(bool isHeadless)
  {
    Console.WriteLine("=== Directory Audio Source Demo ===");
    Console.WriteLine();

    var testDir = "../../../testdata";
    if (!Directory.Exists(testDir))
    {
      Console.WriteLine($"Test directory not found: {testDir}");
      return;
    }

    try
    {
      // Create source from directory (defaults to Manual type)
      using var source = FileAudioSource.FromDirectory(testDir);

      Console.WriteLine($"Source: {source.Name}");
      Console.WriteLine($"Type: {source.SourceType} (directory defaults to Manual)");

      if (!isHeadless)
      {
        Console.WriteLine("\nPlaying first file for 2 seconds...");
        source.Play();
        Thread.Sleep(2000);
        source.Stop();
        Console.WriteLine("Playback stopped.");
      }
      else
      {
        Console.WriteLine("\n(Audio playback simulated in headless mode)");
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error: {ex.Message}");
    }
  }

  static async Task DemoSpotifyAudioSource()
  {
    Console.WriteLine("=== Spotify Audio Source Demo ===");
    Console.WriteLine();
    Console.WriteLine("Note: Spotify requires valid credentials to work.");
    Console.WriteLine("Set the following environment variables:");
    Console.WriteLine("  SPOTIFY_CLIENT_ID");
    Console.WriteLine("  SPOTIFY_CLIENT_SECRET (or SPOTIFY_REFRESH_TOKEN for user auth)");
    Console.WriteLine();

    try
    {
      // Check for environment variables
      var clientId = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID");
      var clientSecret = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_SECRET");
      var refreshToken = Environment.GetEnvironmentVariable("SPOTIFY_REFRESH_TOKEN");

      var config = new SpotifyConfiguration
      {
        ClientId = clientId,
        ClientSecret = clientSecret,
        RefreshToken = refreshToken,
        UseSimulation = string.IsNullOrWhiteSpace(clientId)
      };

      if (config.UseSimulation)
      {
        Console.WriteLine("Running in simulation mode (no credentials provided)");
      }

      using var source = new SpotifyAudioSource(config);
      await source.InitializeAsync();

      Console.WriteLine($"Source: {source.Name}");
      Console.WriteLine($"Type: {source.SourceType}");
      Console.WriteLine($"Initialized: Yes");

      // Display current metadata
      if (source.CurrentlyPlaying != null)
      {
        var metadata = source.CurrentlyPlaying;
        Console.WriteLine($"\nCurrently Playing:");
        Console.WriteLine($"  Title: {metadata.Title}");
        Console.WriteLine($"  Artist: {metadata.Artist}");
        Console.WriteLine($"  Album: {metadata.Album}");
        Console.WriteLine($"  Duration: {metadata.Duration?.ToString(@"mm\:ss")}");
        Console.WriteLine($"  Position: {metadata.Position?.ToString(@"mm\:ss")}");
      }
      else
      {
        Console.WriteLine("\nNo track currently playing.");
      }

      Console.WriteLine("\nNote: Spotify uses Connect API for playback.");
      Console.WriteLine("Use Spotify app to control playback, this source provides metadata only.");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error: {ex.Message}");
    }
  }

  static void DemoUsbAudioSource(bool isHeadless)
  {
    Console.WriteLine("=== USB Audio Source Demo ===");
    Console.WriteLine();
    Console.WriteLine("Note: USB audio source captures from a USB audio device.");
    Console.WriteLine();

    try
    {
      var config = new UsbAudioConfiguration
      {
        DeviceNumber = -1, // Use default device
        DeviceName = "Demo USB Device",
        SampleRate = 44100,
        Channels = 2
      };

      using var source = new UsbAudioSource(config);

      Console.WriteLine($"Source: {source.Name}");
      Console.WriteLine($"Type: {source.SourceType}");
      Console.WriteLine($"Device: {config.DeviceNumber} (default)");
      Console.WriteLine($"Sample Rate: {source.SampleRate} Hz");
      Console.WriteLine($"Channels: {source.Channels}");

      if (!isHeadless)
      {
        Console.WriteLine("\nAttempting to start capture...");
        Console.WriteLine("(This will fail if no USB audio device is available)");

        try
        {
          source.Play();
          Console.WriteLine("Capture started. Recording for 2 seconds...");
          Thread.Sleep(2000);
          source.Stop();
          Console.WriteLine("Capture stopped.");
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Capture failed: {ex.Message}");
          Console.WriteLine("This is expected if no USB audio device is connected.");
        }
      }
      else
      {
        Console.WriteLine("\n(Audio capture simulated in headless mode)");
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error: {ex.Message}");
    }
  }
}
