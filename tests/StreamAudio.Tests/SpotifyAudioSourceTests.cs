using FluentAssertions;
using StreamAudio.Core.Sources;
using StreamAudio.Core.Audio;

namespace StreamAudio.Tests;

[Collection("AudioTests")]
public class SpotifyAudioSourceTests : IDisposable
{
  private readonly List<IDisposable> disposables = new();

  [Fact]
  public void Constructor_WithValidConfiguration_ShouldInitialize()
  {
    // Arrange
    var config = new SpotifyConfiguration
    {
      UseSimulation = true
    };

    // Act
    var source = new SpotifyAudioSource(config);
    disposables.Add(source);

    // Assert
    source.Name.Should().Be("Spotify");
    source.SampleRate.Should().BeGreaterThan(0);
    source.Channels.Should().BeGreaterThan(0);
    source.SourceType.Should().Be(SourceType.Manual);
    source.RepeatCount.Should().Be(0); // Infinite for continuous playback
  }

  [Fact]
  public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
  {
    // Act
    Action act = () => new SpotifyAudioSource(null!);

    // Assert
    act.Should().Throw<ArgumentNullException>()
      .WithParameterName("config");
  }

  [Fact]
  public async Task InitializeAsync_WithSimulation_ShouldSucceed()
  {
    // Arrange
    var config = new SpotifyConfiguration { UseSimulation = true };
    var source = new SpotifyAudioSource(config);
    disposables.Add(source);

    // Act
    await source.InitializeAsync();

    // Assert
    source.CurrentlyPlaying.Should().NotBeNull();
    source.CurrentlyPlaying!.Title.Should().Be("Simulation Track");
    source.CurrentlyPlaying.Artist.Should().Be("Simulation Artist");
  }

  [Fact]
  public async Task InitializeAsync_WithoutClientId_ShouldThrowInvalidOperationException()
  {
    // Arrange
    var config = new SpotifyConfiguration
    {
      UseSimulation = false,
      ClientId = null
    };
    var source = new SpotifyAudioSource(config);
    disposables.Add(source);

    // Act
    Func<Task> act = async () => await source.InitializeAsync();

    // Assert
    await act.Should().ThrowAsync<InvalidOperationException>()
      .WithMessage("*ClientId*");
  }

  [Fact]
  public async Task InitializeAsync_WithoutCredentials_ShouldThrowInvalidOperationException()
  {
    // Arrange
    var config = new SpotifyConfiguration
    {
      UseSimulation = false,
      ClientId = "test_client_id"
      // No RefreshToken or ClientSecret
    };
    var source = new SpotifyAudioSource(config);
    disposables.Add(source);

    // Act
    Func<Task> act = async () => await source.InitializeAsync();

    // Assert
    await act.Should().ThrowAsync<InvalidOperationException>()
      .WithMessage("*RefreshToken*ClientSecret*");
  }

  [Fact]
  public void Play_WithoutInitialization_ShouldThrowInvalidOperationException()
  {
    // Arrange
    var config = new SpotifyConfiguration { UseSimulation = true };
    var source = new SpotifyAudioSource(config);
    disposables.Add(source);

    // Act
    Action act = () => source.Play();

    // Assert
    act.Should().Throw<InvalidOperationException>()
      .WithMessage("*not initialized*");
  }

  [Fact]
  public async Task Play_AfterInitialization_ShouldNotThrow()
  {
    // Arrange
    var config = new SpotifyConfiguration { UseSimulation = true };
    var source = new SpotifyAudioSource(config);
    disposables.Add(source);
    await source.InitializeAsync();

    // Act & Assert (simulation mode doesn't throw)
    source.Play();
  }

  [Fact]
  public void Pause_WithoutInitialization_ShouldNotThrow()
  {
    // Arrange
    var config = new SpotifyConfiguration { UseSimulation = true };
    var source = new SpotifyAudioSource(config);
    disposables.Add(source);

    // Act & Assert (Pause returns early without throwing)
    source.Pause();
  }

  [Fact]
  public void Stop_WithoutInitialization_ShouldNotThrow()
  {
    // Arrange
    var config = new SpotifyConfiguration { UseSimulation = true };
    var source = new SpotifyAudioSource(config);
    disposables.Add(source);

    // Act & Assert (Stop returns early without throwing)
    source.Stop();
  }

  [Fact]
  public async Task PlayTrackAsync_WithoutInitialization_ShouldThrowInvalidOperationException()
  {
    // Arrange
    var config = new SpotifyConfiguration { UseSimulation = true };
    var source = new SpotifyAudioSource(config);
    disposables.Add(source);

    // Act
    Func<Task> act = async () => await source.PlayTrackAsync("spotify:track:test");

    // Assert
    await act.Should().ThrowAsync<InvalidOperationException>()
      .WithMessage("*not initialized*");
  }

  [Fact]
  public async Task PlayTrackAsync_AfterInitialization_InSimulation_ShouldNotThrow()
  {
    // Arrange
    var config = new SpotifyConfiguration { UseSimulation = true };
    var source = new SpotifyAudioSource(config);
    disposables.Add(source);
    await source.InitializeAsync();

    // Act & Assert (simulation mode doesn't actually play tracks)
    await source.PlayTrackAsync("spotify:track:test");
  }

  [Fact]
  public void Player_ShouldThrowNotSupportedException()
  {
    // Arrange
    var config = new SpotifyConfiguration { UseSimulation = true };
    var source = new SpotifyAudioSource(config);
    disposables.Add(source);

    // Act
    Action act = () => { var _ = source.Player; };

    // Assert
    act.Should().Throw<NotSupportedException>()
      .WithMessage("*doesn't use SoundPlayer*");
  }

  [Fact]
  public void SourceType_ShouldBeManual()
  {
    // Arrange
    var config = new SpotifyConfiguration { UseSimulation = true };
    var source = new SpotifyAudioSource(config);
    disposables.Add(source);

    // Assert
    source.SourceType.Should().Be(SourceType.Manual);
  }

  [Fact]
  public void RepeatCount_ShouldBeZero()
  {
    // Arrange
    var config = new SpotifyConfiguration { UseSimulation = true };
    var source = new SpotifyAudioSource(config);
    disposables.Add(source);

    // Assert (0 = infinite for continuous playback)
    source.RepeatCount.Should().Be(0);
  }

  [Fact]
  public void CurrentlyPlaying_BeforeInitialization_ShouldBeNull()
  {
    // Arrange
    var config = new SpotifyConfiguration { UseSimulation = true };
    var source = new SpotifyAudioSource(config);
    disposables.Add(source);

    // Assert
    source.CurrentlyPlaying.Should().BeNull();
  }

  [Fact]
  public async Task CurrentlyPlaying_AfterInitialization_ShouldHaveMetadata()
  {
    // Arrange
    var config = new SpotifyConfiguration { UseSimulation = true };
    var source = new SpotifyAudioSource(config);
    disposables.Add(source);

    // Act
    await source.InitializeAsync();

    // Assert
    source.CurrentlyPlaying.Should().NotBeNull();
    source.CurrentlyPlaying!.Title.Should().NotBeNullOrWhiteSpace();
    source.CurrentlyPlaying.Artist.Should().NotBeNullOrWhiteSpace();
  }

  [Fact]
  public void Configuration_DefaultValues_ShouldBeCorrect()
  {
    // Arrange & Act
    var config = new SpotifyConfiguration();

    // Assert
    config.ClientId.Should().BeNull();
    config.ClientSecret.Should().BeNull();
    config.RefreshToken.Should().BeNull();
    config.RedirectUri.Should().Be("http://localhost:5000/callback");
    config.Market.Should().Be("US");
    config.MaxItems.Should().Be(50);
    config.UseSimulation.Should().BeFalse();
  }

  [Fact]
  public void Configuration_CustomValues_ShouldBeAccepted()
  {
    // Arrange & Act
    var config = new SpotifyConfiguration
    {
      ClientId = "test_id",
      ClientSecret = "test_secret",
      RefreshToken = "test_token",
      RedirectUri = "http://localhost:8080/callback",
      Market = "GB",
      MaxItems = 100,
      UseSimulation = true
    };

    // Assert
    config.ClientId.Should().Be("test_id");
    config.ClientSecret.Should().Be("test_secret");
    config.RefreshToken.Should().Be("test_token");
    config.RedirectUri.Should().Be("http://localhost:8080/callback");
    config.Market.Should().Be("GB");
    config.MaxItems.Should().Be(100);
    config.UseSimulation.Should().BeTrue();
  }

  [Fact]
  public void Dispose_ShouldCleanupResources()
  {
    // Arrange
    var config = new SpotifyConfiguration { UseSimulation = true };
    var source = new SpotifyAudioSource(config);

    // Act
    source.Dispose();

    // Assert (should not throw)
    source.Dispose(); // Second dispose should be safe
  }

  [Fact]
  public async Task State_WithoutPlayback_ShouldBeStopped()
  {
    // Arrange
    var config = new SpotifyConfiguration { UseSimulation = true };
    var source = new SpotifyAudioSource(config);
    disposables.Add(source);
    await source.InitializeAsync();

    // Act
    var state = source.State;

    // Assert
    state.Should().Be(SoundFlow.Enums.PlaybackState.Stopped);
  }

  public void Dispose()
  {
    foreach (var disposable in disposables)
    {
      disposable?.Dispose();
    }
    disposables.Clear();
  }
}
