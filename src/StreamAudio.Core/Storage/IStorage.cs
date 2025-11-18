namespace StreamAudio.Core.Storage;

/// <summary>
/// Interface for persistent storage operations.
/// Supports table-based storage with JSON or SQLite backends.
/// </summary>
public interface IStorage : IDisposable
{
  /// <summary>
  /// Save data to storage table
  /// </summary>
  /// <typeparam name="T">Type of data to save</typeparam>
  /// <param name="table">Table name</param>
  /// <param name="key">Unique key for the data</param>
  /// <param name="data">Data to save</param>
  Task SaveAsync<T>(string table, string key, T data);

  /// <summary>
  /// Load data from storage table
  /// </summary>
  /// <typeparam name="T">Type of data to load</typeparam>
  /// <param name="table">Table name</param>
  /// <param name="key">Unique key for the data</param>
  /// <returns>Data if found, null otherwise</returns>
  Task<T?> LoadAsync<T>(string table, string key);

  /// <summary>
  /// Check if data exists in table
  /// </summary>
  /// <param name="table">Table name</param>
  /// <param name="key">Unique key</param>
  /// <returns>True if data exists, false otherwise</returns>
  Task<bool> ExistsAsync(string table, string key);

  /// <summary>
  /// Delete data from storage table
  /// </summary>
  /// <param name="table">Table name</param>
  /// <param name="key">Unique key for the data</param>
  Task DeleteAsync(string table, string key);

  /// <summary>
  /// Get all keys from storage table
  /// </summary>
  /// <param name="table">Table name</param>
  /// <returns>Collection of all keys in the table</returns>
  Task<IEnumerable<string>> GetKeysAsync(string table);

  /// <summary>
  /// Get all data from storage table
  /// </summary>
  /// <typeparam name="T">Type of data to load</typeparam>
  /// <param name="table">Table name</param>
  /// <returns>Collection of all data in the table</returns>
  Task<IEnumerable<T>> GetAllAsync<T>(string table);

  /// <summary>
  /// Get all table names from storage
  /// </summary>
  /// <returns>Collection of all table names</returns>
  Task<IEnumerable<string>> GetTablesAsync();

  /// <summary>
  /// Copy all data from current storage to target storage
  /// </summary>
  /// <param name="target">Target storage implementation</param>
  Task ReplicateStorageAsync(IStorage target);

  /// <summary>
  /// Create a backup of all data
  /// </summary>
  /// <returns>Path to the backup file</returns>
  Task<string> BackupAsync();

  /// <summary>
  /// Restore data from a backup file
  /// </summary>
  /// <param name="backupPath">Path to the backup file</param>
  Task RestoreAsync(string backupPath);
}
