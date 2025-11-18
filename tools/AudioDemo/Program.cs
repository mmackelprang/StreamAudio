using StreamAudio.Core;
using StreamAudio.Core.Sources;
using StreamAudio.Core.Playback;
using StreamAudio.Core.Platform;
using Spectre.Console;

namespace AudioDemo;

class Program
{
  static int Main(string[] args)
  {
    // Display header
    AnsiConsole.Write(
      new FigletText("Audio Demo")
        .Centered()
        .Color(Color.Green));

    AnsiConsole.WriteLine();

    // Show audio devices
    DisplayAudioDevices();

    AnsiConsole.WriteLine();

    // Check if test files exist
    string testDataPath = Path.Combine(".", "testdata");
    string tone100Hz = Path.Combine(testDataPath, "100hz.wav");
    string tone200Hz = Path.Combine(testDataPath, "200hz.wav");

    if (!File.Exists(tone100Hz) || !File.Exists(tone200Hz))
    {
      AnsiConsole.MarkupLine("[red]ERROR: Test files not found in testdata/ directory.[/]");
      AnsiConsole.WriteLine("Please ensure the following files exist:");
      AnsiConsole.MarkupLine($"  [yellow]{tone100Hz}[/]");
      AnsiConsole.MarkupLine($"  [yellow]{tone200Hz}[/]");
      AnsiConsole.WriteLine("\nPlease run the ToneGenerator tool first to create test files:");
      AnsiConsole.MarkupLine("  [cyan]dotnet run --project tools/ToneGenerator/ToneGenerator.csproj -- 100 1 WAV testdata/100hz.wav[/]");
      AnsiConsole.MarkupLine("  [cyan]dotnet run --project tools/ToneGenerator/ToneGenerator.csproj -- 200 1 WAV testdata/200hz.wav[/]");
      return 1;
    }

    // Demo 1
    RunDemo1(tone100Hz);

    AnsiConsole.WriteLine();

    // Demo 2
    RunDemo2(tone100Hz, tone200Hz);

    AnsiConsole.WriteLine();

    // Demo 3
    RunDemo3(tone100Hz, tone200Hz);

    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[green]Demo complete![/]");

    // Cleanup the audio engine
    AudioEngineManager.Dispose();

    return 0;
  }

  static void DisplayAudioDevices()
  {
    var panel = new Panel(GetAudioDevicesMarkup())
    {
      Header = new PanelHeader(" Audio Devices ", Justify.Center),
      Border = BoxBorder.Rounded,
      BorderStyle = new Style(Color.Blue),
      Padding = new Padding(1, 0, 1, 0)
    };

    AnsiConsole.Write(panel);
  }

  static string GetAudioDevicesMarkup()
  {
    try
    {
      var playbackDevices = AudioDeviceEnumerator.GetPlaybackDevices();
      var captureDevices = AudioDeviceEnumerator.GetCaptureDevices();

      var defaultPlayback = playbackDevices.FirstOrDefault(d => d.IsDefault);
      var defaultCapture = captureDevices.FirstOrDefault(d => d.IsDefault);

      var lines = new List<string>();

      if (defaultPlayback != null)
      {
        lines.Add($"[bold]Output Device:[/] [green]{defaultPlayback.Name}[/]");
      }
      else
      {
        lines.Add($"[bold]Output Device:[/] [dim]None[/]");
      }

      if (defaultCapture != null)
      {
        lines.Add($"[bold]Input Device:[/] [green]{defaultCapture.Name}[/]");
      }
      else
      {
        lines.Add($"[bold]Input Device:[/] [dim]None[/]");
      }

      if (playbackDevices.Count > 1)
      {
        lines.Add($"\n[dim]({playbackDevices.Count} output devices available)[/]");
      }
      if (captureDevices.Count > 1)
      {
        lines.Add($"[dim]({captureDevices.Count} input devices available)[/]");
      }

      return string.Join("\n", lines);
    }
    catch
    {
      return "[dim]Audio device information unavailable[/]";
    }
  }

  static void RunDemo1(string tone100Hz)
  {
    var rule = new Rule("[yellow]Demo 1: Playing a single tone (100 Hz)[/]")
    {
      Justification = Justify.Left,
      Style = Style.Parse("yellow")
    };
    AnsiConsole.Write(rule);

    AnsiConsole.Status()
      .Start("Initializing audio...", ctx =>
      {
        try
        {
          using var source = new FileAudioSource(tone100Hz) { Loop = false };
          using var playback = new AudioPlayback();

          playback.AddPlayer(source.Player);
          source.Play();

          ctx.Status("Playing audio...");
          ctx.Spinner(Spinner.Known.Dots);

          AnsiConsole.MarkupLine($"[cyan]Playing {source.Name} at {source.SampleRate} Hz, {source.Channels} channel(s)[/]");
          AnsiConsole.MarkupLine("[dim]Press any key to stop playback...[/]");
          Console.ReadKey(true);

          source.Stop();
          AnsiConsole.MarkupLine("[green]✓ Playback stopped[/]");
        }
        catch (Exception ex)
        {
          AnsiConsole.MarkupLine($"[red]ERROR during playback: {ex.Message}[/]");
          AnsiConsole.MarkupLine("[dim]Note: Audio playback may not work in headless environments.[/]");
        }
      });
  }

  static void RunDemo2(string tone100Hz, string tone200Hz)
  {
    var rule = new Rule("[yellow]Demo 2: Mixing two tones (100 Hz + 200 Hz)[/]")
    {
      Justification = Justify.Left,
      Style = Style.Parse("yellow")
    };
    AnsiConsole.Write(rule);

    AnsiConsole.Status()
      .Start("Initializing audio mixer...", ctx =>
      {
        try
        {
          using var source1 = new FileAudioSource(tone100Hz) { Loop = true };
          using var source2 = new FileAudioSource(tone200Hz) { Loop = true };
          using var playback = new AudioPlayback();

          // Add both sources to the mixer with equal volume
          playback.AddPlayer(source1.Player);
          playback.AddPlayer(source2.Player);
          playback.SetVolume(source1.Player, 0.5f);
          playback.SetVolume(source2.Player, 0.5f);

          source1.Play();
          source2.Play();

          ctx.Status("Playing mixed audio...");
          ctx.Spinner(Spinner.Known.Dots);

          var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Tone")
            .AddColumn("Volume")
            .AddRow("100 Hz", "50%")
            .AddRow("200 Hz", "50%");

          AnsiConsole.Write(table);
          AnsiConsole.MarkupLine("\n[dim]Both tones repeating. Press any key to stop...[/]");
          Console.ReadKey(true);

          source1.Stop();
          source2.Stop();
          AnsiConsole.MarkupLine("[green]✓ Playback stopped[/]");
        }
        catch (Exception ex)
        {
          AnsiConsole.MarkupLine($"[red]ERROR during playback: {ex.Message}[/]");
          AnsiConsole.MarkupLine("[dim]Note: Audio playback may not work in headless environments.[/]");
        }
      });
  }

  static void RunDemo3(string tone100Hz, string tone200Hz)
  {
    var rule = new Rule("[yellow]Demo 3: Primary/Background Volume Control[/]")
    {
      Justification = Justify.Left,
      Style = Style.Parse("yellow")
    };
    AnsiConsole.Write(rule);

    AnsiConsole.Status()
      .Start("Initializing audio mixer...", ctx =>
      {
        try
        {
          using var primarySource = new FileAudioSource(tone100Hz) { Loop = true };
          using var backgroundSource = new FileAudioSource(tone200Hz) { Loop = true };
          using var playback = new AudioPlayback();

          // Add sources - 100Hz at full volume (primary), 200Hz at low volume (background)
          playback.AddPlayer(primarySource.Player);
          playback.AddPlayer(backgroundSource.Player);
          playback.SetVolume(primarySource.Player, 1.0f);   // Primary at 100%
          playback.SetVolume(backgroundSource.Player, 0.2f); // Background at 20%

          primarySource.Play();
          backgroundSource.Play();

          ctx.Status("Playing mixed audio with priority...");
          ctx.Spinner(Spinner.Known.Dots);

          var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Role")
            .AddColumn("Tone")
            .AddColumn("Volume")
            .AddRow("[cyan]Primary[/]", "100 Hz", $"{playback.GetVolume(primarySource.Player) * 100:F0}%")
            .AddRow("[dim]Background[/]", "200 Hz", $"{playback.GetVolume(backgroundSource.Player) * 100:F0}%");

          AnsiConsole.Write(table);
          AnsiConsole.WriteLine("\nYou should hear 100 Hz prominently with 200 Hz quietly in the background.");
          AnsiConsole.MarkupLine("[dim]Press any key to stop playback...[/]");
          Console.ReadKey(true);

          primarySource.Stop();
          backgroundSource.Stop();
          AnsiConsole.MarkupLine("[green]✓ Playback stopped[/]");
        }
        catch (Exception ex)
        {
          AnsiConsole.MarkupLine($"[red]ERROR during playback: {ex.Message}[/]");
          AnsiConsole.MarkupLine("[dim]Note: Audio playback may not work in headless environments.[/]");
        }
      });
  }
}
