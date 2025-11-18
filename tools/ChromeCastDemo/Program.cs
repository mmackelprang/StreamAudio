using StreamAudio.Core.Playback;
using StreamAudio.Core.Audio;
using StreamAudio.Core.Configuration;
using GoogleCast;

namespace ChromeCastDemo;

class Program
{
  static async Task Main(string[] args)
  {
    Console.WriteLine("=== StreamAudio ChromeCast Demo ===\n");

    // Initialize configuration
    var config = ConfigurationManager.Instance;
    Console.WriteLine("Configuration initialized.\n");

    try
    {
      // Discover available ChromeCast devices
      Console.WriteLine("Discovering ChromeCast devices on the network...");
      var deviceLocator = new DeviceLocator();
      var devices = await deviceLocator.FindReceiversAsync();

      if (!devices.Any())
      {
        Console.WriteLine("❌ No ChromeCast devices found on the network.");
        Console.WriteLine("\nMake sure:");
        Console.WriteLine("  1. A ChromeCast device is powered on");
        Console.WriteLine("  2. The device is connected to the same network");
        Console.WriteLine("  3. Your firewall allows mDNS/multicast traffic");
        return;
      }

      Console.WriteLine($"\n✓ Found {devices.Count()} device(s):\n");
      var deviceList = devices.ToList();
      for (int i = 0; i < deviceList.Count; i++)
      {
        var device = deviceList[i];
        Console.WriteLine($"  {i + 1}. {device.FriendlyName}");
        Console.WriteLine($"     Address: {device.IPEndPoint}");
        Console.WriteLine();
      }

      // Select a device
      Console.Write("Select device number (or press Enter for first device): ");
      var input = Console.ReadLine();
      int selectedIndex = 0;
      if (!string.IsNullOrWhiteSpace(input) && int.TryParse(input, out int parsed))
      {
        selectedIndex = parsed - 1;
      }

      if (selectedIndex < 0 || selectedIndex >= deviceList.Count)
      {
        Console.WriteLine("❌ Invalid selection.");
        return;
      }

      var selectedDevice = deviceList[selectedIndex];
      Console.WriteLine($"\n✓ Selected: {selectedDevice.FriendlyName}\n");

      // Create ChromeCastAudioPlayback instance
      Console.WriteLine("Connecting to ChromeCast device...");
      using var chromecast = new ChromeCastAudioPlayback(selectedDevice.FriendlyName, selectedDevice.Id);

      // Wait for connection
      await Task.Delay(3000);

      if (!chromecast.IsDeviceHealthy())
      {
        Console.WriteLine("❌ Failed to connect to device.");
        return;
      }

      Console.WriteLine("✓ Connected successfully!\n");

      // Load sample media
      Console.WriteLine("Loading sample audio...");
      Console.WriteLine("(Using a publicly available sample MP3)");

      var metadata = new SongMetadata
      {
        Title = "Sample Audio Test",
        Artist = "StreamAudio ChromeCast Demo",
        Album = "Test Album"
      };

      try
      {
        await chromecast.LoadMediaAsync(
          "http://commondatastorage.googleapis.com/codeskulptor-demos/DDR_assets/Kangaroo_MusiQue_-_The_Neverwritten_Role_Playing_Game.mp3",
          "audio/mp3",
          metadata);

        Console.WriteLine("✓ Media loaded and playing!\n");
        Console.WriteLine("Press any key to stop and exit...");
        Console.ReadKey(true);

        chromecast.Stop();
        Console.WriteLine("\n✓ Stopped playback.");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"❌ Failed to load media: {ex.Message}");
        Console.WriteLine("\nNote: The ChromeCast device needs network access to the media URL.");
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"\n❌ Error: {ex.Message}");
      Console.WriteLine($"\nStack trace:\n{ex.StackTrace}");
    }

    Console.WriteLine("\nDemo complete.");
  }
}
