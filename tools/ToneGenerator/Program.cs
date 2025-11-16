using NAudio.Wave;

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
  var sineWave = new SineWaveProvider32(frequency, sampleRate);

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

void GenerateWav(SineWaveProvider32 sineWave, string outputFile, int totalSamples, int sampleRate, int channels)
{
  // Use IEEE float format which is what SineWaveProvider32 produces
  var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
  using var writer = new WaveFileWriter(outputFile, waveFormat);
  
  float[] buffer = new float[sampleRate]; // 1 second buffer
  int samplesWritten = 0;

  while (samplesWritten < totalSamples)
  {
    int samplesToRead = Math.Min(buffer.Length, totalSamples - samplesWritten);
    int samplesRead = sineWave.Read(buffer, 0, samplesToRead);
    
    if (samplesRead == 0)
      break;

    writer.WriteSamples(buffer, 0, samplesRead);
    samplesWritten += samplesRead;
  }
}

void GenerateMp3(SineWaveProvider32 sineWave, string outputFile, int totalSamples, int sampleRate, int channels)
{
  // MP3 encoding requires platform-specific support
  // For now, we'll generate a WAV file with .mp3 extension and provide guidance
  Console.WriteLine("WARNING: MP3 encoding requires additional setup.");
  Console.WriteLine("For testing purposes, generating WAV format instead.");
  Console.WriteLine("To enable MP3: Install NAudio.Lame package and LAME encoder.");
  
  // Generate as WAV for now
  string wavFile = Path.ChangeExtension(outputFile, ".wav");
  GenerateWav(sineWave, wavFile, totalSamples, sampleRate, channels);
  
  Console.WriteLine($"Generated WAV file: {wavFile}");
  Console.WriteLine("For MP3 support, consider using FFmpeg or LAME to convert WAV to MP3.");
}

// Simple sine wave provider that outputs 32-bit float samples
public class SineWaveProvider32 : IWaveProvider
{
  private readonly float frequency;
  private readonly int sampleRate;
  private int sampleIndex;

  public SineWaveProvider32(float frequency, int sampleRate)
  {
    this.frequency = frequency;
    this.sampleRate = sampleRate;
    WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
  }

  public WaveFormat WaveFormat { get; }

  public int Read(byte[] buffer, int offset, int count)
  {
    // Convert to float array for easier manipulation
    int floatCount = count / 4; // 4 bytes per float
    float[] floatBuffer = new float[floatCount];
    
    Read(floatBuffer, 0, floatCount);
    
    // Convert float array back to bytes
    Buffer.BlockCopy(floatBuffer, 0, buffer, offset, count);
    
    return count;
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
