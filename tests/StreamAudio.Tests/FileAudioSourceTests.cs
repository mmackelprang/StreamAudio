using FluentAssertions;
using StreamAudio.Core.Sources;

namespace StreamAudio.Tests;

public class FileAudioSourceTests : IDisposable
{
  private const string TestDataPath = "../../../../../testdata";
  private readonly List<IDisposable> disposables = new();

  [Fact]
  public void Constructor_WithValidFile_ShouldInitialize()
  {
    // Arrange
    string testFile = Path.Combine(TestDataPath, "100hz.wav");

    // Act
    var source = new FileAudioSource(testFile);
    disposables.Add(source);

    // Assert
    source.Name.Should().Be("100hz.wav");
    source.SampleRate.Should().Be(44100);
    source.Channels.Should().Be(1);
    source.HasEnded.Should().BeFalse();
    source.Repeat.Should().BeFalse();
  }

  [Fact]
  public void Constructor_WithInvalidFile_ShouldThrowFileNotFoundException()
  {
    // Arrange
    string invalidFile = "nonexistent.wav";

    // Act
    Action act = () => new FileAudioSource(invalidFile);

    // Assert
    act.Should().Throw<FileNotFoundException>();
  }

  [Fact]
  public void Read_ShouldReturnAudioSamples()
  {
    // Arrange
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    var source = new FileAudioSource(testFile);
    disposables.Add(source);

    float[] buffer = new float[1024];

    // Act
    int samplesRead = source.Read(buffer, 0, buffer.Length);

    // Assert
    samplesRead.Should().Be(buffer.Length);
    buffer.Should().NotBeEmpty();
    buffer.Should().Contain(x => x != 0f); // Should contain non-zero samples
  }

  [Fact]
  public void Read_WithRepeatEnabled_ShouldLoopWhenEndReached()
  {
    // Arrange
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    var source = new FileAudioSource(testFile) { Repeat = true };
    disposables.Add(source);

    // Read all data
    float[] buffer = new float[44100]; // 1 second at 44100 Hz
    int totalSamplesRead = 0;
    while (totalSamplesRead < 44100)
    {
      int samplesRead = source.Read(buffer, 0, buffer.Length);
      totalSamplesRead += samplesRead;
      if (!source.Repeat && source.HasEnded)
        break;
    }

    // Act - Read more data (should loop back)
    int additionalSamples = source.Read(buffer, 0, 1024);

    // Assert
    additionalSamples.Should().Be(1024);
    source.HasEnded.Should().BeFalse();
  }

  [Fact]
  public void Read_WithRepeatDisabled_ShouldMarkAsEnded()
  {
    // Arrange
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    var source = new FileAudioSource(testFile) { Repeat = false };
    disposables.Add(source);

    // Read all data (file is 1 second = 44100 samples)
    float[] buffer = new float[44100 * 2]; // Larger than file

    // Act
    int samplesRead = source.Read(buffer, 0, buffer.Length);

    // Assert - File should end and pad with silence
    samplesRead.Should().Be(buffer.Length);
    source.HasEnded.Should().BeTrue();
  }

  public void Dispose()
  {
    foreach (var disposable in disposables)
    {
      disposable.Dispose();
    }
    disposables.Clear();
  }
}
