if (args.Length < 4)
{
  Console.WriteLine("ToneGenerator - Generate audio test files");
  Console.WriteLine("Usage: ToneGenerator <frequency> <duration> <format> <output-file>");
  Console.WriteLine("  frequency    - Sine wave frequency in Hz (e.g., 50, 100, 200)");
  Console.WriteLine("  duration     - Duration in seconds (e.g., 1, 2, 5)");
  Console.WriteLine("  format       - Audio format: WAV or MP3");
  Console.WriteLine("  output-file  - Output filename (extension determines format if format is AUTO)");
  Console.WriteLine();
  Console.WriteLine("Example: ToneGenerator 100 1 WAV 100hz.wav");
  return 1;
}

try
{
  float frequency = float.Parse(args[0]);
  float duration = float.Parse(args[1]);
  string format = args[2].ToUpperInvariant();
  string outputFile = args[3];

  // Auto-detect format from extension if format is AUTO
  if (format == "AUTO")
  {
    string extension = Path.GetExtension(outputFile).ToLowerInvariant();
    format = extension switch
    {
      ".wav" => "WAV",
      ".mp3" => "MP3",
      _ => throw new ArgumentException($"Cannot auto-detect format from extension '{extension}'. Supported: .wav, .mp3")
    };
  }

  Console.WriteLine($"Generating {frequency} Hz sine wave, {duration}s duration, format: {format}");
  
  // Create the sine wave generator
  int sampleRate = 44100;
  int channels = 1; // Mono
  var sineWave = new SineWaveProvider(frequency, sampleRate);

  // Calculate total samples needed
  int totalSamples = (int)(sampleRate * duration);
  
  // Create output based on format
  switch (format)
  {
    case "WAV":
      GenerateWav(sineWave, outputFile, totalSamples, sampleRate, channels);
      break;
    case "MP3":
      GenerateMp3(sineWave, outputFile, totalSamples, sampleRate, channels);
      break;
    default:
      throw new ArgumentException($"Unsupported format: {format}. Use WAV or MP3.");
  }

  Console.WriteLine($"Successfully generated: {outputFile}");
  return 0;
}
catch (Exception ex)
{
  Console.Error.WriteLine($"Error: {ex.Message}");
  return 1;
}

void GenerateWav(SineWaveProvider sineWave, string outputFile, int totalSamples, int sampleRate, int channels)
{
  using var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
  using var writer = new BinaryWriter(outputStream);
  
  // Write WAV header
  WriteWavHeader(writer, totalSamples, sampleRate, channels);
  
  // Write audio data
  float[] buffer = new float[sampleRate]; // 1 second buffer
  int samplesWritten = 0;

  while (samplesWritten < totalSamples)
  {
    int samplesToRead = Math.Min(buffer.Length, totalSamples - samplesWritten);
    int samplesRead = sineWave.Read(buffer, 0, samplesToRead);
    
    if (samplesRead == 0)
      break;

    // Write floats as bytes
    for (int i = 0; i < samplesRead; i++)
    {
      writer.Write(buffer[i]);
    }
    
    samplesWritten += samplesRead;
  }
}

void WriteWavHeader(BinaryWriter writer, int totalSamples, int sampleRate, int channels)
{
  int bytesPerSample = 4; // 32-bit float
  int dataSize = totalSamples * channels * bytesPerSample;
  
  // RIFF header
  writer.Write(new[] { 'R', 'I', 'F', 'F' });
  writer.Write(36 + dataSize); // File size - 8
  writer.Write(new[] { 'W', 'A', 'V', 'E' });
  
  // fmt chunk
  writer.Write(new[] { 'f', 'm', 't', ' ' });
  writer.Write(16); // fmt chunk size
  writer.Write((short)3); // Audio format (3 = IEEE float)
  writer.Write((short)channels);
  writer.Write(sampleRate);
  writer.Write(sampleRate * channels * bytesPerSample); // Byte rate
  writer.Write((short)(channels * bytesPerSample)); // Block align
  writer.Write((short)(bytesPerSample * 8)); // Bits per sample
  
  // data chunk
  writer.Write(new[] { 'd', 'a', 't', 'a' });
  writer.Write(dataSize);
}

void GenerateMp3(SineWaveProvider sineWave, string outputFile, int totalSamples, int sampleRate, int channels)
{
  // MP3 encoding requires platform-specific support
  // For now, we'll generate a WAV file with .mp3 extension and provide guidance
  Console.WriteLine("WARNING: MP3 encoding requires additional setup.");
  Console.WriteLine("For testing purposes, generating WAV format instead.");
  Console.WriteLine("To enable MP3: Use FFmpeg or LAME to convert WAV to MP3.");
  
  // Generate as WAV for now
  string wavFile = Path.ChangeExtension(outputFile, ".wav");
  GenerateWav(sineWave, wavFile, totalSamples, sampleRate, channels);
  
  Console.WriteLine($"Generated WAV file: {wavFile}");
  Console.WriteLine("For MP3 support, consider using FFmpeg or LAME to convert WAV to MP3.");
}

// Simple sine wave provider that outputs 32-bit float samples
public class SineWaveProvider
{
  private readonly float frequency;
  private readonly int sampleRate;
  private int sampleIndex;

  public SineWaveProvider(float frequency, int sampleRate)
  {
    this.frequency = frequency;
    this.sampleRate = sampleRate;
  }

  public int Read(float[] buffer, int offset, int count)
  {
    for (int i = 0; i < count; i++)
    {
      // Generate sine wave: amplitude * sin(2π * frequency * time)
      double time = (double)sampleIndex / sampleRate;
      buffer[offset + i] = (float)(Math.Sin(2 * Math.PI * frequency * time) * 0.25); // 25% amplitude to avoid clipping
      sampleIndex++;
    }
    
    return count;
  }
}
