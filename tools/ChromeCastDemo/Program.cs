using StreamAudio.Core.Playback;
using StreamAudio.Core.Audio;
using StreamAudio.Core.Configuration;
using StreamAudio.Core.Platform;
using GoogleCast;
using Spectre.Console;

namespace ChromeCastDemo;

class Program
{
  static async Task Main(string[] args)
  {
    // Initialize configuration (silently)
    var config = ConfigurationManager.Instance;

    var layout = new Layout("Root")
      .SplitRows(
        new Layout("Header").Size(3),
        new Layout("Body").SplitColumns(
          new Layout("Devices").Ratio(1),
          new Layout("Status").Ratio(1)
        ),
        new Layout("Logs").Size(10)
      );

    // Create panels
    var headerPanel = new Panel(
      new FigletText("ChromeCast Demo")
        .Centered()
        .Color(Color.Blue))
    {
      Border = BoxBorder.Double
    };

    var logMessages = new List<string>();
    var logsPanel = CreateLogsPanel(logMessages);
    var devicesPanel = CreateDevicesPanel();
    var statusPanel = CreateStatusPanel("Initializing...");

    layout["Header"].Update(headerPanel);
    layout["Devices"].Update(devicesPanel);
    layout["Status"].Update(statusPanel);
    layout["Logs"].Update(logsPanel);

    await AnsiConsole.Live(layout)
      .StartAsync(async ctx =>
      {
        AddLog(logMessages, "Starting ChromeCast discovery...", ctx, layout);
        
        try
        {
          var deviceLocator = new DeviceLocator();
          var devices = await deviceLocator.FindReceiversAsync();

          if (!devices.Any())
          {
            AddLog(logMessages, "[red]No ChromeCast devices found[/]", ctx, layout);
            layout["Status"].Update(CreateStatusPanel("No devices found"));
            ctx.Refresh();
            Thread.Sleep(3000);
            return;
          }

          AddLog(logMessages, $"[green]Found {devices.Count()} ChromeCast device(s)[/]", ctx, layout);
          
          var deviceList = devices.ToList();
          layout["Devices"].Update(CreateDevicesPanel(deviceList));
          ctx.Refresh();

          // Prompt for device selection
          var selectedDevice = await SelectDeviceAsync(deviceList);
          if (selectedDevice == null)
          {
            AddLog(logMessages, "[yellow]No device selected[/]", ctx, layout);
            return;
          }

          AddLog(logMessages, $"[cyan]Selected: {selectedDevice.FriendlyName}[/]", ctx, layout);
          layout["Status"].Update(CreateStatusPanel($"Connecting to {selectedDevice.FriendlyName}..."));
          ctx.Refresh();

          using var chromecast = new ChromeCastAudioPlayback(selectedDevice.FriendlyName, selectedDevice.Id);
          
          var connected = await chromecast.WaitForConnectionAsync(TimeSpan.FromSeconds(10));

          if (!connected)
          {
            AddLog(logMessages, "[red]Failed to connect to device[/]", ctx, layout);
            layout["Status"].Update(CreateStatusPanel("Connection failed"));
            ctx.Refresh();
            Thread.Sleep(3000);
            return;
          }

          AddLog(logMessages, "[green]Connected successfully![/]", ctx, layout);
          
          // Show audio devices
          var audioDevicesInfo = GetAudioDevicesInfo();
          layout["Status"].Update(CreateStatusPanel("Connected", selectedDevice.FriendlyName, audioDevicesInfo));
          ctx.Refresh();

          var metadata = new SongMetadata
          {
            Title = "Sample Audio Test",
            Artist = "StreamAudio ChromeCast Demo",
            Album = "Test Album"
          };

          AddLog(logMessages, "Loading sample audio...", ctx, layout);
          
          try
          {
            await chromecast.LoadMediaAsync(
              "http://commondatastorage.googleapis.com/codeskulptor-demos/DDR_assets/Kangaroo_MusiQue_-_The_Neverwritten_Role_Playing_Game.mp3",
              "audio/mp3",
              metadata);

            AddLog(logMessages, "[green]Media loaded and playing![/]", ctx, layout);
            layout["Status"].Update(CreateStatusPanel("Playing", selectedDevice.FriendlyName, audioDevicesInfo));
            ctx.Refresh();

            AddLog(logMessages, "[yellow]Press any key to stop and exit...[/]", ctx, layout);
            Console.ReadKey(true);

            AddLog(logMessages, "Stopping playback...", ctx, layout);
            chromecast.Stop();
            AddLog(logMessages, "[green]Stopped playback[/]", ctx, layout);
          }
          catch (Exception ex)
          {
            AddLog(logMessages, $"[red]Failed to load media: {ex.Message}[/]", ctx, layout);
          }
        }
        catch (Exception ex)
        {
          AddLog(logMessages, $"[red]Error: {ex.Message}[/]", ctx, layout);
        }

        AddLog(logMessages, "[cyan]Demo complete[/]", ctx, layout);
        Thread.Sleep(2000);
      });
  }

  static Panel CreateLogsPanel(List<string> messages)
  {
    var content = messages.Count == 0 
      ? (Spectre.Console.Rendering.IRenderable)new Markup("[dim]Waiting for logs...[/]")
      : (Spectre.Console.Rendering.IRenderable)new Rows(messages.TakeLast(8).Select(m => new Markup(m)));

    return new Panel(content)
    {
      Header = new PanelHeader(" Logs "),
      Border = BoxBorder.Rounded,
      BorderStyle = new Style(Color.Grey)
    };
  }

  static Panel CreateDevicesPanel(List<IReceiver>? devices = null)
  {
    var table = new Table()
      .Border(TableBorder.Rounded)
      .AddColumn("Device Name")
      .AddColumn("Address");

    if (devices != null && devices.Any())
    {
      foreach (var device in devices)
      {
        table.AddRow(
          new Markup($"[cyan]{device.FriendlyName}[/]"),
          new Markup($"[dim]{device.IPEndPoint}[/]")
        );
      }
    }
    else
    {
      table.AddRow(
        new Markup("[dim]No devices found[/]"),
        new Markup("[dim]-[/]")
      );
    }

    return new Panel(table)
    {
      Header = new PanelHeader(" ChromeCast Devices "),
      Border = BoxBorder.Rounded,
      BorderStyle = new Style(Color.Blue)
    };
  }

  static Panel CreateStatusPanel(string status, string? deviceName = null, string? audioInfo = null)
  {
    var grid = new Grid()
      .AddColumn()
      .AddRow($"[bold]Status:[/] {status}");

    if (!string.IsNullOrEmpty(deviceName))
    {
      grid.AddRow($"[bold]Device:[/] [cyan]{deviceName}[/]");
    }

    if (!string.IsNullOrEmpty(audioInfo))
    {
      grid.AddEmptyRow();
      grid.AddRow(new Rule("[yellow]Audio Devices[/]").LeftJustified());
      grid.AddRow(new Markup(audioInfo));
    }

    return new Panel(grid)
    {
      Header = new PanelHeader(" Status "),
      Border = BoxBorder.Rounded,
      BorderStyle = new Style(Color.Green)
    };
  }

  static string GetAudioDevicesInfo()
  {
    var lines = new List<string>();
    
    try
    {
      var playbackDevices = AudioDeviceEnumerator.GetPlaybackDevices();
      var captureDevices = AudioDeviceEnumerator.GetCaptureDevices();

      var defaultPlayback = playbackDevices.FirstOrDefault(d => d.IsDefault);
      var defaultCapture = captureDevices.FirstOrDefault(d => d.IsDefault);

      if (defaultPlayback != null)
      {
        lines.Add($"[bold]Output:[/] [green]{defaultPlayback.Name}[/]");
      }
      else
      {
        lines.Add($"[bold]Output:[/] [dim]None[/]");
      }

      if (defaultCapture != null)
      {
        lines.Add($"[bold]Input:[/] [green]{defaultCapture.Name}[/]");
      }
      else
      {
        lines.Add($"[bold]Input:[/] [dim]None[/]");
      }
    }
    catch
    {
      lines.Add("[dim]Audio device info unavailable[/]");
    }

    return string.Join("\n", lines);
  }

  static void AddLog(List<string> messages, string message, LiveDisplayContext ctx, Layout layout)
  {
    messages.Add($"[dim]{DateTime.Now:HH:mm:ss}[/] {message}");
    layout["Logs"].Update(CreateLogsPanel(messages));
    ctx.Refresh();
  }

  static async Task<IReceiver?> SelectDeviceAsync(List<IReceiver> devices)
  {
    // For now, just select the first device automatically
    // In a real scenario, you might want to add a selection prompt
    await Task.Delay(100);
    return devices.FirstOrDefault();
  }
}
