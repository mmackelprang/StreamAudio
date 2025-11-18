using System.Diagnostics;

namespace StreamAudio.Core.Monitoring;

/// <summary>
/// Provides performance monitoring for audio streaming operations.
/// Tracks CPU usage, memory consumption, and other performance metrics.
/// </summary>
public class PerformanceMonitor : IDisposable
{
  private readonly Process currentProcess;
  private DateTime lastCpuCheck;
  private TimeSpan lastTotalProcessorTime;
  private bool disposed;

  /// <summary>
  /// Creates a new PerformanceMonitor instance.
  /// </summary>
  public PerformanceMonitor()
  {
    currentProcess = Process.GetCurrentProcess();
    lastCpuCheck = DateTime.UtcNow;
    lastTotalProcessorTime = currentProcess.TotalProcessorTime;
  }

  /// <summary>
  /// Gets the current CPU usage percentage for the process.
  /// </summary>
  /// <returns>CPU usage as a percentage (0.0 to 100.0 * number of cores).</returns>
  public double GetCpuUsagePercent()
  {
    var now = DateTime.UtcNow;
    var currentTotalProcessorTime = currentProcess.TotalProcessorTime;

    var cpuUsedMs = (currentTotalProcessorTime - lastTotalProcessorTime).TotalMilliseconds;
    var totalMsPassed = (now - lastCpuCheck).TotalMilliseconds;

    var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
    var cpuUsagePercent = cpuUsageTotal * 100;

    lastCpuCheck = now;
    lastTotalProcessorTime = currentTotalProcessorTime;

    return cpuUsagePercent;
  }

  /// <summary>
  /// Gets the current memory usage in bytes.
  /// </summary>
  /// <returns>Memory usage in bytes.</returns>
  public long GetMemoryUsageBytes()
  {
    currentProcess.Refresh();
    return currentProcess.WorkingSet64;
  }

  /// <summary>
  /// Gets the current memory usage in megabytes.
  /// </summary>
  /// <returns>Memory usage in MB.</returns>
  public double GetMemoryUsageMB()
  {
    return GetMemoryUsageBytes() / (1024.0 * 1024.0);
  }

  /// <summary>
  /// Gets the current thread count for the process.
  /// </summary>
  /// <returns>Number of threads.</returns>
  public int GetThreadCount()
  {
    currentProcess.Refresh();
    return currentProcess.Threads.Count;
  }

  /// <summary>
  /// Gets a snapshot of current performance metrics.
  /// </summary>
  /// <returns>A PerformanceSnapshot containing current metrics.</returns>
  public PerformanceSnapshot GetSnapshot()
  {
    return new PerformanceSnapshot
    {
      Timestamp = DateTime.UtcNow,
      CpuUsagePercent = GetCpuUsagePercent(),
      MemoryUsageMB = GetMemoryUsageMB(),
      ThreadCount = GetThreadCount()
    };
  }

  /// <summary>
  /// Starts continuous monitoring and returns snapshots at the specified interval.
  /// </summary>
  /// <param name="intervalMs">Interval between snapshots in milliseconds.</param>
  /// <param name="cancellationToken">Token to cancel monitoring.</param>
  /// <returns>An async enumerable of performance snapshots.</returns>
  public async IAsyncEnumerable<PerformanceSnapshot> MonitorAsync(
    int intervalMs = 1000,
    [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    while (!cancellationToken.IsCancellationRequested && !disposed)
    {
      yield return GetSnapshot();
      
      try
      {
        await Task.Delay(intervalMs, cancellationToken);
      }
      catch (TaskCanceledException)
      {
        // Expected when cancellation is requested during delay
        yield break;
      }
    }
  }

  public void Dispose()
  {
    if (disposed)
      return;

    disposed = true;
    currentProcess?.Dispose();
    GC.SuppressFinalize(this);
  }
}

/// <summary>
/// Represents a snapshot of performance metrics at a specific point in time.
/// </summary>
public class PerformanceSnapshot
{
  /// <summary>
  /// Gets or sets the timestamp when this snapshot was taken.
  /// </summary>
  public DateTime Timestamp { get; set; }

  /// <summary>
  /// Gets or sets the CPU usage percentage.
  /// </summary>
  public double CpuUsagePercent { get; set; }

  /// <summary>
  /// Gets or sets the memory usage in megabytes.
  /// </summary>
  public double MemoryUsageMB { get; set; }

  /// <summary>
  /// Gets or sets the thread count.
  /// </summary>
  public int ThreadCount { get; set; }

  public override string ToString()
  {
    return $"[{Timestamp:HH:mm:ss}] CPU: {CpuUsagePercent:F2}%, Memory: {MemoryUsageMB:F2} MB, Threads: {ThreadCount}";
  }
}
