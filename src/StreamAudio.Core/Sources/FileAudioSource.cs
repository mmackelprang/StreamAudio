using NAudio.Wave;
using StreamAudio.Core.Interfaces;

namespace StreamAudio.Core.Sources;

/// <summary>
/// Audio source that reads from an audio file (WAV, MP3, etc.).
/// </summary>
public class FileAudioSource : IAudioSource
{
  private readonly AudioFileReader audioFileReader;
  private readonly string filePath;
  private bool disposed;

  public FileAudioSource(string filePath)
  {
    if (string.IsNullOrWhiteSpace(filePath))
      throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

    if (!File.Exists(filePath))
      throw new FileNotFoundException($"Audio file not found: {filePath}");

    this.filePath = filePath;
    audioFileReader = new AudioFileReader(filePath);
  }

  public string Name => Path.GetFileName(filePath);

  public int SampleRate => audioFileReader.WaveFormat.SampleRate;

  public int Channels => audioFileReader.WaveFormat.Channels;

  public bool Repeat { get; set; }

  public bool HasEnded { get; private set; }

  public int Read(float[] buffer, int offset, int count)
  {
    if (disposed)
      throw new ObjectDisposedException(nameof(FileAudioSource));

    int samplesRead = audioFileReader.Read(buffer, offset, count);

    // Handle end of file
    if (samplesRead < count)
    {
      if (Repeat)
      {
        // Rewind to the beginning
        audioFileReader.Position = 0;
        
        // Fill the rest of the buffer
        int additionalSamples = audioFileReader.Read(buffer, offset + samplesRead, count - samplesRead);
        samplesRead += additionalSamples;
      }
      else
      {
        // Mark as ended
        HasEnded = true;
        
        // Fill remaining buffer with silence
        Array.Fill(buffer, 0f, offset + samplesRead, count - samplesRead);
        samplesRead = count;
      }
    }

    return samplesRead;
  }

  public void Dispose()
  {
    if (disposed)
      return;

    audioFileReader?.Dispose();
    disposed = true;
    GC.SuppressFinalize(this);
  }
}
