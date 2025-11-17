using System.Diagnostics;
using NAudio.Wave;
using SoundFlow.Components;
using SoundFlow.Interfaces;
using SoundFlow.Providers;
using SoundFlow.Structs;
using StreamAudio.Core.Audio;

namespace StreamAudio.Core.Sources;

/// <summary>
/// Configuration for TTS audio source.
/// </summary>
public class TtsConfiguration
{
  /// <summary>
  /// TTS engine to use: espeak, google, or azure
  /// </summary>
  public string Engine { get; set; } = "espeak";

  /// <summary>
  /// Voice to use (engine-specific)
  /// </summary>
  public string? Voice { get; set; }

  /// <summary>
  /// Speaking rate/speed (0.5 to 2.0, 1.0 is normal)
  /// </summary>
  public double Rate { get; set; } = 1.0;

  /// <summary>
  /// Pitch adjustment (-1.0 to 1.0, 0 is normal)
  /// </summary>
  public double Pitch { get; set; } = 0.0;

  /// <summary>
  /// Volume (0.0 to 1.0, 1.0 is normal)
  /// </summary>
  public double Volume { get; set; } = 1.0;

  /// <summary>
  /// Google Cloud API key (for google engine)
  /// </summary>
  public string? GoogleApiKey { get; set; }

  /// <summary>
  /// Azure Speech key (for azure engine)
  /// </summary>
  public string? AzureSpeechKey { get; set; }

  /// <summary>
  /// Azure Speech region (for azure engine)
  /// </summary>
  public string? AzureSpeechRegion { get; set; }
}

/// <summary>
/// Audio source for text-to-speech output.
/// Uses eSpeak by default, with support for Google Cloud TTS and Azure Speech.
/// </summary>
public class TtsAudioSource : IAudioSource
{
  private readonly string text;
  private readonly TtsConfiguration config;
  private SoundPlayer? player;
  private ISoundDataProvider? dataProvider;
  private Stream? audioStream;
  private bool disposed;
  private readonly AudioFormat format;

  public TtsAudioSource(string text, AudioFormat? format = null, TtsConfiguration? config = null)
  {
    if (string.IsNullOrWhiteSpace(text))
      throw new ArgumentException("Text cannot be null or empty.", nameof(text));

    this.text = text;
    this.config = config ?? new TtsConfiguration();
    this.format = format ?? AudioFormat.DvdHq;
  }

  /// <summary>
  /// Gets the name of the audio source.
  /// </summary>
  public string Name => "Text-to-Speech";

  /// <summary>
  /// Gets the audio format.
  /// </summary>
  public AudioFormat Format => player?.Format ?? format;

  /// <summary>
  /// Gets the sample rate.
  /// </summary>
  public int SampleRate => Format.SampleRate;

  /// <summary>
  /// Gets the number of channels.
  /// </summary>
  public int Channels => Format.Channels;

  /// <summary>
  /// TTS sources are Auto type by default (short announcements).
  /// </summary>
  public SourceType SourceType => SourceType.Auto;

  /// <summary>
  /// TTS sources play once by default.
  /// </summary>
  public int RepeatCount { get; set; } = 1;

  /// <summary>
  /// Gets metadata for TTS (null - TTS doesn't have song metadata).
  /// </summary>
  public SongMetadata? CurrentlyPlaying => null;

  /// <summary>
  /// Gets the underlying SoundPlayer.
  /// </summary>
  public SoundPlayer Player
  {
    get
    {
      if (player == null)
      {
        // Lazy initialization
        InitializePlayer();
      }
      return player!;
    }
  }

  /// <summary>
  /// Gets the current playback state.
  /// </summary>
  public SoundFlow.Enums.PlaybackState State => player?.State ?? SoundFlow.Enums.PlaybackState.Stopped;

  /// <summary>
  /// Plays the TTS audio.
  /// </summary>
  public void Play()
  {
    if (player == null)
    {
      InitializePlayer();
    }
    player!.Play();
  }

  /// <summary>
  /// Pauses the TTS audio.
  /// </summary>
  public void Pause() => player?.Pause();

  /// <summary>
  /// Stops the TTS audio.
  /// </summary>
  public void Stop() => player?.Stop();

  private void InitializePlayer()
  {
    // Generate TTS audio
    audioStream = GenerateTtsAudio(text, config);

    // Create data provider from stream
    var engine = AudioEngineManager.Engine;
    dataProvider = new StreamDataProvider(engine, format, audioStream);

    // Create player
    player = new SoundPlayer(engine, format, dataProvider);
  }

  private Stream GenerateTtsAudio(string text, TtsConfiguration config)
  {
    switch (config.Engine.ToLowerInvariant())
    {
      case "espeak":
        return GenerateESpeakAudio(text, config);
      case "google":
        return GenerateGoogleTtsAudio(text, config);
      case "azure":
        return GenerateAzureTtsAudio(text, config);
      default:
        throw new NotSupportedException($"TTS engine '{config.Engine}' is not supported.");
    }
  }

  private Stream GenerateESpeakAudio(string text, TtsConfiguration config)
  {
    try
    {
      // Create temporary WAV file
      var tempFile = Path.GetTempFileName();
      var wavFile = Path.ChangeExtension(tempFile, ".wav");
      File.Delete(tempFile);

      // Build espeak command
      var args = new List<string>
      {
        $"\"{text}\"",
        "-w", $"\"{wavFile}\""
      };

      if (!string.IsNullOrWhiteSpace(config.Voice))
      {
        args.Add("-v");
        args.Add(config.Voice);
      }

      // Set speed (espeak uses words per minute, default is ~175)
      var speed = (int)(175 * config.Rate);
      args.Add("-s");
      args.Add(speed.ToString());

      // Set pitch (espeak uses 0-99, default is 50)
      var pitch = (int)(50 + (config.Pitch * 50));
      args.Add("-p");
      args.Add(pitch.ToString());

      // Set amplitude (espeak uses 0-200, default is 100)
      var amplitude = (int)(100 * config.Volume);
      args.Add("-a");
      args.Add(amplitude.ToString());

      // Execute espeak
      var startInfo = new ProcessStartInfo
      {
        FileName = "espeak",
        Arguments = string.Join(" ", args),
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
      };

      using var process = Process.Start(startInfo);
      if (process == null)
        throw new InvalidOperationException("Failed to start espeak process");

      process.WaitForExit();

      if (process.ExitCode != 0)
      {
        var error = process.StandardError.ReadToEnd();
        throw new InvalidOperationException($"espeak failed: {error}");
      }

      // Read the WAV file and convert to appropriate format
      if (!File.Exists(wavFile))
        throw new InvalidOperationException("espeak did not generate output file");

      // Read the entire WAV file into memory
      var waveBytes = File.ReadAllBytes(wavFile);
      File.Delete(wavFile);

      return new MemoryStream(waveBytes);
    }
    catch (Exception ex)
    {
      throw new InvalidOperationException($"Failed to generate TTS audio with espeak: {ex.Message}", ex);
    }
  }

  private Stream GenerateGoogleTtsAudio(string text, TtsConfiguration config)
  {
    // TODO: Implement Google Cloud TTS
    // For now, fall back to eSpeak
    if (string.IsNullOrWhiteSpace(config.GoogleApiKey))
    {
      throw new InvalidOperationException("Google Cloud TTS requires GoogleApiKey in configuration");
    }

    // Placeholder - would use Google.Cloud.TextToSpeech.V1
    throw new NotImplementedException("Google Cloud TTS is not yet implemented. Use 'espeak' engine instead.");
  }

  private Stream GenerateAzureTtsAudio(string text, TtsConfiguration config)
  {
    // TODO: Implement Azure Speech
    // For now, fall back to eSpeak
    if (string.IsNullOrWhiteSpace(config.AzureSpeechKey) || string.IsNullOrWhiteSpace(config.AzureSpeechRegion))
    {
      throw new InvalidOperationException("Azure Speech requires AzureSpeechKey and AzureSpeechRegion in configuration");
    }

    // Placeholder - would use Microsoft.CognitiveServices.Speech
    throw new NotImplementedException("Azure Speech is not yet implemented. Use 'espeak' engine instead.");
  }

  public void Dispose()
  {
    if (disposed)
      return;

    disposed = true;
    player?.Stop();
    player?.Dispose();
    dataProvider?.Dispose();
    audioStream?.Dispose();
    GC.SuppressFinalize(this);
  }
}
