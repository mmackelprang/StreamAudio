namespace StreamAudio.Core.Configuration;

/// <summary>
/// Application settings loaded from appsettings.json
/// </summary>
public class AppSettings
{
  /// <summary>
  /// Root directory for the application. Defaults to solution directory.
  /// Used for logs, storage, backups, etc.
  /// </summary>
  public string RootDir { get; set; } = Directory.GetCurrentDirectory();

  /// <summary>
  /// Logging configuration
  /// </summary>
  public LoggingSettings Logging { get; set; } = new();

  /// <summary>
  /// Storage configuration
  /// </summary>
  public StorageSettings Storage { get; set; } = new();
}

/// <summary>
/// Logging configuration
/// </summary>
public class LoggingSettings
{
  /// <summary>
  /// Minimum log level (Verbose, Debug, Information, Warning, Error, Fatal)
  /// </summary>
  public string MinimumLevel { get; set; } = "Information";

  /// <summary>
  /// Whether to write logs to console
  /// </summary>
  public bool WriteToConsole { get; set; } = true;

  /// <summary>
  /// Whether to write logs to file
  /// </summary>
  public bool WriteToFile { get; set; } = true;

  /// <summary>
  /// Log file rolling interval (Day, Hour, Minute, Month, Year, Infinite)
  /// </summary>
  public string RollingInterval { get; set; } = "Day";

  /// <summary>
  /// Number of log files to retain
  /// </summary>
  public int RetainedFileCountLimit { get; set; } = 31;
}

/// <summary>
/// Storage configuration
/// </summary>
public class StorageSettings
{
  /// <summary>
  /// Storage type: "Json" or "Sqlite"
  /// </summary>
  public string Type { get; set; } = "Json";

  /// <summary>
  /// Storage directory relative to RootDir
  /// </summary>
  public string Directory { get; set; } = "storage";

  /// <summary>
  /// Backup directory relative to RootDir
  /// </summary>
  public string BackupDirectory { get; set; } = "backup";
}
