using FluentAssertions;
using StreamAudio.Core.Monitoring;

namespace StreamAudio.Tests;

public class PerformanceMonitorTests : IDisposable
{
  private readonly PerformanceMonitor monitor;

  public PerformanceMonitorTests()
  {
    monitor = new PerformanceMonitor();
  }

  [Fact]
  public void GetCpuUsagePercent_ShouldReturnValidValue()
  {
    // Act
    var cpuUsage = monitor.GetCpuUsagePercent();

    // Assert
    cpuUsage.Should().BeGreaterThanOrEqualTo(0.0);
  }

  [Fact]
  public void GetMemoryUsageBytes_ShouldReturnPositiveValue()
  {
    // Act
    var memoryUsage = monitor.GetMemoryUsageBytes();

    // Assert
    memoryUsage.Should().BeGreaterThan(0);
  }

  [Fact]
  public void GetMemoryUsageMB_ShouldReturnPositiveValue()
  {
    // Act
    var memoryUsageMB = monitor.GetMemoryUsageMB();

    // Assert
    memoryUsageMB.Should().BeGreaterThan(0);
  }

  [Fact]
  public void GetThreadCount_ShouldReturnPositiveValue()
  {
    // Act
    var threadCount = monitor.GetThreadCount();

    // Assert
    threadCount.Should().BeGreaterThan(0);
  }

  [Fact]
  public void GetSnapshot_ShouldReturnValidSnapshot()
  {
    // Act
    var snapshot = monitor.GetSnapshot();

    // Assert
    snapshot.Should().NotBeNull();
    snapshot.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    snapshot.CpuUsagePercent.Should().BeGreaterThanOrEqualTo(0.0);
    snapshot.MemoryUsageMB.Should().BeGreaterThan(0);
    snapshot.ThreadCount.Should().BeGreaterThan(0);
  }

  [Fact]
  public void PerformanceSnapshot_ToString_ShouldReturnFormattedString()
  {
    // Arrange
    var snapshot = new PerformanceSnapshot
    {
      Timestamp = DateTime.UtcNow,
      CpuUsagePercent = 15.5,
      MemoryUsageMB = 128.75,
      ThreadCount = 10
    };

    // Act
    var result = snapshot.ToString();

    // Assert
    result.Should().Contain("CPU:");
    result.Should().Contain("Memory:");
    result.Should().Contain("Threads:");
    result.Should().Contain("15.50");
    result.Should().Contain("128.75");
    result.Should().Contain("10");
  }

  [Fact]
  public async Task MonitorAsync_ShouldYieldSnapshots()
  {
    // Arrange
    using var cts = new CancellationTokenSource();
    var snapshots = new List<PerformanceSnapshot>();
    var maxSnapshots = 3;

    // Act
    await foreach (var snapshot in monitor.MonitorAsync(100, cts.Token))
    {
      snapshots.Add(snapshot);
      if (snapshots.Count >= maxSnapshots)
      {
        cts.Cancel();
        break;
      }
    }

    // Assert
    snapshots.Should().HaveCount(maxSnapshots);
    snapshots.Should().OnlyContain(s => s.MemoryUsageMB > 0);
  }

  public void Dispose()
  {
    monitor?.Dispose();
  }
}
