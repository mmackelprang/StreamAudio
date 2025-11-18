using StreamAudio.Core.Configuration;

namespace StreamAudio.Core.Storage;

/// <summary>
/// Manages storage instances and provides factory methods.
/// Singleton pattern to ensure consistent storage access across the application.
/// </summary>
public class StorageManager
{
  private static StorageManager? _instance;
  private static readonly object _lock = new();

  private IStorage? _storage;

  /// <summary>
  /// Singleton instance of StorageManager
  /// </summary>
  public static StorageManager Instance
  {
    get
    {
      if (_instance == null)
      {
        lock (_lock)
        {
          _instance ??= new StorageManager();
        }
      }
      return _instance;
    }
  }

  /// <summary>
  /// Current storage instance
  /// </summary>
  public IStorage Storage
  {
    get
    {
      if (_storage == null)
      {
        lock (_lock)
        {
          _storage ??= CreateStorage();
        }
      }
      return _storage;
    }
  }

  private StorageManager()
  {
    // Private constructor for singleton
  }

  /// <summary>
  /// Create a storage instance based on configuration
  /// </summary>
  private IStorage CreateStorage()
  {
    var config = ConfigurationManager.Instance.Settings;
    var storageType = config.Storage.Type.ToLowerInvariant();

    IStorage storage = storageType switch
    {
      "json" => new JsonFileStorage(),
      "sqlite" => new SqliteStorage(),
      _ => throw new InvalidOperationException($"Unknown storage type: {config.Storage.Type}")
    };

    ConfigurationManager.Instance.Logger.Information("Storage initialized: {StorageType}", storageType);
    return storage;
  }

  /// <summary>
  /// Create a JSON file storage instance
  /// </summary>
  public static IStorage CreateJsonStorage(string? storageDir = null, string? backupDir = null)
  {
    return new JsonFileStorage(storageDir, backupDir);
  }

  /// <summary>
  /// Create a SQLite storage instance
  /// </summary>
  public static IStorage CreateSqliteStorage(string? dbPath = null, string? backupDir = null)
  {
    return new SqliteStorage(dbPath, backupDir);
  }

  /// <summary>
  /// Replace the current storage instance
  /// </summary>
  /// <param name="storage">New storage instance</param>
  public void SetStorage(IStorage storage)
  {
    lock (_lock)
    {
      _storage?.Dispose();
      _storage = storage;
      ConfigurationManager.Instance.Logger.Information("Storage instance replaced");
    }
  }

  /// <summary>
  /// Reset the singleton instance (primarily for testing)
  /// </summary>
  public static void Reset()
  {
    lock (_lock)
    {
      _instance?._storage?.Dispose();
      _instance = null;
    }
  }

  /// <summary>
  /// Migrate data from one storage type to another
  /// </summary>
  public static async Task MigrateStorageAsync(IStorage source, IStorage target)
  {
    ConfigurationManager.Instance.Logger.Information("Starting storage migration");
    await source.ReplicateStorageAsync(target);
    ConfigurationManager.Instance.Logger.Information("Storage migration completed");
  }
}
