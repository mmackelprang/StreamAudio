using FluentAssertions;
using StreamAudio.Core.Interfaces;
using StreamAudio.Core.Mixing;
using StreamAudio.Core.Sources;

namespace StreamAudio.Tests;

public class BasicMixerTests : IDisposable
{
  private const string TestDataPath = "../../../../../testdata";
  private readonly List<IDisposable> disposables = new();

  [Fact]
  public void Constructor_WithValidParameters_ShouldInitialize()
  {
    // Act
    var mixer = new BasicMixer(44100, 1);
    disposables.Add(mixer);

    // Assert
    mixer.SampleRate.Should().Be(44100);
    mixer.Channels.Should().Be(1);
    mixer.PrimaryVolume.Should().Be(1.0f);
    mixer.BackgroundVolume.Should().Be(0.3f);
  }

  [Fact]
  public void AddSource_WithValidSource_ShouldAddToMixer()
  {
    // Arrange
    var mixer = new BasicMixer(44100, 1);
    disposables.Add(mixer);

    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    var source = new FileAudioSource(testFile);
    disposables.Add(source);

    // Act
    mixer.AddSource(source);

    // Assert - Should not throw and mixer should work
    float[] buffer = new float[1024];
    int samplesRead = mixer.Read(buffer, 0, buffer.Length);
    samplesRead.Should().Be(buffer.Length);
  }

  [Fact]
  public void AddSource_WithMismatchedSampleRate_ShouldThrowException()
  {
    // Arrange
    var mixer = new BasicMixer(48000, 1); // Different sample rate
    disposables.Add(mixer);

    string testFile = Path.Combine(TestDataPath, "100hz.wav"); // 44100 Hz
    var source = new FileAudioSource(testFile);
    disposables.Add(source);

    // Act
    Action act = () => mixer.AddSource(source);

    // Assert
    act.Should().Throw<ArgumentException>()
      .WithMessage("*sample rate*");
  }

  [Fact]
  public void Read_WithTwoSources_ShouldMixAudio()
  {
    // Arrange
    var mixer = new BasicMixer(44100, 1);
    disposables.Add(mixer);

    string testFile1 = Path.Combine(TestDataPath, "100hz.wav");
    string testFile2 = Path.Combine(TestDataPath, "200hz.wav");
    
    var source1 = new FileAudioSource(testFile1);
    var source2 = new FileAudioSource(testFile2);
    disposables.Add(source1);
    disposables.Add(source2);

    mixer.AddSource(source1);
    mixer.AddSource(source2);

    float[] buffer = new float[1024];

    // Act
    int samplesRead = mixer.Read(buffer, 0, buffer.Length);

    // Assert
    samplesRead.Should().Be(buffer.Length);
    buffer.Should().Contain(x => x != 0f); // Should have audio data
    
    // Mixed signal should have higher amplitude than a single source
    float maxAmplitude = buffer.Max(Math.Abs);
    maxAmplitude.Should().BeGreaterThan(0.1f);
  }

  [Fact]
  public void SetVolume_ShouldAdjustSourceVolume()
  {
    // Arrange
    var mixer = new BasicMixer(44100, 1);
    disposables.Add(mixer);

    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    var source = new FileAudioSource(testFile);
    disposables.Add(source);

    mixer.AddSource(source);

    // Act
    mixer.SetVolume(source, 0.5f);

    // Assert
    mixer.GetVolume(source).Should().Be(0.5f);
  }

  [Fact]
  public void SetPrimarySource_ShouldAdjustVolumes()
  {
    // Arrange
    var mixer = new BasicMixer(44100, 1);
    disposables.Add(mixer);

    string testFile1 = Path.Combine(TestDataPath, "100hz.wav");
    string testFile2 = Path.Combine(TestDataPath, "200hz.wav");
    
    var source1 = new FileAudioSource(testFile1);
    var source2 = new FileAudioSource(testFile2);
    disposables.Add(source1);
    disposables.Add(source2);

    mixer.AddSource(source1);
    mixer.AddSource(source2);

    // Act
    mixer.SetPrimarySource(source1);

    // Assert
    mixer.GetVolume(source1).Should().Be(mixer.PrimaryVolume);
    mixer.GetVolume(source2).Should().Be(mixer.BackgroundVolume);
  }

  [Fact]
  public void RemoveSource_ShouldRemoveFromMixer()
  {
    // Arrange
    var mixer = new BasicMixer(44100, 1);
    disposables.Add(mixer);

    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    var source = new FileAudioSource(testFile);
    disposables.Add(source);

    mixer.AddSource(source);

    // Act
    mixer.RemoveSource(source);

    // Assert - Reading should return silence
    float[] buffer = new float[1024];
    int samplesRead = mixer.Read(buffer, 0, buffer.Length);
    samplesRead.Should().Be(buffer.Length);
    buffer.Should().OnlyContain(x => x == 0f);
  }

  [Fact]
  public void Read_WithPrimaryAndBackgroundSources_ShouldApplyVolumeCorrectly()
  {
    // Arrange
    var mixer = new BasicMixer(44100, 1)
    {
      PrimaryVolume = 1.0f,
      BackgroundVolume = 0.2f
    };
    disposables.Add(mixer);

    string primaryFile = Path.Combine(TestDataPath, "100hz.wav");
    string backgroundFile = Path.Combine(TestDataPath, "200hz.wav");
    
    var primarySource = new FileAudioSource(primaryFile);
    var backgroundSource = new FileAudioSource(backgroundFile);
    disposables.Add(primarySource);
    disposables.Add(backgroundSource);

    mixer.AddSource(primarySource, isPrimary: true);
    mixer.AddSource(backgroundSource, isPrimary: false);

    float[] buffer = new float[1024];

    // Act
    int samplesRead = mixer.Read(buffer, 0, buffer.Length);

    // Assert
    samplesRead.Should().Be(buffer.Length);
    
    // Primary source should be at full volume
    mixer.GetVolume(primarySource).Should().Be(1.0f);
    // Background should be attenuated
    mixer.GetVolume(backgroundSource).Should().Be(0.2f);
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
