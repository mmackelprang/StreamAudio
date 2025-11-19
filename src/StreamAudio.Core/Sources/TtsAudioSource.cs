using System.Diagnostics;
using NAudio.Wave;
using SoundFlow.Components;
using SoundFlow.Interfaces;
using SoundFlow.Providers;
using SoundFlow.Structs;
using StreamAudio.Core.Audio;
using Google.Cloud.TextToSpeech.V1;
using Google.Api.Gax.ResourceNames;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace StreamAudio.Core.Sources;

/// <summary>
/// Configuration for TTS audio source.
/// </summary>
public class TtsConfiguration
{
  /// <summary>
  /// TTS engine to use: espeak, google, azure, or piper
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

  /// <summary>
  /// Path to Piper model file (for piper engine, e.g., "en_US-lessac-medium.onnx")
  /// </summary>
  public string? PiperModelPath { get; set; }

  /// <summary>
  /// Path to Piper executable (for piper engine, defaults to "piper")
  /// </summary>
  public string PiperExecutablePath { get; set; } = "piper";
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
  private readonly System.Timers.Timer? loopTimer;
  private int currentPlayCount = 0;

  public TtsAudioSource(string text, AudioFormat? format = null, TtsConfiguration? config = null)
  {
    if (string.IsNullOrWhiteSpace(text))
      throw new ArgumentException("Text cannot be null or empty.", nameof(text));

    this.text = text;
    this.config = config ?? new TtsConfiguration();
    this.format = format ?? AudioFormat.DvdHq;
    
    // Set up a timer to monitor for playback end and loop/repeat if needed
    loopTimer = new System.Timers.Timer(100); // Check every 100ms
    loopTimer.Elapsed += (sender, e) =>
    {
      // Check if looping/repeating is enabled (Loop = true OR RepeatCount > 1 OR RepeatCount = 0 for infinite)
      bool shouldLoop = Loop || RepeatCount > 1 || RepeatCount == 0;
      
      if (shouldLoop && player != null && player.State == SoundFlow.Enums.PlaybackState.Stopped && !disposed)
      {
        // Check if we should repeat based on RepeatCount
        // RepeatCount = 0 means infinite, RepeatCount = 1 means play once (no repeat)
        bool shouldContinue = (RepeatCount == 0) || (currentPlayCount < RepeatCount);
        
        if (shouldContinue)
        {
          // Increment play count when starting a new iteration
          currentPlayCount++;
          
          // Restart playback - need to reinitialize since TTS is typically a one-shot stream
          try
          {
            // Seek to beginning if possible
            if (audioStream != null && audioStream.CanSeek)
            {
              audioStream.Seek(0, SeekOrigin.Begin);
              player?.Play();
            }
            else
            {
              // If stream cannot seek, need to regenerate (rare case)
              InitializePlayer();
              player?.Play();
            }
          }
          catch
          {
            // Ignore errors during loop
          }
        }
        else
        {
          // RepeatCount reached - stop the timer
          loopTimer?.Stop();
        }
      }
    };
    loopTimer.Start();
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
  /// Gets or sets whether the audio should loop.
  /// Note: Looping is handled by monitoring playback state and restarting when finished.
  /// </summary>
  public bool Loop { get; set; }

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
    // Initialize play count to 0 if starting fresh (first play doesn't count as a repeat)
    if (currentPlayCount == 0)
    {
      currentPlayCount = 1;
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
      case "piper":
        return GeneratePiperTtsAudio(text, config);
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
    if (string.IsNullOrWhiteSpace(config.GoogleApiKey))
    {
      throw new InvalidOperationException("Google Cloud TTS requires GoogleApiKey in configuration");
    }

    try
    {
      // Set the API key via environment variable for Google SDK
      Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", config.GoogleApiKey);

      // Create client
      var client = TextToSpeechClient.Create();

      // Build synthesis input
      var input = new SynthesisInput
      {
        Text = text
      };

      // Build voice request
      var voice = new VoiceSelectionParams
      {
        LanguageCode = config.Voice ?? "en-US",
        SsmlGender = SsmlVoiceGender.Neutral
      };

      // Build audio config
      var audioConfig = new Google.Cloud.TextToSpeech.V1.AudioConfig
      {
        AudioEncoding = AudioEncoding.Linear16,
        SampleRateHertz = format.SampleRate,
        SpeakingRate = config.Rate,
        Pitch = config.Pitch,
        VolumeGainDb = ConvertVolumeToDb(config.Volume)
      };

      // Perform TTS request
      var response = client.SynthesizeSpeech(input, voice, audioConfig);

      // Convert audio content to stream
      var memoryStream = new MemoryStream();
      response.AudioContent.WriteTo(memoryStream);
      memoryStream.Position = 0;

      // Convert raw audio to WAV format
      return ConvertRawAudioToWav(memoryStream, format.SampleRate, 1);
    }
    catch (Exception ex)
    {
      throw new InvalidOperationException($"Failed to generate Google Cloud TTS audio: {ex.Message}", ex);
    }
  }

  private Stream GenerateAzureTtsAudio(string text, TtsConfiguration config)
  {
    if (string.IsNullOrWhiteSpace(config.AzureSpeechKey) || string.IsNullOrWhiteSpace(config.AzureSpeechRegion))
    {
      throw new InvalidOperationException("Azure Speech requires AzureSpeechKey and AzureSpeechRegion in configuration");
    }

    try
    {
      // Create speech config
      var speechConfig = SpeechConfig.FromSubscription(config.AzureSpeechKey, config.AzureSpeechRegion);
      
      // Set voice name (default to en-US-JennyNeural if not specified)
      speechConfig.SpeechSynthesisVoiceName = config.Voice ?? "en-US-JennyNeural";
      
      // Set output format to WAV
      speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Riff16Khz16BitMonoPcm);

      // Create a memory stream for output
      var outputStream = new MemoryStream();
      
      // Create audio config to write to stream
      using var audioConfig = Microsoft.CognitiveServices.Speech.Audio.AudioConfig.FromStreamOutput(AudioOutputStream.CreatePullStream());
      
      // Create synthesizer
      using var synthesizer = new SpeechSynthesizer(speechConfig, audioConfig);

      // Build SSML for rate, pitch, and volume control
      var ssml = BuildAzureSsml(text, config);

      // Synthesize speech
      var result = synthesizer.SpeakSsmlAsync(ssml).GetAwaiter().GetResult();

      if (result.Reason == ResultReason.SynthesizingAudioCompleted)
      {
        // Write audio data to memory stream
        outputStream.Write(result.AudioData, 0, result.AudioData.Length);
        outputStream.Position = 0;
        return outputStream;
      }
      else if (result.Reason == ResultReason.Canceled)
      {
        var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
        throw new InvalidOperationException($"Azure TTS synthesis canceled: {cancellation.Reason} - {cancellation.ErrorDetails}");
      }
      else
      {
        throw new InvalidOperationException($"Azure TTS synthesis failed with reason: {result.Reason}");
      }
    }
    catch (Exception ex)
    {
      throw new InvalidOperationException($"Failed to generate Azure Speech audio: {ex.Message}", ex);
    }
  }

  private string BuildAzureSsml(string text, TtsConfiguration config)
  {
    // Build SSML with prosody controls
    var rate = $"{(int)(config.Rate * 100)}%";
    var pitch = config.Pitch >= 0 ? $"+{config.Pitch * 50}%" : $"{config.Pitch * 50}%";
    var volume = $"{(int)(config.Volume * 100)}";

    return $@"
      <speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>
        <voice name='{config.Voice ?? "en-US-JennyNeural"}'>
          <prosody rate='{rate}' pitch='{pitch}' volume='{volume}'>
            {System.Security.SecurityElement.Escape(text)}
          </prosody>
        </voice>
      </speak>";
  }

  private Stream GeneratePiperTtsAudio(string text, TtsConfiguration config)
  {
    if (string.IsNullOrWhiteSpace(config.PiperModelPath))
    {
      throw new InvalidOperationException("Piper TTS requires PiperModelPath in configuration");
    }

    if (!File.Exists(config.PiperModelPath))
    {
      throw new InvalidOperationException($"Piper model file not found: {config.PiperModelPath}");
    }

    try
    {
      // Create temporary WAV file
      var tempFile = Path.GetTempFileName();
      var wavFile = Path.ChangeExtension(tempFile, ".wav");
      File.Delete(tempFile);

      // Create temporary text file for input
      var textFile = Path.GetTempFileName();
      File.WriteAllText(textFile, text);

      try
      {
        // Build piper command
        // piper --model <model> --output_file <output> < <input_text>
        var args = new List<string>
        {
          "--model", $"\"{config.PiperModelPath}\"",
          "--output_file", $"\"{wavFile}\""
        };

        // Add rate control if supported (length scale - inverse of rate)
        if (config.Rate != 1.0)
        {
          var lengthScale = 1.0 / config.Rate;
          args.Add("--length_scale");
          args.Add(lengthScale.ToString("F2"));
        }

        var process = new Process
        {
          StartInfo = new ProcessStartInfo
          {
            FileName = config.PiperExecutablePath,
            Arguments = string.Join(" ", args),
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
          }
        };

        process.Start();
        
        // Send text to stdin
        process.StandardInput.Write(text);
        process.StandardInput.Close();

        // Wait for completion
        process.WaitForExit(30000); // 30 second timeout

        if (process.ExitCode != 0)
        {
          var error = process.StandardError.ReadToEnd();
          throw new InvalidOperationException($"Piper TTS failed with exit code {process.ExitCode}: {error}");
        }

        if (!File.Exists(wavFile))
        {
          throw new InvalidOperationException("Piper TTS did not generate output file");
        }

        // Read the WAV file into a stream
        var fileStream = new FileStream(wavFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        var memoryStream = new MemoryStream();
        fileStream.CopyTo(memoryStream);
        fileStream.Close();
        
        // Clean up temp files
        try 
        { 
          File.Delete(wavFile);
          File.Delete(textFile);
        }
        catch 
        { 
          // Ignore cleanup errors
        }

        memoryStream.Position = 0;
        return memoryStream;
      }
      catch
      {
        // Clean up on error
        try
        {
          if (File.Exists(wavFile)) File.Delete(wavFile);
          if (File.Exists(textFile)) File.Delete(textFile);
        }
        catch
        {
          // Ignore cleanup errors
        }
        throw;
      }
    }
    catch (Exception ex)
    {
      throw new InvalidOperationException($"Failed to generate Piper TTS audio: {ex.Message}", ex);
    }
  }

  private double ConvertVolumeToDb(double volume)
  {
    // Convert 0.0-1.0 volume to decibels (-96.0 to 16.0)
    if (volume <= 0)
      return -96.0;
    if (volume >= 1.0)
      return 0.0;
    
    return 20 * Math.Log10(volume);
  }

  private Stream ConvertRawAudioToWav(Stream rawAudio, int sampleRate, int channels)
  {
    // Read raw audio data
    rawAudio.Position = 0;
    var rawData = new byte[rawAudio.Length];
    rawAudio.Read(rawData, 0, rawData.Length);

    // Create WAV file in memory
    var wavStream = new MemoryStream();
    using var waveFileWriter = new WaveFileWriter(wavStream, new WaveFormat(sampleRate, 16, channels));
    waveFileWriter.Write(rawData, 0, rawData.Length);
    waveFileWriter.Flush();

    wavStream.Position = 0;
    return wavStream;
  }

  public void Dispose()
  {
    if (disposed)
      return;

    disposed = true;
    loopTimer?.Stop();
    loopTimer?.Dispose();
    player?.Stop();
    player?.Dispose();
    dataProvider?.Dispose();
    audioStream?.Dispose();
    GC.SuppressFinalize(this);
  }
}
