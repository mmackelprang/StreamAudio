using StreamAudio.Core.Configuration;
using StreamAudio.Core.Storage;

Console.WriteLine("═══════════════════════════════════════════");
Console.WriteLine("  StreamAudio Storage Demo");
Console.WriteLine("═══════════════════════════════════════════\n");

// Initialize configuration
var config = ConfigurationManager.Instance;
var logger = config.Logger;

logger.Information("Storage Demo Started");

// Demonstrate storage operations
await DemoBasicOperations();
await DemoSecretsManagement();
await DemoBackupRestore();
await DemoMigration();

logger.Information("Storage Demo Completed");

async Task DemoBasicOperations()
{
  Console.WriteLine("\n--- Basic Storage Operations ---");
  
  var storage = StorageManager.Instance.Storage;
  
  // Save data
  var userData = new UserData { Id = 1, Name = "John Doe", Email = "john@example.com" };
  await storage.SaveAsync("Users", "user1", userData);
  Console.WriteLine("✓ Saved user data");
  
  // Load data
  var loaded = await storage.LoadAsync<UserData>("Users", "user1");
  Console.WriteLine($"✓ Loaded user: {loaded?.Name} ({loaded?.Email})");
  
  // Check existence
  var exists = await storage.ExistsAsync("Users", "user1");
  Console.WriteLine($"✓ User exists: {exists}");
  
  // Get all keys
  await storage.SaveAsync("Users", "user2", new UserData { Id = 2, Name = "Jane Smith", Email = "jane@example.com" });
  var keys = await storage.GetKeysAsync("Users");
  Console.WriteLine($"✓ Total users: {keys.Count()}");
  
  // Get all values
  var allUsers = await storage.GetAllAsync<UserData>("Users");
  Console.WriteLine($"✓ Retrieved all users: {allUsers.Count()}");
  foreach (var user in allUsers)
  {
    Console.WriteLine($"  - {user.Name}");
  }
}

async Task DemoSecretsManagement()
{
  Console.WriteLine("\n--- Secrets Management ---");
  
  var storage = StorageManager.Instance.Storage;
  
  // Store a secret
  await storage.SaveAsync("SECRETS", "api_key", "sk_test_1234567890abcdef");
  Console.WriteLine("✓ Stored API key secret");
  
  // Store configuration with secret reference
  var apiConfig = new ApiConfig
  {
    Name = "External API",
    Endpoint = "https://api.example.com",
    ApiKey = "[SECRET:api_key]"
  };
  await storage.SaveAsync("Config", "external_api", apiConfig);
  Console.WriteLine("✓ Stored config with secret reference");
  
  // Load configuration (secret will be resolved automatically)
  var loadedConfig = await storage.LoadAsync<ApiConfig>("Config", "external_api");
  Console.WriteLine($"✓ Loaded config - API Key resolved: {loadedConfig?.ApiKey?.StartsWith("sk_")}");
}

async Task DemoBackupRestore()
{
  Console.WriteLine("\n--- Backup and Restore ---");
  
  var storage = StorageManager.Instance.Storage;
  
  // Create backup
  var backupPath = await storage.BackupAsync();
  Console.WriteLine($"✓ Backup created: {backupPath}");
  
  // Modify data
  await storage.SaveAsync("Users", "user1", new UserData { Id = 1, Name = "Modified Name", Email = "modified@example.com" });
  Console.WriteLine("✓ Modified user data");
  
  // Restore from backup
  await storage.RestoreAsync(backupPath);
  Console.WriteLine("✓ Restored from backup");
  
  // Verify restoration
  var restored = await storage.LoadAsync<UserData>("Users", "user1");
  Console.WriteLine($"✓ Verified restoration: {restored?.Name}");
}

async Task DemoMigration()
{
  Console.WriteLine("\n--- Storage Migration (JSON ↔ SQLite) ---");
  
  var currentStorage = StorageManager.Instance.Storage;
  Console.WriteLine($"Current storage type: {config.Settings.Storage.Type}");
  
  // Create target storage (opposite type)
  var targetStorage = config.Settings.Storage.Type.ToLowerInvariant() == "json"
    ? StorageManager.CreateSqliteStorage()
    : StorageManager.CreateJsonStorage();
  
  Console.WriteLine("✓ Created target storage");
  
  // Replicate data
  await StorageManager.MigrateStorageAsync(currentStorage, targetStorage);
  Console.WriteLine("✓ Migrated all data to target storage");
  
  // Verify migration
  var tables = await targetStorage.GetTablesAsync();
  Console.WriteLine($"✓ Target storage has {tables.Count()} tables");
  
  targetStorage.Dispose();
}

class UserData
{
  public int Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public string Email { get; set; } = string.Empty;
}

class ApiConfig
{
  public string Name { get; set; } = string.Empty;
  public string Endpoint { get; set; } = string.Empty;
  public string ApiKey { get; set; } = string.Empty;
}

