using SoundFlow.Abstracts;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Providers;
using SoundFlow.Structs;
using SoundFlow.Interfaces;
using StreamAudio.Core.Interfaces;

namespace StreamAudio.Core.Sources;

/// <summary>
/// Audio source that reads from an audio file (WAV, MP3, etc.) using SoundFlow.
/// </summary>
public class FileAudioSource : IAudioSource
{
  private readonly AudioEngine engine;
  private readonly SoundPlayer player;
  private readonly ISoundDataProvider dataProvider;
  private readonly string filePath;
  private readonly Stream fileStream;
  private bool disposed;

  public FileAudioSource(string filePath)
  {
    if (string.IsNullOrWhiteSpace(filePath))
      throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

    if (!File.Exists(filePath))
      throw new FileNotFoundException($"Audio file not found: {filePath}");

    this.filePath = filePath;
    
    // Create engine and file stream
    engine = new MiniAudioEngine();
    fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
    
    // Determine audio format from file (defaulting to CD quality)
    var format = AudioFormat.CdQuality;
    
    // Create the data provider for the file stream
    dataProvider = new StreamDataProvider(engine, format, fileStream);
    
    // Create a player with the data provider
    player = new SoundPlayer(engine, format, dataProvider);
  }

  public string Name => Path.GetFileName(filePath);

  public int SampleRate => player.Format.SampleRate;

  public int Channels => player.Format.Channels;

  public bool Repeat { get; set; }

  public bool HasEnded { get; private set; }

  public int Read(float[] buffer, int offset, int count)
  {
    if (disposed)
      throw new ObjectDisposedException(nameof(FileAudioSource));

    try
    {
      // Read data from the provider
      byte[] byteBuffer = new byte[count * sizeof(float)];
      int bytesRead = dataProvider.Read(byteBuffer, 0, byteBuffer.Length);
      
      if (bytesRead == 0)
      {
        // End of stream
        if (Repeat)
        {
          // Seek to beginning
          fileStream.Seek(0, SeekOrigin.Begin);
          bytesRead = dataProvider.Read(byteBuffer, 0, byteBuffer.Length);
        }
        else
        {
          HasEnded = true;
          // Fill with silence
          Array.Fill(buffer, 0f, offset, count);
          return count;
        }
      }

      // Convert bytes to floats
      int samplesRead = bytesRead / sizeof(float);
      Buffer.BlockCopy(byteBuffer, 0, buffer, offset * sizeof(float), bytesRead);
      
      // Fill any remaining with silence if we didn't get enough samples
      if (samplesRead < count)
      {
        Array.Fill(buffer, 0f, offset + samplesRead, count - samplesRead);
        if (!Repeat)
        {
          HasEnded = true;
        }
        samplesRead = count;
      }

      return samplesRead;
    }
    catch
    {
      // On error, fill with silence
      Array.Fill(buffer, 0f, offset, count);
      HasEnded = true;
      return count;
    }
  }

  public void Dispose()
  {
    if (disposed)
      return;

    player?.Dispose();
    dataProvider?.Dispose();
    fileStream?.Dispose();
    engine?.Dispose();
    disposed = true;
    GC.SuppressFinalize(this);
  }
}
