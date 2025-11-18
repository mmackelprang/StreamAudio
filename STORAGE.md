# Storage System Documentation

## Overview

StreamAudio provides a flexible storage system for persisting configuration data, device settings, metadata history, and other application state. The storage system supports multiple backends (JSON files and SQLite) with a consistent interface.

## Storage Interface

The `IStorage` interface provides a simple key-value storage API organized by tables:

```csharp
public interface IStorage
{
    Task SaveAsync<T>(string table, string key, T data);
    Task<T?> LoadAsync<T>(string table, string key);
    Task<bool> ExistsAsync(string table, string key);
    Task DeleteAsync(string table, string key);
    Task<IEnumerable<string>> GetKeysAsync(string table);
    Task<Dictionary<string, T>> GetAllAsync<T>(string table);
    Task<IEnumerable<string>> GetTablesAsync();
    Task<IStorage> ReplicateStorageAsync(IStorage target);
    Task<string> BackupAsync();
    Task RestoreAsync(string backupPath);
    Task ClearAsync(string table);
}
```

## Storage Implementations

### JSON File Storage

Stores data in JSON files, one file per table. Simple and human-readable.

**Location:** `<RootDir>/storage/` (configurable)
**Backup Location:** `<RootDir>/backup/` (configurable)

```csharp
var storage = new JsonFileStorage(storageDir, backupDir);
```

### SQLite Storage

Stores data in a SQLite database file. Better for larger datasets and more complex queries.

**Location:** `<RootDir>/storage/streamaudio.db` (configurable)
**Backup Location:** `<RootDir>/backup/` (configurable)

```csharp
var storage = new SqliteStorage(dbPath, backupDir);
```

## StorageManager

The `StorageManager` class provides a singleton for accessing storage throughout the application:

```csharp
// Get the current storage instance
var storage = StorageManager.Instance.Storage;

// Save data
await storage.SaveAsync("devices", "chromecast-1", deviceConfig);

// Load data
var config = await storage.LoadAsync<DeviceConfiguration>("devices", "chromecast-1");

// Switch storage backends
var sqliteStorage = new SqliteStorage("./data/app.db", "./backups");
StorageManager.Instance.SetStorage(sqliteStorage);
```

## Configuration

Storage type and location are configured in `appsettings.json`:

```json
{
  "StorageType": "Json",  // or "Sqlite"
  "RootDir": "./",
  "Storage": {
    "Directory": "storage",
    "BackupDirectory": "backup",
    "SqlitePath": "streamaudio.db"
  }
}
```

## Secrets Management

The storage system includes a simple secrets management feature. Store sensitive data in the `[SECRETS]` table:

```csharp
// Store a secret
await storage.SaveAsync("[SECRETS]", "spotify_api_key", "your-secret-key");

// Reference a secret in configuration
await storage.SaveAsync("spotify", "config", new {
    ApiKey = "[SECRET:spotify_api_key]"  // Will be replaced with actual value on load
});

// Load configuration (secret automatically resolved)
var config = await storage.LoadAsync<SpotifyConfig>("spotify", "config");
// config.ApiKey will contain "your-secret-key"
```

**Note:** This is a basic secrets system. For production use, consider integrating with a proper secrets manager like Azure Key Vault or HashiCorp Vault.

## Common Use Cases

### Device Configuration

Store and retrieve device configurations:

```csharp
var storage = StorageManager.Instance.Storage;

// Save device configuration
var deviceConfig = new DeviceConfiguration
{
    Id = "living-room-chromecast",
    Name = "Living Room Speaker",
    Type = "ChromeCast",
    Category = "Playback"
};
await storage.SaveAsync("devices", deviceConfig.Id, deviceConfig);

// Load device configuration
var config = await storage.LoadAsync<DeviceConfiguration>("devices", "living-room-chromecast");

// List all devices
var allConfigs = await storage.GetAllAsync<DeviceConfiguration>("devices");
```

### Metadata History

Track song metadata over time:

```csharp
// Save metadata with timestamp
var metadata = new SongMetadata
{
    Title = "Amazing Song",
    Artist = "Great Artist",
    Band = "FM",
    FrequencyHz = 95300000
};
var historyEntry = new MetadataHistoryEntry
{
    Timestamp = DateTime.UtcNow,
    Metadata = metadata
};
await storage.SaveAsync("metadata_history", Guid.NewGuid().ToString(), historyEntry);

// Retrieve recent history
var history = await storage.GetAllAsync<MetadataHistoryEntry>("metadata_history");
var recent = history.Values
    .OrderByDescending(e => e.Timestamp)
    .Take(10);
```

### Application Settings

Store user preferences and settings:

```csharp
// Save settings
var settings = new UserSettings
{
    DefaultVolume = 0.7f,
    AutoPlay = true,
    Theme = "Dark"
};
await storage.SaveAsync("settings", "user_preferences", settings);

// Load settings
var userSettings = await storage.LoadAsync<UserSettings>("settings", "user_preferences");
```

## Backup and Restore

### Creating Backups

```csharp
var storage = StorageManager.Instance.Storage;

// Create a backup (returns path to backup file)
string backupPath = await storage.BackupAsync();
// e.g., "/app/backup/20231215_143022_Json.backup"
```

### Restoring from Backup

```csharp
// Restore from a backup file
await storage.RestoreAsync("/app/backup/20231215_143022_Json.backup");
```

## Migrating Between Storage Types

Use `ReplicateStorageAsync` to migrate data from one storage type to another:

```csharp
// Current storage (JSON)
var jsonStorage = new JsonFileStorage("./storage", "./backup");

// New storage (SQLite)
var sqliteStorage = new SqliteStorage("./data/app.db", "./backup");

// Copy all data from JSON to SQLite
await jsonStorage.ReplicateStorageAsync(sqliteStorage);

// Switch to the new storage
StorageManager.Instance.SetStorage(sqliteStorage);
```

## Storage Tool

The `StorageDemo` tool in the `tools` directory demonstrates storage operations:

```bash
cd tools/StorageDemo
dotnet run
```

This tool demonstrates:
- Saving and loading data
- Using different storage types
- Backup and restore operations
- Migration between storage types
- Secrets management

## Best Practices

1. **Use Descriptive Table Names:** Organize data logically (e.g., "devices", "settings", "history")

2. **Consistent Key Naming:** Use consistent naming patterns for keys (e.g., "device-id", "config-name")

3. **Regular Backups:** Schedule periodic backups, especially before upgrades

4. **Version Your Data:** Include version fields in your data structures for future compatibility

5. **Cleanup Old Data:** Periodically clean up old history or temporary data:
   ```csharp
   await storage.ClearAsync("temp_data");
   ```

6. **Use Secrets for Sensitive Data:** Never store API keys or passwords directly in regular tables

7. **Handle Missing Data:** Always check for null when loading data:
   ```csharp
   var config = await storage.LoadAsync<Config>("table", "key");
   if (config == null)
   {
       // Handle missing configuration
       config = CreateDefaultConfig();
   }
   ```

## Troubleshooting

### Storage Directory Not Found

Ensure the storage directory exists. The system will create it automatically on first use, but permissions must be correct:

```bash
mkdir -p ./storage
chmod 755 ./storage
```

### Backup Fails

Check that the backup directory is writable:

```bash
mkdir -p ./backup
chmod 755 ./backup
```

### SQLite Database Locked

SQLite may lock the database during long operations. Ensure only one process accesses the database at a time, or use a proper connection pool.

### Migration Issues

When migrating between storage types, verify all data was copied:

```csharp
// After migration, compare table counts
var sourceTables = await sourceStorage.GetTablesAsync();
var targetTables = await targetStorage.GetTablesAsync();

if (sourceTables.Count() != targetTables.Count())
{
    Console.WriteLine("Warning: Table count mismatch!");
}
```

## API Integration

The REST API provides full access to storage operations. See the API documentation for details.

**Base URL:** `/api/storage`

**Endpoints:**
- `POST /api/storage/{table}/{key}` - Save data
- `GET /api/storage/{table}/{key}` - Load data
- `DELETE /api/storage/{table}/{key}` - Delete data
- `GET /api/storage/{table}/keys` - List keys in table
- `GET /api/storage/{table}/all` - Get all data from table
- `GET /api/storage/tables` - List all tables
- `POST /api/storage/backup` - Create backup
- `POST /api/storage/restore` - Restore from backup

See the Swagger documentation at `http://localhost:5000` (when API is running) for interactive API documentation.
