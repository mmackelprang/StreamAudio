using SpotifyAPI.Web;
using SoundFlow.Components;
using SoundFlow.Structs;
using StreamAudio.Core.Audio;

namespace StreamAudio.Core.Sources;

/// <summary>
/// Configuration for Spotify audio source.
/// </summary>
public class SpotifyConfiguration
{
  /// <summary>
  /// Spotify Client ID
  /// </summary>
  public string? ClientId { get; set; }

  /// <summary>
  /// Spotify Client Secret
  /// </summary>
  public string? ClientSecret { get; set; }

  /// <summary>
  /// Spotify Refresh Token (for user authentication)
  /// </summary>
  public string? RefreshToken { get; set; }

  /// <summary>
  /// Redirect URI for authentication
  /// </summary>
  public string RedirectUri { get; set; } = "http://localhost:5000/callback";

  /// <summary>
  /// Market/Country code (e.g., "US")
  /// </summary>
  public string Market { get; set; } = "US";

  /// <summary>
  /// Maximum items to retrieve in API calls
  /// </summary>
  public int MaxItems { get; set; } = 50;

  /// <summary>
  /// Use simulation mode (for testing without credentials)
  /// </summary>
  public bool UseSimulation { get; set; } = false;
}

/// <summary>
/// Audio source for Spotify streaming.
/// Note: Spotify Web API doesn't provide direct audio streaming.
/// This source manages Spotify playback control and metadata only.
/// Actual audio playback happens through the Spotify Connect API on authorized devices.
/// </summary>
public class SpotifyAudioSource : IAudioSource
{
  private readonly SpotifyConfiguration config;
  private SpotifyClient? spotify;
  private SongMetadata? currentMetadata;
  private bool disposed;
  private bool isInitialized;
  private CancellationTokenSource? metadataUpdateCts;
  private Task? metadataUpdateTask;
  private readonly AudioFormat format;

  public SpotifyAudioSource(SpotifyConfiguration config, AudioFormat? format = null)
  {
    this.config = config ?? throw new ArgumentNullException(nameof(config));
    this.format = format ?? AudioFormat.DvdHq;
  }

  /// <summary>
  /// Gets the name of the audio source.
  /// </summary>
  public string Name => "Spotify";

  /// <summary>
  /// Gets the audio format.
  /// Note: Spotify doesn't provide direct audio streaming, so this is nominal.
  /// </summary>
  public AudioFormat Format => format;

  /// <summary>
  /// Gets the sample rate.
  /// </summary>
  public int SampleRate => format.SampleRate;

  /// <summary>
  /// Gets the number of channels.
  /// </summary>
  public int Channels => format.Channels;

  /// <summary>
  /// Spotify sources are Manual type (long-running user-controlled playback).
  /// </summary>
  public SourceType SourceType => SourceType.Manual;

  /// <summary>
  /// Spotify sources don't repeat (continuous playback from playlists).
  /// </summary>
  public int RepeatCount { get; set; } = 0; // 0 = infinite

  /// <summary>
  /// Gets metadata for the currently playing track.
  /// </summary>
  public SongMetadata? CurrentlyPlaying => currentMetadata;

  /// <summary>
  /// Gets the underlying SoundPlayer.
  /// Note: Spotify doesn't use SoundPlayer as it doesn't provide direct audio streaming.
  /// This returns a stub player for interface compatibility.
  /// </summary>
  public SoundPlayer Player
  {
    get
    {
      // Spotify uses Connect API, not direct streaming
      // Return a stub player for interface compatibility
      throw new NotSupportedException(
        "Spotify audio source doesn't use SoundPlayer. " +
        "Use Play(), Pause(), Stop() methods to control Spotify Connect playback.");
    }
  }

  /// <summary>
  /// Gets the current playback state.
  /// </summary>
  public SoundFlow.Enums.PlaybackState State
  {
    get
    {
      if (!isInitialized || spotify == null)
        return SoundFlow.Enums.PlaybackState.Stopped;

      try
      {
        var playback = spotify.Player.GetCurrentPlayback().GetAwaiter().GetResult();
        if (playback == null || !playback.IsPlaying)
          return SoundFlow.Enums.PlaybackState.Stopped;

        return SoundFlow.Enums.PlaybackState.Playing;
      }
      catch
      {
        return SoundFlow.Enums.PlaybackState.Stopped;
      }
    }
  }

  /// <summary>
  /// Initializes the Spotify client and authenticates.
  /// </summary>
  public async Task InitializeAsync()
  {
    if (isInitialized)
      return;

    if (config.UseSimulation)
    {
      isInitialized = true;
      currentMetadata = new SongMetadata
      {
        Title = "Simulation Track",
        Artist = "Simulation Artist",
        Album = "Simulation Album",
        Duration = TimeSpan.FromMinutes(3)
      };
      return;
    }

    if (string.IsNullOrWhiteSpace(config.ClientId))
      throw new InvalidOperationException("Spotify ClientId is required");

    try
    {
      // Authenticate with refresh token if available
      if (!string.IsNullOrWhiteSpace(config.RefreshToken))
      {
        var tokenResponse = new PKCETokenResponse
        {
          AccessToken = "initial",
          TokenType = "Bearer",
          ExpiresIn = 0,
          RefreshToken = config.RefreshToken,
          Scope = string.Empty,
          CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var authenticator = new PKCEAuthenticator(config.ClientId, tokenResponse);
        var clientConfig = SpotifyClientConfig.CreateDefault().WithAuthenticator(authenticator);
        spotify = new SpotifyClient(clientConfig);

        // Test authentication
        var profile = await spotify.UserProfile.Current();
        isInitialized = true;
      }
      // Fall back to client credentials (limited functionality)
      else if (!string.IsNullOrWhiteSpace(config.ClientSecret))
      {
        var clientConfig = SpotifyClientConfig
          .CreateDefault()
          .WithAuthenticator(new ClientCredentialsAuthenticator(config.ClientId, config.ClientSecret));

        spotify = new SpotifyClient(clientConfig);

        // Test connection
        var search = await spotify.Search.Item(new SearchRequest(SearchRequest.Types.Track, "test"));
        isInitialized = search != null;
      }
      else
      {
        throw new InvalidOperationException(
          "Spotify requires either RefreshToken (for user auth) or ClientSecret (for client credentials)");
      }

      // Start metadata update task
      StartMetadataUpdates();
    }
    catch (Exception ex)
    {
      throw new InvalidOperationException($"Failed to initialize Spotify: {ex.Message}", ex);
    }
  }

  /// <summary>
  /// Plays/resumes Spotify playback.
  /// </summary>
  public void Play()
  {
    if (!isInitialized)
      throw new InvalidOperationException("Spotify is not initialized. Call InitializeAsync() first.");

    if (config.UseSimulation)
      return;

    if (spotify == null)
      throw new InvalidOperationException("Spotify client is not available");

    try
    {
      spotify.Player.ResumePlayback().GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
      throw new InvalidOperationException($"Failed to play Spotify: {ex.Message}", ex);
    }
  }

  /// <summary>
  /// Pauses Spotify playback.
  /// </summary>
  public void Pause()
  {
    if (!isInitialized || spotify == null)
      return;

    if (config.UseSimulation)
      return;

    try
    {
      spotify.Player.PausePlayback().GetAwaiter().GetResult();
    }
    catch
    {
      // Ignore pause errors
    }
  }

  /// <summary>
  /// Stops Spotify playback.
  /// </summary>
  public void Stop()
  {
    Pause(); // Spotify doesn't have a "stop" - use pause
    StopMetadataUpdates();
  }

  /// <summary>
  /// Play a specific track by URI.
  /// </summary>
  public async Task PlayTrackAsync(string trackUri)
  {
    if (!isInitialized || spotify == null)
      throw new InvalidOperationException("Spotify is not initialized");

    if (config.UseSimulation)
      return;

    try
    {
      var request = new PlayerResumePlaybackRequest
      {
        Uris = new List<string> { trackUri }
      };

      await spotify.Player.ResumePlayback(request);
    }
    catch (Exception ex)
    {
      throw new InvalidOperationException($"Failed to play track {trackUri}: {ex.Message}", ex);
    }
  }

  private void StartMetadataUpdates()
  {
    metadataUpdateCts = new CancellationTokenSource();
    metadataUpdateTask = Task.Run(async () =>
    {
      while (!metadataUpdateCts.Token.IsCancellationRequested)
      {
        try
        {
          await UpdateCurrentMetadata();
          await Task.Delay(2000, metadataUpdateCts.Token); // Update every 2 seconds
        }
        catch (OperationCanceledException)
        {
          break;
        }
        catch
        {
          // Ignore metadata update errors
        }
      }
    }, metadataUpdateCts.Token);
  }

  private void StopMetadataUpdates()
  {
    metadataUpdateCts?.Cancel();
    if (metadataUpdateTask != null)
    {
      try
      {
        metadataUpdateTask.Wait(TimeSpan.FromSeconds(5));
      }
      catch
      {
        // Ignore
      }
    }
    metadataUpdateCts?.Dispose();
    metadataUpdateCts = null;
    metadataUpdateTask = null;
  }

  private async Task UpdateCurrentMetadata()
  {
    if (spotify == null || config.UseSimulation)
      return;

    try
    {
      var currentlyPlaying = await spotify.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest());

      if (currentlyPlaying?.Item is FullTrack track)
      {
        currentMetadata = new SongMetadata
        {
          Title = track.Name,
          Artist = string.Join(", ", track.Artists.Select(a => a.Name)),
          Album = track.Album.Name,
          Duration = TimeSpan.FromMilliseconds(track.DurationMs),
          Position = TimeSpan.FromMilliseconds(currentlyPlaying.ProgressMs ?? 0),
          AlbumArtUrl = track.Album.Images.FirstOrDefault()?.Url,
          Genre = null // Spotify doesn't provide genre in track object
        };

        // Add additional info
        currentMetadata.AdditionalInfo["SpotifyUri"] = track.Uri;
        currentMetadata.AdditionalInfo["TrackId"] = track.Id;
        currentMetadata.AdditionalInfo["IsPlayable"] = track.IsPlayable.ToString();
      }
    }
    catch
    {
      // Ignore metadata fetch errors
    }
  }

  public void Dispose()
  {
    if (disposed)
      return;

    disposed = true;
    StopMetadataUpdates();
    GC.SuppressFinalize(this);
  }
}
