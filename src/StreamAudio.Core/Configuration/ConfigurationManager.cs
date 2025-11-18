using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Core;

namespace StreamAudio.Core.Configuration;

/// <summary>
/// Manages application configuration and initialization
/// </summary>
public class ConfigurationManager
{
  private static ConfigurationManager? _instance;
  private static readonly object _lock = new();

  private readonly IConfiguration _configuration;
  private readonly AppSettings _settings;

  /// <summary>
  /// Singleton instance of ConfigurationManager
  /// </summary>
  public static ConfigurationManager Instance
  {
    get
    {
      if (_instance == null)
      {
        lock (_lock)
        {
          _instance ??= new ConfigurationManager();
        }
      }
      return _instance;
    }
  }

  /// <summary>
  /// Application settings
  /// </summary>
  public AppSettings Settings => _settings;

  /// <summary>
  /// Logger instance
  /// </summary>
  public ILogger Logger { get; private set; } = null!;

  private ConfigurationManager()
  {
    // Detect if running in test environment
    var isTestEnvironment = AppDomain.CurrentDomain.GetAssemblies()
      .Any(a => a.FullName?.StartsWith("xunit", StringComparison.OrdinalIgnoreCase) == true);

    // Build configuration from appsettings.json and environment variables
    var configBuilder = new ConfigurationBuilder()
      .SetBasePath(Directory.GetCurrentDirectory())
      .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
      .AddEnvironmentVariables("STREAMAUDIO_");

    _configuration = configBuilder.Build();

    // Load settings
    _settings = new AppSettings();
    _configuration.Bind(_settings);

    // Validate and ensure RootDir exists
    if (string.IsNullOrWhiteSpace(_settings.RootDir))
    {
      _settings.RootDir = Directory.GetCurrentDirectory();
    }

    // Ensure RootDir is absolute path
    if (!Path.IsPathRooted(_settings.RootDir))
    {
      _settings.RootDir = Path.GetFullPath(_settings.RootDir);
    }

    // Check if appsettings.json exists and warn if not (but silence during tests)
    if (!isTestEnvironment)
    {
      CheckAppSettingsFile();
    }

    // Initialize logger
    InitializeLogger();

    // Ensure directories exist
    EnsureDirectories();

    // Log startup information (but silence during tests)
    if (!isTestEnvironment)
    {
      LogStartupInfo();
    }
  }

  private void CheckAppSettingsFile()
  {
    var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
    if (!File.Exists(appSettingsPath))
    {
      Console.WriteLine("╔════════════════════════════════════════════════════════════════════════╗");
      Console.WriteLine("║                          ⚠️  WARNING  ⚠️                               ║");
      Console.WriteLine("╠════════════════════════════════════════════════════════════════════════╣");
      Console.WriteLine("║ appsettings.json file not found!                                       ║");
      Console.WriteLine("║                                                                        ║");
      Console.WriteLine($"║ Looking for: {appSettingsPath,-52} ║");
      Console.WriteLine($"║ Current Dir: {Directory.GetCurrentDirectory(),-52} ║");
      Console.WriteLine("║                                                                        ║");
      Console.WriteLine("║ Using default settings. To customize configuration:                    ║");
      Console.WriteLine("║ 1. Create appsettings.json in the current directory                    ║");
      Console.WriteLine("║ 2. Set STREAMAUDIO_* environment variables                             ║");
      Console.WriteLine("║ 3. See documentation for configuration options                         ║");
      Console.WriteLine("╚════════════════════════════════════════════════════════════════════════╝");
      Console.WriteLine();
    }
  }

  private void InitializeLogger()
  {
    // Detect if running in test environment
    var isTestEnvironment = AppDomain.CurrentDomain.GetAssemblies()
      .Any(a => a.FullName?.StartsWith("xunit", StringComparison.OrdinalIgnoreCase) == true);

    // Parse log level
    if (!Enum.TryParse<LogEventLevel>(_settings.Logging.MinimumLevel, true, out var minLevel))
    {
      minLevel = LogEventLevel.Information;
    }

    // Build logger configuration
    var logConfig = new LoggerConfiguration()
      .MinimumLevel.Is(minLevel)
      .Enrich.FromLogContext()
      .Enrich.WithMachineName()
      .Enrich.WithEnvironmentName();

    // Add console sink if enabled (but silence during tests)
    if (_settings.Logging.WriteToConsole && !isTestEnvironment)
    {
      logConfig.WriteTo.Console(
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}"
      );
    }

    // Add file sink if enabled
    if (_settings.Logging.WriteToFile)
    {
      var logDir = Path.Combine(_settings.RootDir, "logs");
      Directory.CreateDirectory(logDir);

      var logPath = Path.Combine(logDir, "streamaudio-.log");

      // Parse rolling interval
      if (!Enum.TryParse<RollingInterval>(_settings.Logging.RollingInterval, true, out var interval))
      {
        interval = RollingInterval.Day;
      }

      logConfig.WriteTo.File(
        logPath,
        rollingInterval: interval,
        retainedFileCountLimit: _settings.Logging.RetainedFileCountLimit,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
      );
    }

    Logger = logConfig.CreateLogger();
  }

  private void EnsureDirectories()
  {
    // Ensure logs directory
    var logsDir = Path.Combine(_settings.RootDir, "logs");
    if (!Directory.Exists(logsDir))
    {
      Directory.CreateDirectory(logsDir);
    }

    // Ensure storage directory
    var storageDir = Path.Combine(_settings.RootDir, _settings.Storage.Directory);
    if (!Directory.Exists(storageDir))
    {
      Directory.CreateDirectory(storageDir);
    }

    // Ensure backup directory
    var backupDir = Path.Combine(_settings.RootDir, _settings.Storage.BackupDirectory);
    if (!Directory.Exists(backupDir))
    {
      Directory.CreateDirectory(backupDir);
    }
  }

  private void LogStartupInfo()
  {
    Logger.Information("═══════════════════════════════════════════════════════════════");
    Logger.Information("StreamAudio Configuration Manager Initialized");
    Logger.Information("═══════════════════════════════════════════════════════════════");
    Logger.Information("Root Directory: {RootDir}", _settings.RootDir);
    Logger.Information("Logs Directory: {LogsDir}", Path.Combine(_settings.RootDir, "logs"));
    Logger.Information("Storage Type: {StorageType}", _settings.Storage.Type);
    Logger.Information("Storage Directory: {StorageDir}", Path.Combine(_settings.RootDir, _settings.Storage.Directory));
    Logger.Information("Backup Directory: {BackupDir}", Path.Combine(_settings.RootDir, _settings.Storage.BackupDirectory));
    Logger.Information("Log Level: {LogLevel}", _settings.Logging.MinimumLevel);
    Logger.Information("═══════════════════════════════════════════════════════════════");
  }

  /// <summary>
  /// Reset the singleton instance (primarily for testing)
  /// </summary>
  public static void Reset()
  {
    lock (_lock)
    {
      _instance?.Logger.Information("Configuration Manager reset");
      _instance = null;
    }
  }
}
