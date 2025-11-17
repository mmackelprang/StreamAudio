using FluentAssertions;
using StreamAudio.Core;
using StreamAudio.Core.Sources;

namespace StreamAudio.Tests;

[Collection("AudioTests")]
public class EnhancedFileAudioSourceTests : IDisposable
{
  private const string TestDataPath = "../../../../../testdata";
  private readonly List<IDisposable> disposables = new();

  [Fact]
  public void Constructor_WithSingleFile_ShouldDefaultToManualSourceType()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    string testFile = Path.Combine(TestDataPath, "100hz.wav");

    // Act
    var source = new FileAudioSource(testFile);
    disposables.Add(source);

    // Assert
    source.SourceType.Should().Be(SourceType.Manual);
    source.RepeatCount.Should().Be(1);
  }

  [Fact]
  public void Constructor_WithMultipleFiles_ShouldDefaultToManualSourceType()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    var files = new List<string>
    {
      Path.Combine(TestDataPath, "100hz.wav"),
      Path.Combine(TestDataPath, "200hz.wav")
    };

    // Act
    var source = new FileAudioSource(files);
    disposables.Add(source);

    // Assert
    source.SourceType.Should().Be(SourceType.Manual);
    source.RepeatCount.Should().Be(1);
  }

  [Fact]
  public void FromDirectory_ShouldLoadAllAudioFiles()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange & Act
    var source = FileAudioSource.FromDirectory(TestDataPath);
    disposables.Add(source);

    // Assert
    source.SourceType.Should().Be(SourceType.Manual);
    source.Name.Should().Contain("Directory");
  }

  [Fact]
  public void FromDirectory_WithNonexistentDirectory_ShouldThrowDirectoryNotFoundException()
  {
    // Arrange
    string invalidDir = "/nonexistent/directory";

    // Act
    Action act = () => FileAudioSource.FromDirectory(invalidDir);

    // Assert
    act.Should().Throw<DirectoryNotFoundException>();
  }

  [Fact]
  public void Constructor_WithEmptyFileList_ShouldThrowArgumentException()
  {
    // Arrange
    var emptyList = new List<string>();

    // Act
    Action act = () => new FileAudioSource(emptyList);

    // Assert
    act.Should().Throw<ArgumentException>();
  }

  [Fact]
  public void Constructor_WithNullFileList_ShouldThrowArgumentNullException()
  {
    // Arrange
    List<string>? nullList = null;

    // Act
    Action act = () => new FileAudioSource(nullList!);

    // Assert
    act.Should().Throw<ArgumentNullException>();
  }

  [Fact]
  public void CurrentlyPlaying_WithSingleFile_ShouldReturnMetadata()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    var source = new FileAudioSource(testFile);
    disposables.Add(source);

    // Act
    var metadata = source.CurrentlyPlaying;

    // Assert (WAV files may have limited metadata)
    metadata.Should().NotBeNull();
    metadata!.AdditionalInfo.Should().ContainKey("FileName");
  }

  [Fact]
  public void SourceType_CanBeOverridden()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    string testFile = Path.Combine(TestDataPath, "100hz.wav");

    // Act
    var source = new FileAudioSource(testFile, sourceType: SourceType.Manual);
    disposables.Add(source);

    // Assert
    source.SourceType.Should().Be(SourceType.Manual);
  }

  [Fact]
  public void RepeatCount_CanBeSet()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    var source = new FileAudioSource(testFile);
    disposables.Add(source);

    // Act
    source.RepeatCount = 3;

    // Assert
    source.RepeatCount.Should().Be(3);
  }

  [Theory]
  [InlineData(1)]
  [InlineData(2)]
  [InlineData(5)]
  [InlineData(0)] // Infinite
  public void RepeatCount_ShouldAcceptValidValues(int repeatCount)
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    var source = new FileAudioSource(testFile);
    disposables.Add(source);

    // Act
    source.RepeatCount = repeatCount;

    // Assert
    source.RepeatCount.Should().Be(repeatCount);
  }

  [Fact]
  public void Constructor_WithMultipleFiles_ShouldAcceptAllValidFiles()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    var files = new List<string>
    {
      Path.Combine(TestDataPath, "50hz.wav"),
      Path.Combine(TestDataPath, "100hz.wav"),
      Path.Combine(TestDataPath, "200hz.wav")
    };

    // Act
    var source = new FileAudioSource(files);
    disposables.Add(source);

    // Assert
    source.Name.Should().NotBeNullOrEmpty();
    source.SourceType.Should().Be(SourceType.Manual);
  }

  [Fact]
  public void Constructor_WithMixedValidAndInvalidFiles_ShouldThrow()
  {
    // Arrange
    var files = new List<string>
    {
      Path.Combine(TestDataPath, "100hz.wav"),
      "nonexistent.wav"
    };

    // Act
    Action act = () => new FileAudioSource(files);

    // Assert
    act.Should().Throw<FileNotFoundException>();
  }

  [Fact]
  public void Name_WithSingleFile_ShouldBeFileName()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    string testFile = Path.Combine(TestDataPath, "100hz.wav");

    // Act
    var source = new FileAudioSource(testFile);
    disposables.Add(source);

    // Assert
    source.Name.Should().Be("100hz.wav");
  }

  [Fact]
  public void Name_WithMultipleFiles_ShouldIndicatePlaylist()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    var files = new List<string>
    {
      Path.Combine(TestDataPath, "100hz.wav"),
      Path.Combine(TestDataPath, "200hz.wav")
    };

    // Act
    var source = new FileAudioSource(files);
    disposables.Add(source);

    // Assert
    source.Name.Should().NotBeNullOrEmpty();
  }

  [Fact]
  public void Metadata_ShouldContainFileInformation()
  {
    // Skip audio tests in headless environment
    if (IsHeadlessEnvironment())
    {
      return;
    }

    // Arrange
    string testFile = Path.Combine(TestDataPath, "100hz.wav");
    var source = new FileAudioSource(testFile);
    disposables.Add(source);

    // Act
    var metadata = source.CurrentlyPlaying;

    // Assert
    metadata.Should().NotBeNull();
    metadata!.AdditionalInfo.Should().ContainKey("FileName");
    metadata.AdditionalInfo["FileName"].Should().Contain("100hz.wav");
  }

  private bool IsHeadlessEnvironment()
  {
    // Check for CI environment variables
    return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) ||
           !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
  }

  public void Dispose()
  {
    foreach (var disposable in disposables)
    {
      try
      {
        disposable?.Dispose();
      }
      catch
      {
        // Ignore disposal errors
      }
    }
    disposables.Clear();
  }
}
