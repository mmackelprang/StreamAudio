using StreamAudio.Core.Configuration;

Console.WriteLine("Configuration Test Tool");
Console.WriteLine("=======================\n");

// Initialize configuration manager
var config = ConfigurationManager.Instance;

Console.WriteLine("\n✓ Configuration Manager initialized successfully!");
Console.WriteLine("\nCurrent Settings:");
Console.WriteLine($"  RootDir: {config.Settings.RootDir}");
Console.WriteLine($"  Log Level: {config.Settings.Logging.MinimumLevel}");
Console.WriteLine($"  Storage Type: {config.Settings.Storage.Type}");
Console.WriteLine($"  Storage Directory: {config.Settings.Storage.Directory}");
Console.WriteLine($"  Backup Directory: {config.Settings.Storage.BackupDirectory}");

// Test logging
config.Logger.Information("This is an Information log message");
config.Logger.Warning("This is a Warning log message");
config.Logger.Error("This is an Error log message");

Console.WriteLine("\n✓ Logging test completed!");
Console.WriteLine($"\nCheck logs at: {Path.Combine(config.Settings.RootDir, "logs")}");

