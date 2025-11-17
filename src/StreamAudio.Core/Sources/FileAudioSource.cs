using SoundFlow.Components;
using SoundFlow.Interfaces;
using SoundFlow.Providers;
using SoundFlow.Structs;
using StreamAudio.Core.Audio;
using TagLib;

namespace StreamAudio.Core.Sources;

/// <summary>
/// Audio source that reads from audio files using SoundFlow.
/// Supports single file, list of files, or directory of files.
/// Wraps a SoundPlayer for easy file playback with looping support.
/// </summary>
public class FileAudioSource : IAudioSource
{
  private SoundPlayer? player;
  private ISoundDataProvider? dataProvider;
  private FileStream? fileStream;
  private readonly List<string> filePaths;
  private int currentFileIndex = 0;
  private readonly System.Timers.Timer? loopTimer;
  private bool disposed;
  private int repeatCount = 1;
  private int currentPlayCount = 0;
  private SongMetadata? currentlyPlaying;
  private readonly AudioFormat format;
  private readonly bool isDirectory;

  /// <summary>
  /// Creates a FileAudioSource from a single file (Manual source type by default).
  /// </summary>
  public FileAudioSource(string filePath, AudioFormat? format = null, SourceType? sourceType = null)
    : this(new List<string> { filePath }, format, sourceType ?? SourceType.Manual)
  {
  }

  /// <summary>
  /// Creates a FileAudioSource from a list of files (Manual source type by default).
  /// </summary>
  public FileAudioSource(IEnumerable<string> filePaths, AudioFormat? format = null, SourceType? sourceType = null)
    : this(filePaths.ToList(), format, sourceType ?? SourceType.Manual)
  {
  }

  /// <summary>
  /// Creates a FileAudioSource from all audio files in a directory (Manual source type by default).
  /// </summary>
  public static FileAudioSource FromDirectory(string directoryPath, AudioFormat? format = null, SourceType? sourceType = null)
  {
    if (string.IsNullOrWhiteSpace(directoryPath))
      throw new ArgumentException("Directory path cannot be null or empty.", nameof(directoryPath));

    if (!Directory.Exists(directoryPath))
      throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

    // Get all audio files
    var audioExtensions = new[] { ".mp3", ".wav", ".flac", ".ogg", ".m4a", ".wma", ".aac" };
    var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.TopDirectoryOnly)
      .Where(f => audioExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
      .OrderBy(f => f)
      .ToList();

    if (files.Count == 0)
      throw new InvalidOperationException($"No audio files found in directory: {directoryPath}");

    return new FileAudioSource(files, format, sourceType ?? SourceType.Manual, isDirectory: true);
  }

  private FileAudioSource(List<string> filePaths, AudioFormat? format, SourceType sourceType, bool isDirectory = false)
  {
    if (filePaths == null || filePaths.Count == 0)
      throw new ArgumentException("File paths cannot be null or empty.", nameof(filePaths));

    // Validate all files exist
    foreach (var path in filePaths)
    {
      if (string.IsNullOrWhiteSpace(path))
        throw new ArgumentException("File path cannot be null or empty.");

      if (!System.IO.File.Exists(path))
        throw new FileNotFoundException($"Audio file not found: {path}");
    }

    this.filePaths = filePaths;
    this.SourceType = sourceType;
    this.format = format ?? AudioFormat.DvdHq;
    this.isDirectory = isDirectory;

    // Initialize with first file
    InitializeCurrentFile();

    // Set up a timer to check for playback end and loop/advance if needed
    loopTimer = new System.Timers.Timer(100); // Check every 100ms
    loopTimer.Elapsed += (sender, e) =>
    {
      if (Loop && player != null && player.State == SoundFlow.Enums.PlaybackState.Stopped && !disposed)
      {
        // Check if we should repeat based on RepeatCount
        currentPlayCount++;
        if (RepeatCount == 0 || currentPlayCount < RepeatCount)
        {
          // Check if we should advance to next file
          if (filePaths.Count > 1)
          {
            currentFileIndex = (currentFileIndex + 1) % filePaths.Count;
            if (currentFileIndex == 0)
            {
              // Completed all files, check repeat count
              if (RepeatCount != 0 && currentPlayCount >= RepeatCount)
                return; // Done repeating
            }
            InitializeCurrentFile();
          }
          else
          {
            // Single file - seek back to beginning
            try
            {
              fileStream?.Seek(0, SeekOrigin.Begin);
              player?.Play();
            }
            catch
            {
              // Ignore errors during loop
            }
          }
        }
      }
    };
    loopTimer.Start();
  }

  private void InitializeCurrentFile()
  {
    // Clean up previous file resources
    player?.Stop();
    player?.Dispose();
    dataProvider?.Dispose();
    fileStream?.Dispose();

    var currentPath = filePaths[currentFileIndex];

    // Open file stream
    fileStream = new FileStream(currentPath, FileMode.Open, FileAccess.Read);

    // Create data provider from stream
    var engine = AudioEngineManager.Engine;
    dataProvider = new StreamDataProvider(engine, format, fileStream);

    // Create player
    player = new SoundPlayer(engine, format, dataProvider);

    // Extract metadata from file
    ExtractMetadata(currentPath);
  }

  private void ExtractMetadata(string filePath)
  {
    try
    {
      using var file = TagLib.File.Create(filePath);
      currentlyPlaying = new SongMetadata
      {
        Title = file.Tag.Title ?? Path.GetFileNameWithoutExtension(filePath),
        Artist = file.Tag.FirstPerformer,
        Album = file.Tag.Album,
        Genre = file.Tag.FirstGenre,
        Duration = file.Properties.Duration
      };

      // Add file-specific info
      currentlyPlaying.AdditionalInfo["FilePath"] = filePath;
      currentlyPlaying.AdditionalInfo["FileName"] = Path.GetFileName(filePath);
      currentlyPlaying.AdditionalInfo["BitRate"] = file.Properties.AudioBitrate.ToString();
      currentlyPlaying.AdditionalInfo["SampleRate"] = file.Properties.AudioSampleRate.ToString();
      currentlyPlaying.AdditionalInfo["Channels"] = file.Properties.AudioChannels.ToString();
    }
    catch
    {
      // If metadata extraction fails, use filename
      currentlyPlaying = new SongMetadata
      {
        Title = Path.GetFileNameWithoutExtension(filePath)
      };
      currentlyPlaying.AdditionalInfo["FilePath"] = filePath;
      currentlyPlaying.AdditionalInfo["FileName"] = Path.GetFileName(filePath);
    }
  }

  /// <summary>
  /// Gets the file name.
  /// </summary>
  public string Name => filePaths.Count == 1 
    ? Path.GetFileName(filePaths[0]) 
    : isDirectory ? $"Directory ({filePaths.Count} files)" : $"Playlist ({filePaths.Count} files)";

  /// <summary>
  /// Gets the audio format.
  /// </summary>
  public AudioFormat Format => player?.Format ?? format;

  /// <summary>
  /// Gets the sample rate.
  /// </summary>
  public int SampleRate => player?.Format.SampleRate ?? format.SampleRate;

  /// <summary>
  /// Gets the number of channels.
  /// </summary>
  public int Channels => player?.Format.Channels ?? format.Channels;

  /// <summary>
  /// Gets the type of this source (Manual or Auto).
  /// </summary>
  public SourceType SourceType { get; }

  /// <summary>
  /// Gets or sets the number of times to repeat this source.
  /// 0 means infinite loop (only applies to Auto sources).
  /// Default is 1 (play once).
  /// </summary>
  public int RepeatCount
  {
    get => repeatCount;
    set
    {
      if (value < 0)
        throw new ArgumentOutOfRangeException(nameof(value), "RepeatCount must be non-negative.");
      repeatCount = value;
      currentPlayCount = 0; // Reset the play count when setting new repeat count
    }
  }

  /// <summary>
  /// Gets or sets whether the audio should loop.
  /// Note: Looping is handled by monitoring playback state and restarting when finished.
  /// </summary>
  public bool Loop { get; set; }

  /// <summary>
  /// Gets metadata for the currently playing file, if available.
  /// For file sources, this will be null initially but can be populated
  /// with file metadata extraction in the future.
  /// </summary>
  public SongMetadata? CurrentlyPlaying => currentlyPlaying;

  /// <summary>
  /// Gets the underlying SoundPlayer for advanced operations.
  /// </summary>
  public SoundPlayer Player => player ?? throw new InvalidOperationException("Player not initialized");

  /// <summary>
  /// Plays the audio.
  /// </summary>
  public void Play()
  {
    currentPlayCount = 0; // Reset count when explicitly played
    player?.Play();
  }

  /// <summary>
  /// Pauses the audio.
  /// </summary>
  public void Pause() => player?.Pause();

  /// <summary>
  /// Stops the audio.
  /// </summary>
  public void Stop() => player?.Stop();

  /// <summary>
  /// Gets the current playback state.
  /// </summary>
  public SoundFlow.Enums.PlaybackState State => player?.State ?? SoundFlow.Enums.PlaybackState.Stopped;

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
    fileStream?.Dispose();
    GC.SuppressFinalize(this);
  }
}
