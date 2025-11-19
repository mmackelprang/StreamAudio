using System.Text.Json;
using StreamAudio.Core.Configuration;

namespace StreamAudio.Core.Storage;

/// <summary>
/// JSON file-based storage implementation.
/// Each table is stored as a separate JSON file in the storage directory.
/// Supports special [SECRETS] table for simple secrets management.
/// </summary>
public class JsonFileStorage : IStorage
{
  private readonly string _storageDir;
  private readonly string _backupDir;
  private readonly JsonSerializerOptions _jsonOptions;
  private const string SecretsTable = "SECRETS";
  private const string SecretPrefix = "[SECRET:";
  private const string SecretSuffix = "]";

  public JsonFileStorage(string? storageDir = null, string? backupDir = null)
  {
    var config = ConfigurationManager.Instance.Settings;
    _storageDir = storageDir ?? Path.Combine(config.RootDir, config.Storage.Directory);
    _backupDir = backupDir ?? Path.Combine(config.RootDir, config.Storage.BackupDirectory);

    Directory.CreateDirectory(_storageDir);
    Directory.CreateDirectory(_backupDir);

    _jsonOptions = new JsonSerializerOptions
    {
      WriteIndented = true,
      PropertyNameCaseInsensitive = true
    };

    ConfigurationManager.Instance.Logger.Information("JsonFileStorage initialized at {StorageDir}", _storageDir);
  }

  public async Task SaveAsync<T>(string table, string key, T data)
  {
    ValidateTableName(table);
    ValidateKey(key);

    var tableFile = GetTableFilePath(table);
    var tableData = await LoadTableDataAsync(tableFile);

    var serializedData = JsonSerializer.Serialize(data, _jsonOptions);
    tableData[key] = serializedData;

    await SaveTableDataAsync(tableFile, tableData);

    ConfigurationManager.Instance.Logger.Debug("Saved {Table}:{Key}", table, key);
  }

  public async Task<T?> LoadAsync<T>(string table, string key)
  {
    ValidateTableName(table);
    ValidateKey(key);

    var tableFile = GetTableFilePath(table);
    if (!File.Exists(tableFile))
    {
      return default;
    }

    var tableData = await LoadTableDataAsync(tableFile);

    if (!tableData.TryGetValue(key, out var serializedData))
    {
      return default;
    }

    // Check if value contains secret reference
    serializedData = await ResolveSecretsAsync(serializedData);

    var result = JsonSerializer.Deserialize<T>(serializedData, _jsonOptions);
    ConfigurationManager.Instance.Logger.Debug("Loaded {Table}:{Key}", table, key);
    return result;
  }

  public async Task<bool> ExistsAsync(string table, string key)
  {
    ValidateTableName(table);
    ValidateKey(key);

    var tableFile = GetTableFilePath(table);
    if (!File.Exists(tableFile))
    {
      return false;
    }

    var tableData = await LoadTableDataAsync(tableFile);
    return tableData.ContainsKey(key);
  }

  public async Task DeleteAsync(string table, string key)
  {
    ValidateTableName(table);
    ValidateKey(key);

    var tableFile = GetTableFilePath(table);
    if (!File.Exists(tableFile))
    {
      return;
    }

    var tableData = await LoadTableDataAsync(tableFile);
    if (tableData.Remove(key))
    {
      await SaveTableDataAsync(tableFile, tableData);
      ConfigurationManager.Instance.Logger.Debug("Deleted {Table}:{Key}", table, key);
    }
  }

  public async Task<IEnumerable<string>> GetKeysAsync(string table)
  {
    ValidateTableName(table);

    var tableFile = GetTableFilePath(table);
    if (!File.Exists(tableFile))
    {
      return Enumerable.Empty<string>();
    }

    var tableData = await LoadTableDataAsync(tableFile);
    return tableData.Keys.ToList();
  }

  public async Task<IEnumerable<T>> GetAllAsync<T>(string table)
  {
    ValidateTableName(table);

    var tableFile = GetTableFilePath(table);
    if (!File.Exists(tableFile))
    {
      return Enumerable.Empty<T>();
    }

    var tableData = await LoadTableDataAsync(tableFile);
    var results = new List<T>();

    foreach (var (_, serializedData) in tableData)
    {
      var resolved = await ResolveSecretsAsync(serializedData);
      var item = JsonSerializer.Deserialize<T>(resolved, _jsonOptions);
      if (item != null)
      {
        results.Add(item);
      }
    }

    return results;
  }

  public Task<IEnumerable<string>> GetTablesAsync()
  {
    var files = Directory.GetFiles(_storageDir, "*.json");
    var tables = files.Select(f => Path.GetFileNameWithoutExtension(f)).ToList();
    return Task.FromResult<IEnumerable<string>>(tables);
  }

  public async Task ReplicateStorageAsync(IStorage target)
  {
    ConfigurationManager.Instance.Logger.Information("Starting storage replication");

    var tables = await GetTablesAsync();
    int totalKeys = 0;

    foreach (var table in tables)
    {
      var keys = await GetKeysAsync(table);
      foreach (var key in keys)
      {
        var tableFile = GetTableFilePath(table);
        var tableData = await LoadTableDataAsync(tableFile);
        if (tableData.TryGetValue(key, out var serializedData))
        {
          // Deserialize as object to preserve type information
          var data = JsonSerializer.Deserialize<object>(serializedData, _jsonOptions);
          await target.SaveAsync(table, key, data);
          totalKeys++;
        }
      }
    }

    ConfigurationManager.Instance.Logger.Information("Replication complete: {TotalKeys} keys copied", totalKeys);
  }

  public Task<string> BackupAsync()
  {
    return Task.Run(() =>
    {
      var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
      var backupFileName = $"{timestamp}_JsonFileStorage.backup.zip";
      var backupPath = Path.Combine(_backupDir, backupFileName);

      ConfigurationManager.Instance.Logger.Information("Creating backup at {BackupPath}", backupPath);

      // Simple backup: create a zip of all JSON files
      var files = Directory.GetFiles(_storageDir, "*.json");
      if (files.Length == 0)
      {
        throw new InvalidOperationException("No data to backup");
      }

      // Create a temporary directory for the backup
      var tempDir = Path.Combine(Path.GetTempPath(), $"streamaudio_backup_{timestamp}");
      Directory.CreateDirectory(tempDir);

      try
      {
        // Copy all files to temp directory
        foreach (var file in files)
        {
          var fileName = Path.GetFileName(file);
          File.Copy(file, Path.Combine(tempDir, fileName), true);
        }

        // Create zip file
        System.IO.Compression.ZipFile.CreateFromDirectory(tempDir, backupPath);

        ConfigurationManager.Instance.Logger.Information("Backup created successfully: {BackupPath}", backupPath);
        return backupPath;
      }
      finally
      {
        // Clean up temp directory
        if (Directory.Exists(tempDir))
        {
          Directory.Delete(tempDir, true);
        }
      }
    });
  }

  public async Task RestoreAsync(string backupPath)
  {
    if (!File.Exists(backupPath))
    {
      throw new FileNotFoundException($"Backup file not found: {backupPath}");
    }

    ConfigurationManager.Instance.Logger.Information("Restoring from backup: {BackupPath}", backupPath);

    // Extract to temporary directory
    var tempDir = Path.Combine(Path.GetTempPath(), $"streamaudio_restore_{DateTime.Now:yyyyMMdd_HHmmss}");
    Directory.CreateDirectory(tempDir);

    try
    {
      System.IO.Compression.ZipFile.ExtractToDirectory(backupPath, tempDir);

      // Copy all JSON files to storage directory
      var files = Directory.GetFiles(tempDir, "*.json");
      foreach (var file in files)
      {
        var fileName = Path.GetFileName(file);
        var targetPath = Path.Combine(_storageDir, fileName);
        File.Copy(file, targetPath, true);
        ConfigurationManager.Instance.Logger.Debug("Restored {FileName}", fileName);
      }

      ConfigurationManager.Instance.Logger.Information("Restore completed successfully");
    }
    finally
    {
      // Clean up temp directory
      if (Directory.Exists(tempDir))
      {
        Directory.Delete(tempDir, true);
      }
    }

    await Task.CompletedTask;
  }

  public void Dispose()
  {
    // No resources to dispose for file-based storage
  }

  public Task<bool> TableExistsAsync(string table)
  {
    var tableFile = GetTableFilePath(table);
    return Task.FromResult(File.Exists(tableFile));
  }

  public Task DeleteTableAsync(string table)
  {
    var tableFile = GetTableFilePath(table);
    if (File.Exists(tableFile))
    {
      File.Delete(tableFile);
    }
    return Task.CompletedTask;
  }

  private string GetTableFilePath(string table)
  {
    return Path.Combine(_storageDir, $"{table}.json");
  }

  private async Task<Dictionary<string, string>> LoadTableDataAsync(string tableFile)
  {
    if (!File.Exists(tableFile))
    {
      return new Dictionary<string, string>();
    }

    try
    {
      var json = await File.ReadAllTextAsync(tableFile);
      var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json, _jsonOptions);
      return data ?? new Dictionary<string, string>();
    }
    catch (Exception ex)
    {
      ConfigurationManager.Instance.Logger.Error(ex, "Failed to load table data from {TableFile}", tableFile);
      return new Dictionary<string, string>();
    }
  }

  private async Task SaveTableDataAsync(string tableFile, Dictionary<string, string> tableData)
  {
    try
    {
      var json = JsonSerializer.Serialize(tableData, _jsonOptions);
      await File.WriteAllTextAsync(tableFile, json);
    }
    catch (Exception ex)
    {
      ConfigurationManager.Instance.Logger.Error(ex, "Failed to save table data to {TableFile}", tableFile);
      throw;
    }
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

    if (table.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
    {
      throw new ArgumentException($"Table name contains invalid characters: {table}", nameof(table));
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
