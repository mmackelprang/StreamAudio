using StreamAudio.Core.Audio;
using StreamAudio.Core.Configuration;
using StreamAudio.Core.Storage;

namespace StreamAudio.Core.History;

/// <summary>
/// Represents a metadata entry with timestamp for history tracking.
/// </summary>
public class MetadataHistoryEntry
{
  /// <summary>
  /// Timestamp when the metadata was recorded
  /// </summary>
  public DateTime Timestamp { get; set; }

  /// <summary>
  /// Song metadata snapshot
  /// </summary>
  public SongMetadata Metadata { get; set; } = new();

  /// <summary>
  /// Source name that played this content
  /// </summary>
  public string SourceName { get; set; } = string.Empty;
}

/// <summary>
/// Manages storage of metadata history for Manual audio sources.
/// Stores timestamped metadata entries in persistent storage.
/// </summary>
public class MetadataHistoryManager
{
  private const string HistoryTable = "MetadataHistory";
  private readonly IStorage _storage;
  private readonly string _sourceName;
  private readonly bool _isEnabled;

  /// <summary>
  /// Creates a new metadata history manager for a specific audio source.
  /// </summary>
  /// <param name="sourceName">Name of the audio source</param>
  /// <param name="isManualSource">True if this is a Manual source type (history is only stored for Manual sources)</param>
  public MetadataHistoryManager(string sourceName, bool isManualSource)
  {
    _sourceName = sourceName;
    _isEnabled = isManualSource;
    _storage = StorageManager.Instance.Storage;

    if (_isEnabled)
    {
      ConfigurationManager.Instance.Logger.Debug(
        "Metadata history tracking enabled for source: {SourceName}", _sourceName);
    }
  }

  /// <summary>
  /// Records metadata to history with current timestamp.
  /// Only stores if source is Manual type.
  /// </summary>
  public async Task RecordMetadataAsync(SongMetadata metadata)
  {
    if (!_isEnabled || metadata == null)
      return;

    try
    {
      var entry = new MetadataHistoryEntry
      {
        Timestamp = DateTime.UtcNow,
        Metadata = metadata.Clone(),
        SourceName = _sourceName
      };

      // Use timestamp as key for uniqueness
      var key = $"{_sourceName}_{entry.Timestamp:yyyyMMddHHmmss_fff}";
      await _storage.SaveAsync(HistoryTable, key, entry);

      ConfigurationManager.Instance.Logger.Debug(
        "Recorded metadata history: {SourceName} - {Title} at {Timestamp}",
        _sourceName, metadata.Title ?? "Unknown", entry.Timestamp);
    }
    catch (Exception ex)
    {
      ConfigurationManager.Instance.Logger.Error(
        ex, "Failed to record metadata history for {SourceName}", _sourceName);
    }
  }

  /// <summary>
  /// Gets metadata history for this source, ordered by timestamp descending.
  /// </summary>
  /// <param name="limit">Maximum number of entries to return (default 100)</param>
  public async Task<IEnumerable<MetadataHistoryEntry>> GetHistoryAsync(int limit = 100)
  {
    if (!_isEnabled)
      return Enumerable.Empty<MetadataHistoryEntry>();

    try
    {
      var allEntries = await _storage.GetAllAsync<MetadataHistoryEntry>(HistoryTable);
      return allEntries
        .Where(e => e.SourceName == _sourceName)
        .OrderByDescending(e => e.Timestamp)
        .Take(limit);
    }
    catch (Exception ex)
    {
      ConfigurationManager.Instance.Logger.Error(
        ex, "Failed to retrieve metadata history for {SourceName}", _sourceName);
      return Enumerable.Empty<MetadataHistoryEntry>();
    }
  }

  /// <summary>
  /// Gets metadata history for all sources, ordered by timestamp descending.
  /// </summary>
  /// <param name="limit">Maximum number of entries to return (default 100)</param>
  public static async Task<IEnumerable<MetadataHistoryEntry>> GetAllHistoryAsync(int limit = 100)
  {
    try
    {
      var storage = StorageManager.Instance.Storage;
      var allEntries = await storage.GetAllAsync<MetadataHistoryEntry>(HistoryTable);
      return allEntries
        .OrderByDescending(e => e.Timestamp)
        .Take(limit);
    }
    catch (Exception ex)
    {
      ConfigurationManager.Instance.Logger.Error(
        ex, "Failed to retrieve all metadata history");
      return Enumerable.Empty<MetadataHistoryEntry>();
    }
  }

  /// <summary>
  /// Clears history for this source.
  /// </summary>
  public async Task ClearHistoryAsync()
  {
    if (!_isEnabled)
      return;

    try
    {
      var keys = await _storage.GetKeysAsync(HistoryTable);
      var sourceKeys = keys.Where(k => k.StartsWith($"{_sourceName}_"));

      foreach (var key in sourceKeys)
      {
        await _storage.DeleteAsync(HistoryTable, key);
      }

      ConfigurationManager.Instance.Logger.Information(
        "Cleared metadata history for {SourceName}", _sourceName);
    }
    catch (Exception ex)
    {
      ConfigurationManager.Instance.Logger.Error(
        ex, "Failed to clear metadata history for {SourceName}", _sourceName);
    }
  }

  /// <summary>
  /// Clears all metadata history for all sources.
  /// </summary>
  public static async Task ClearAllHistoryAsync()
  {
    try
    {
      var storage = StorageManager.Instance.Storage;
      var keys = await storage.GetKeysAsync(HistoryTable);

      foreach (var key in keys)
      {
        await storage.DeleteAsync(HistoryTable, key);
      }

      ConfigurationManager.Instance.Logger.Information("Cleared all metadata history");
    }
    catch (Exception ex)
    {
      ConfigurationManager.Instance.Logger.Error(ex, "Failed to clear all metadata history");
    }
  }
}
