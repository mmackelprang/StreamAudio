namespace StreamAudio.Core.Audio;

/// <summary>
/// Represents metadata for currently playing audio content.
/// Used by audio sources to provide information about what's currently playing.
/// </summary>
public class SongMetadata
{
  /// <summary>
  /// Gets or sets the title of the song/audio content.
  /// </summary>
  public string? Title { get; set; }

  /// <summary>
  /// Gets or sets the artist name.
  /// </summary>
  public string? Artist { get; set; }

  /// <summary>
  /// Gets or sets the album name.
  /// </summary>
  public string? Album { get; set; }

  /// <summary>
  /// Gets or sets the station name (for radio sources).
  /// </summary>
  public string? Station { get; set; }

  /// <summary>
  /// Gets or sets the radio band (e.g., "FM", "AM", "SW" for radio sources).
  /// </summary>
  public string? Band { get; set; }

  /// <summary>
  /// Gets or sets the frequency in Hz (for radio sources).
  /// </summary>
  public int? FrequencyHz { get; set; }

  /// <summary>
  /// Gets or sets the genre.
  /// </summary>
  public string? Genre { get; set; }

  /// <summary>
  /// Gets or sets the total duration of the audio content.
  /// </summary>
  public TimeSpan? Duration { get; set; }

  /// <summary>
  /// Gets or sets the current playback position.
  /// </summary>
  public TimeSpan? Position { get; set; }

  /// <summary>
  /// Gets or sets the URL to album art/cover image.
  /// </summary>
  public string? AlbumArtUrl { get; set; }

  /// <summary>
  /// Gets or sets additional information as key-value pairs.
  /// Can be used to store source-specific metadata.
  /// </summary>
  public Dictionary<string, string> AdditionalInfo { get; set; } = new();

  /// <summary>
  /// Creates a clone of this metadata instance.
  /// </summary>
  public SongMetadata Clone()
  {
    return new SongMetadata
    {
      Title = this.Title,
      Artist = this.Artist,
      Album = this.Album,
      Station = this.Station,
      Band = this.Band,
      FrequencyHz = this.FrequencyHz,
      Genre = this.Genre,
      Duration = this.Duration,
      Position = this.Position,
      AlbumArtUrl = this.AlbumArtUrl,
      AdditionalInfo = new Dictionary<string, string>(this.AdditionalInfo)
    };
  }
}
