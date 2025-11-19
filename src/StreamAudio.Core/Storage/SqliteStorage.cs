using System.Text.Json;
using Microsoft.Data.Sqlite;
using StreamAudio.Core.Configuration;

namespace StreamAudio.Core.Storage;

/// <summary>
/// SQLite-based storage implementation.
/// All tables are stored in a single SQLite database file.
/// Supports special [SECRETS] table for simple secrets management.
/// </summary>
public class SqliteStorage : IStorage
{
  private readonly string _dbPath;
  private readonly string _backupDir;
  private readonly SqliteConnection _connection;
  private readonly JsonSerializerOptions _jsonOptions;
  private const string SecretsTable = "SECRETS";
  private const string SecretPrefix = "[SECRET:";
  private const string SecretSuffix = "]";

  public SqliteStorage(string? dbPath = null, string? backupDir = null)
  {
    var config = ConfigurationManager.Instance.Settings;
    var storageDir = Path.Combine(config.RootDir, config.Storage.Directory);
    _dbPath = dbPath ?? Path.Combine(storageDir, "streamaudio.db");
    _backupDir = backupDir ?? Path.Combine(config.RootDir, config.Storage.BackupDirectory);

    Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
    Directory.CreateDirectory(_backupDir);

    _connection = new SqliteConnection($"Data Source={_dbPath}");
    _connection.Open();

    _jsonOptions = new JsonSerializerOptions
    {
      WriteIndented = false,
      PropertyNameCaseInsensitive = true
    };

    InitializeDatabaseAsync().GetAwaiter().GetResult();

    ConfigurationManager.Instance.Logger.Information("SqliteStorage initialized at {DbPath}", _dbPath);
  }

  private async Task InitializeDatabaseAsync()
  {
    // Create metadata table to track all tables
    var createMetadataTable = @"
      CREATE TABLE IF NOT EXISTS __metadata__ (
        table_name TEXT PRIMARY KEY,
        created_at TEXT NOT NULL
      )";

    using var cmd = _connection.CreateCommand();
    cmd.CommandText = createMetadataTable;
    await cmd.ExecuteNonQueryAsync();
  }

  private async Task EnsureTableExistsAsync(string table)
  {
    var createTableSql = $@"
      CREATE TABLE IF NOT EXISTS [{table}] (
        key TEXT PRIMARY KEY,
        value TEXT NOT NULL,
        updated_at TEXT NOT NULL
      )";

    using var cmd = _connection.CreateCommand();
    cmd.CommandText = createTableSql;
    await cmd.ExecuteNonQueryAsync();

    // Add to metadata
    var insertMetadataSql = @"
      INSERT OR IGNORE INTO __metadata__ (table_name, created_at)
      VALUES (@table, @created)";

    using var metaCmd = _connection.CreateCommand();
    metaCmd.CommandText = insertMetadataSql;
    metaCmd.Parameters.AddWithValue("@table", table);
    metaCmd.Parameters.AddWithValue("@created", DateTime.UtcNow.ToString("O"));
    await metaCmd.ExecuteNonQueryAsync();
  }

  public async Task SaveAsync<T>(string table, string key, T data)
  {
    ValidateTableName(table);
    ValidateKey(key);

    await EnsureTableExistsAsync(table);

    var serializedData = JsonSerializer.Serialize(data, _jsonOptions);

    var sql = $@"
      INSERT OR REPLACE INTO [{table}] (key, value, updated_at)
      VALUES (@key, @value, @updated)";

    using var cmd = _connection.CreateCommand();
    cmd.CommandText = sql;
    cmd.Parameters.AddWithValue("@key", key);
    cmd.Parameters.AddWithValue("@value", serializedData);
    cmd.Parameters.AddWithValue("@updated", DateTime.UtcNow.ToString("O"));

    await cmd.ExecuteNonQueryAsync();

    ConfigurationManager.Instance.Logger.Debug("Saved {Table}:{Key}", table, key);
  }

  public async Task<T?> LoadAsync<T>(string table, string key)
  {
    ValidateTableName(table);
    ValidateKey(key);

    await EnsureTableExistsAsync(table);

    var sql = $"SELECT value FROM [{table}] WHERE key = @key";

    using var cmd = _connection.CreateCommand();
    cmd.CommandText = sql;
    cmd.Parameters.AddWithValue("@key", key);

    var result = await cmd.ExecuteScalarAsync();
    if (result == null || result == DBNull.Value)
    {
      return default;
    }

    var serializedData = result.ToString()!;

    // Check if value contains secret reference
    serializedData = await ResolveSecretsAsync(serializedData);

    var data = JsonSerializer.Deserialize<T>(serializedData, _jsonOptions);
    ConfigurationManager.Instance.Logger.Debug("Loaded {Table}:{Key}", table, key);
    return data;
  }

  public async Task<bool> ExistsAsync(string table, string key)
  {
    ValidateTableName(table);
    ValidateKey(key);

    await EnsureTableExistsAsync(table);

    var sql = $"SELECT COUNT(*) FROM [{table}] WHERE key = @key";

    using var cmd = _connection.CreateCommand();
    cmd.CommandText = sql;
    cmd.Parameters.AddWithValue("@key", key);

    var count = Convert.ToInt64(await cmd.ExecuteScalarAsync());
    return count > 0;
  }

  public async Task DeleteAsync(string table, string key)
  {
    ValidateTableName(table);
    ValidateKey(key);

    await EnsureTableExistsAsync(table);

    var sql = $"DELETE FROM [{table}] WHERE key = @key";

    using var cmd = _connection.CreateCommand();
    cmd.CommandText = sql;
    cmd.Parameters.AddWithValue("@key", key);

    var rowsAffected = await cmd.ExecuteNonQueryAsync();
    if (rowsAffected > 0)
    {
      ConfigurationManager.Instance.Logger.Debug("Deleted {Table}:{Key}", table, key);
    }
  }

  public async Task<IEnumerable<string>> GetKeysAsync(string table)
  {
    ValidateTableName(table);

    await EnsureTableExistsAsync(table);

    var sql = $"SELECT key FROM [{table}]";

    using var cmd = _connection.CreateCommand();
    cmd.CommandText = sql;

    var keys = new List<string>();
    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
      keys.Add(reader.GetString(0));
    }

    return keys;
  }

  public async Task<IEnumerable<T>> GetAllAsync<T>(string table)
  {
    ValidateTableName(table);

    await EnsureTableExistsAsync(table);

    var sql = $"SELECT value FROM [{table}]";

    using var cmd = _connection.CreateCommand();
    cmd.CommandText = sql;

    var results = new List<T>();
    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
      var serializedData = reader.GetString(0);
      var resolved = await ResolveSecretsAsync(serializedData);
      var item = JsonSerializer.Deserialize<T>(resolved, _jsonOptions);
      if (item != null)
      {
        results.Add(item);
      }
    }

    return results;
  }

  public async Task<IEnumerable<string>> GetTablesAsync()
  {
    var sql = "SELECT table_name FROM __metadata__";

    using var cmd = _connection.CreateCommand();
    cmd.CommandText = sql;

    var tables = new List<string>();
    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
      tables.Add(reader.GetString(0));
    }

    return tables;
  }

  public async Task ReplicateStorageAsync(IStorage target)
  {
    ConfigurationManager.Instance.Logger.Information("Starting storage replication");

    var tables = await GetTablesAsync();
    int totalKeys = 0;

    foreach (var table in tables)
    {
      var sql = $"SELECT key, value FROM [{table}]";

      using var cmd = _connection.CreateCommand();
      cmd.CommandText = sql;

      using var reader = await cmd.ExecuteReaderAsync();
      while (await reader.ReadAsync())
      {
        var key = reader.GetString(0);
        var serializedData = reader.GetString(1);

        // Deserialize as object to preserve type information
        var data = JsonSerializer.Deserialize<object>(serializedData, _jsonOptions);
        await target.SaveAsync(table, key, data);
        totalKeys++;
      }
    }

    ConfigurationManager.Instance.Logger.Information("Replication complete: {TotalKeys} keys copied", totalKeys);
  }

  public async Task<string> BackupAsync()
  {
    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
    var backupFileName = $"{timestamp}_SqliteStorage.backup.db";
    var backupPath = Path.Combine(_backupDir, backupFileName);

    ConfigurationManager.Instance.Logger.Information("Creating backup at {BackupPath}", backupPath);

    // Close and copy the database file
    _connection.Close();

    try
    {
      File.Copy(_dbPath, backupPath, true);
      ConfigurationManager.Instance.Logger.Information("Backup created successfully: {BackupPath}", backupPath);
    }
    finally
    {
      // Reopen connection
      _connection.Open();
    }

    return await Task.FromResult(backupPath);
  }

  public async Task RestoreAsync(string backupPath)
  {
    if (!File.Exists(backupPath))
    {
      throw new FileNotFoundException($"Backup file not found: {backupPath}");
    }

    ConfigurationManager.Instance.Logger.Information("Restoring from backup: {BackupPath}", backupPath);

    // Close connection and replace database file
    _connection.Close();

    try
    {
      File.Copy(backupPath, _dbPath, true);
      ConfigurationManager.Instance.Logger.Information("Restore completed successfully");
    }
    finally
    {
      // Reopen connection
      _connection.Open();
    }

    await Task.CompletedTask;
  }

  public void Dispose()
  {
    _connection?.Close();
    _connection?.Dispose();
    SqliteConnection.ClearAllPools();
    GC.Collect();
    GC.WaitForPendingFinalizers();
  }

  public async Task<bool> TableExistsAsync(string table)
  {
    var sql = "SELECT name FROM sqlite_master WHERE type='table' AND name=@table;";
    using var cmd = _connection.CreateCommand();
    cmd.CommandText = sql;
    cmd.Parameters.AddWithValue("@table", table);
    var result = await cmd.ExecuteScalarAsync();
    return result != null;
  }

  public async Task DeleteTableAsync(string table)
  {
    ValidateTableName(table);
    var sql = $"DROP TABLE IF EXISTS [{table}]";
    using var cmd = _connection.CreateCommand();
    cmd.CommandText = sql;
    await cmd.ExecuteNonQueryAsync();

    // Also remove from metadata
    var metaSql = "DELETE FROM __metadata__ WHERE table_name = @table";
    using var metaCmd = _connection.CreateCommand();
    metaCmd.CommandText = metaSql;
    metaCmd.Parameters.AddWithValue("@table", table);
    await metaCmd.ExecuteNonQueryAsync();
  }

  private async Task<string> ResolveSecretsAsync(string value)
  {
    if (string.IsNullOrEmpty(value))
    {
      return value;
    }

    // Check if value contains secret references
    var result = value;
    var startIndex = result.IndexOf(SecretPrefix, StringComparison.Ordinal);

    while (startIndex >= 0)
    {
      var endIndex = result.IndexOf(SecretSuffix, startIndex + SecretPrefix.Length, StringComparison.Ordinal);
      if (endIndex < 0)
      {
        break;
      }

      var secretKey = result.Substring(startIndex + SecretPrefix.Length, endIndex - startIndex - SecretPrefix.Length);
      var secretValue = await LoadAsync<string>(SecretsTable, secretKey);

      if (secretValue != null)
      {
        var secretRef = result.Substring(startIndex, endIndex - startIndex + SecretSuffix.Length);
        result = result.Replace(secretRef, secretValue);
      }

      startIndex = result.IndexOf(SecretPrefix, startIndex + 1, StringComparison.Ordinal);
    }

    return result;
  }

  private void ValidateTableName(string table)
  {
    if (string.IsNullOrWhiteSpace(table))
    {
      throw new ArgumentException("Table name cannot be null or empty", nameof(table));
    }

    // Don't allow metadata table
    if (table == "__metadata__")
    {
      throw new ArgumentException("Cannot use reserved table name: __metadata__", nameof(table));
    }
  }

  private void ValidateKey(string key)
  {
    if (string.IsNullOrWhiteSpace(key))
    {
      throw new ArgumentException("Key cannot be null or empty", nameof(key));
    }
  }
}
