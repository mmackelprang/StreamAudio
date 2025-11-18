using StreamAudio.Core;
using StreamAudio.Core.Sources;
using StreamAudio.Core.Playback;
using StreamAudio.Core.Platform;
using StreamAudio.Core.Audio;
using Spectre.Console;
using GoogleCast;

namespace AudioSourceTester;

class Program
{
  private static readonly List<string> logMessages = new();
  private static AudioPlayback? audioPlayback;
  private static ChromeCastAudioPlayback? chromeCastPlayback;
  private static readonly List<IAudioSource> audioSources = new();
  private static bool isPlaying = false;
  private static bool isPaused = false;

  static async Task<int> Main(string[] args)
  {
    AnsiConsole.Write(
      new FigletText("Audio Source Tester")
        .Centered()
        .Color(Color.Cyan));

    AnsiConsole.WriteLine();

    // Show initial menu
    await ShowMainMenuAsync();

    // Cleanup
    DisposeAll();

    return 0;
  }

  static async Task ShowMainMenuAsync()
  {
    while (true)
    {
      AnsiConsole.Clear();
      DisplayHeader();
      
      var choice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
          .Title("[cyan]Main Menu[/]")
          .AddChoices(new[]
          {
            "Configure Audio Devices",
            "Add FileAudioSource",
            "Add TtsAudioSource",
            "List Audio Sources",
            "Start Playback",
            "Stop Playback",
            "Pause Playback",
            "Resume Playback",
            "View Logs",
            "Exit"
          }));

      switch (choice)
      {
        case "Configure Audio Devices":
          await ConfigureAudioDevicesAsync();
          break;
        case "Add FileAudioSource":
          await AddFileAudioSourceAsync();
          break;
        case "Add TtsAudioSource":
          await AddTtsAudioSourceAsync();
          break;
        case "List Audio Sources":
          ListAudioSources();
          break;
        case "Start Playback":
          StartPlayback();
          break;
        case "Stop Playback":
          StopPlayback();
          break;
        case "Pause Playback":
          PausePlayback();
          break;
        case "Resume Playback":
          ResumePlayback();
          break;
        case "View Logs":
          ViewLogs();
          break;
        case "Exit":
          return;
      }
    }
  }

  static void DisplayHeader()
  {
    var table = new Table()
      .Border(TableBorder.Rounded)
      .AddColumn("[cyan]Property[/]")
      .AddColumn("[yellow]Value[/]");

    table.AddRow("Status", isPlaying ? (isPaused ? "[yellow]Paused[/]" : "[green]Playing[/]") : "[dim]Stopped[/]");
    table.AddRow("Audio Sources", $"{audioSources.Count}");
    table.AddRow("Output Device", GetOutputDeviceName());

    AnsiConsole.Write(table);
    AnsiConsole.WriteLine();
  }

  static string GetOutputDeviceName()
  {
    if (chromeCastPlayback != null)
      return "[cyan]ChromeCast Device[/]";
    if (audioPlayback != null)
      return "[green]Local Audio Device[/]";
    return "[dim]None[/]";
  }

  static async Task ConfigureAudioDevicesAsync()
  {
    AnsiConsole.Clear();
    AnsiConsole.MarkupLine("[cyan]Configure Audio Devices[/]");
    AnsiConsole.WriteLine();

    var deviceChoice = AnsiConsole.Prompt(
      new SelectionPrompt<string>()
        .Title("Select output device type:")
        .AddChoices(new[] { "Local Audio Device", "ChromeCast Device", "Back" }));

    if (deviceChoice == "Back")
      return;

    // Dispose existing playback
    DisposePlayback();

    if (deviceChoice == "ChromeCast Device")
    {
      await ConfigureChromeCastAsync();
    }
    else
    {
      audioPlayback = new AudioPlayback();
      LogMessage("[green]Local audio device configured[/]");
    }

    AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
    Console.ReadKey(true);
  }

  static async Task ConfigureChromeCastAsync()
  {
    await AnsiConsole.Status()
      .StartAsync("Discovering ChromeCast devices...", async ctx =>
      {
        try
        {
          var deviceLocator = new DeviceLocator();
          var devices = await deviceLocator.FindReceiversAsync();

          if (!devices.Any())
          {
            LogMessage("[red]No ChromeCast devices found[/]");
            return;
          }

          ctx.Status("Devices found");

          var deviceNames = devices.Select(d => d.FriendlyName).ToList();
          deviceNames.Add("Cancel");

          var selectedName = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
              .Title("Select ChromeCast device:")
              .AddChoices(deviceNames));

          if (selectedName == "Cancel")
            return;

          var selectedDevice = devices.First(d => d.FriendlyName == selectedName);
          
          ctx.Status($"Connecting to {selectedName}...");
          chromeCastPlayback = new ChromeCastAudioPlayback(selectedDevice.FriendlyName, selectedDevice.Id);
          
          var connected = await chromeCastPlayback.WaitForConnectionAsync(TimeSpan.FromSeconds(10));
          if (connected)
          {
            LogMessage($"[green]Connected to ChromeCast: {selectedName}[/]");
          }
          else
          {
            LogMessage($"[red]Failed to connect to ChromeCast: {selectedName}[/]");
            chromeCastPlayback.Dispose();
            chromeCastPlayback = null;
          }
        }
        catch (Exception ex)
        {
          LogMessage($"[red]Error: {ex.Message}[/]");
        }
      });
  }

  static async Task AddFileAudioSourceAsync()
  {
    AnsiConsole.Clear();
    AnsiConsole.MarkupLine("[cyan]Add FileAudioSource[/]");
    AnsiConsole.WriteLine();

    var name = AnsiConsole.Ask<string>("Enter source [cyan]name[/]:");
    
    var pathType = AnsiConsole.Prompt(
      new SelectionPrompt<string>()
        .Title("Select path type:")
        .AddChoices(new[] { "Single File", "Directory" }));

    var path = AnsiConsole.Ask<string>($"Enter {(pathType == "Single File" ? "file" : "directory")} [cyan]path[/]:");

    var repeatCount = AnsiConsole.Ask("Enter [cyan]repeat count[/] (0 for infinite):", 1);

    var sourceType = AnsiConsole.Prompt(
      new SelectionPrompt<SourceType>()
        .Title("Select source type:")
        .AddChoices(SourceType.Manual, SourceType.Auto));

    try
    {
      IAudioSource source;
      if (pathType == "Directory")
      {
        source = FileAudioSource.FromDirectory(path, sourceType: sourceType);
      }
      else
      {
        source = new FileAudioSource(path, sourceType: sourceType);
      }

      source.RepeatCount = repeatCount;
      
      // Override the name if we can (using reflection or just track separately)
      audioSources.Add(source);
      
      LogMessage($"[green]Added FileAudioSource: {name}[/]");
      LogMessage($"  Path: {path}");
      LogMessage($"  Repeat: {repeatCount}");
      LogMessage($"  Type: {sourceType}");
    }
    catch (Exception ex)
    {
      LogMessage($"[red]Error creating FileAudioSource: {ex.Message}[/]");
    }

    AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
    Console.ReadKey(true);
  }

  static async Task AddTtsAudioSourceAsync()
  {
    AnsiConsole.Clear();
    AnsiConsole.MarkupLine("[cyan]Add TtsAudioSource[/]");
    AnsiConsole.WriteLine();

    var name = AnsiConsole.Ask<string>("Enter source [cyan]name[/]:");
    var text = AnsiConsole.Ask<string>("Enter [cyan]TTS text[/]:");

    var repeatCount = AnsiConsole.Ask("Enter [cyan]repeat count[/] (0 for infinite):", 1);

    try
    {
      var source = new TtsAudioSource(text);
      source.RepeatCount = repeatCount;
      
      audioSources.Add(source);
      
      LogMessage($"[green]Added TtsAudioSource: {name}[/]");
      LogMessage($"  Text: {text}");
      LogMessage($"  Repeat: {repeatCount}");
      LogMessage($"  Type: {source.SourceType}");
      
      // Wait for TTS to be ready
      await Task.Delay(100);
    }
    catch (Exception ex)
    {
      LogMessage($"[red]Error creating TtsAudioSource: {ex.Message}[/]");
    }

    AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
    Console.ReadKey(true);
  }

  static void ListAudioSources()
  {
    AnsiConsole.Clear();
    AnsiConsole.MarkupLine("[cyan]Audio Sources[/]");
    AnsiConsole.WriteLine();

    if (audioSources.Count == 0)
    {
      AnsiConsole.MarkupLine("[dim]No audio sources added[/]");
    }
    else
    {
      var table = new Table()
        .Border(TableBorder.Rounded)
        .AddColumn("#")
        .AddColumn("Name")
        .AddColumn("Type")
        .AddColumn("Repeat")
        .AddColumn("State");

      for (int i = 0; i < audioSources.Count; i++)
      {
        var source = audioSources[i];
        var typeName = source is FileAudioSource ? "File" : "TTS";
        table.AddRow(
          (i + 1).ToString(),
          source.Name,
          typeName,
          source.RepeatCount == 0 ? "∞" : source.RepeatCount.ToString(),
          source.State.ToString()
        );
      }

      AnsiConsole.Write(table);
    }

    AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
    Console.ReadKey(true);
  }

  static void StartPlayback()
  {
    if (audioSources.Count == 0)
    {
      LogMessage("[yellow]No audio sources to play[/]");
      AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
      Console.ReadKey(true);
      return;
    }

    if (audioPlayback == null && chromeCastPlayback == null)
    {
      LogMessage("[yellow]No output device configured. Please configure audio devices first.[/]");
      AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
      Console.ReadKey(true);
      return;
    }

    try
    {
      if (audioPlayback != null)
      {
        // Add all sources to the playback
        foreach (var source in audioSources)
        {
          audioPlayback.AddPlayer(source.Player);
        }

        // Start playing all sources
        foreach (var source in audioSources)
        {
          source.Play();
        }

        isPlaying = true;
        isPaused = false;
        LogMessage("[green]Playback started[/]");
      }
      else if (chromeCastPlayback != null)
      {
        LogMessage("[yellow]ChromeCast playback requires media URL - not yet implemented for audio sources[/]");
      }
    }
    catch (Exception ex)
    {
      LogMessage($"[red]Error starting playback: {ex.Message}[/]");
    }

    AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
    Console.ReadKey(true);
  }

  static void StopPlayback()
  {
    try
    {
      foreach (var source in audioSources)
      {
        source.Stop();
      }

      if (audioPlayback != null)
      {
        foreach (var source in audioSources)
        {
          audioPlayback.RemovePlayer(source.Player);
        }
      }

      isPlaying = false;
      isPaused = false;
      LogMessage("[green]Playback stopped[/]");
    }
    catch (Exception ex)
    {
      LogMessage($"[red]Error stopping playback: {ex.Message}[/]");
    }

    AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
    Console.ReadKey(true);
  }

  static void PausePlayback()
  {
    if (!isPlaying)
    {
      LogMessage("[yellow]No playback to pause[/]");
      AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
      Console.ReadKey(true);
      return;
    }

    try
    {
      foreach (var source in audioSources)
      {
        source.Pause();
      }

      isPaused = true;
      LogMessage("[green]Playback paused[/]");
    }
    catch (Exception ex)
    {
      LogMessage($"[red]Error pausing playback: {ex.Message}[/]");
    }

    AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
    Console.ReadKey(true);
  }

  static void ResumePlayback()
  {
    if (!isPaused)
    {
      LogMessage("[yellow]Playback is not paused[/]");
      AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
      Console.ReadKey(true);
      return;
    }

    try
    {
      foreach (var source in audioSources)
      {
        source.Play();
      }

      isPaused = false;
      LogMessage("[green]Playback resumed[/]");
    }
    catch (Exception ex)
    {
      LogMessage($"[red]Error resuming playback: {ex.Message}[/]");
    }

    AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
    Console.ReadKey(true);
  }

  static void ViewLogs()
  {
    AnsiConsole.Clear();
    
    var panel = new Panel(
      new Rows(logMessages.Count == 0 
        ? new[] { new Markup("[dim]No log messages[/]") }
        : logMessages.TakeLast(50).Select(m => new Markup(m))))
    {
      Header = new PanelHeader(" Logs "),
      Border = BoxBorder.Rounded,
      BorderStyle = new Style(Color.Grey)
    };

    AnsiConsole.Write(panel);
    AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
    Console.ReadKey(true);
  }

  static void LogMessage(string message)
  {
    logMessages.Add($"[dim]{DateTime.Now:HH:mm:ss}[/] {message}");
    
    // Keep only last 100 messages
    if (logMessages.Count > 100)
    {
      logMessages.RemoveAt(0);
    }
  }

  static void DisposePlayback()
  {
    if (isPlaying)
    {
      StopPlayback();
    }

    audioPlayback?.Dispose();
    audioPlayback = null;

    chromeCastPlayback?.Dispose();
    chromeCastPlayback = null;
  }

  static void DisposeAll()
  {
    DisposePlayback();

    foreach (var source in audioSources)
    {
      source.Dispose();
    }
    audioSources.Clear();

    AudioEngineManager.Dispose();
  }
}
