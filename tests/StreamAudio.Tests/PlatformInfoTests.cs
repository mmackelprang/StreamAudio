using StreamAudio.Core.Platform;
using Xunit;
using FluentAssertions;

namespace StreamAudio.Tests;

public class PlatformInfoTests
{
  [Fact]
  public void PlatformInfo_ShouldIdentifyOperatingSystem()
  {
    // At least one platform should be identified
    var isAnyPlatform = PlatformInfo.IsWindows || 
                       PlatformInfo.IsLinux || 
                       PlatformInfo.IsMacOS;
    
    isAnyPlatform.Should().BeTrue("at least one platform should be identified");
  }

  [Fact]
  public void PlatformName_ShouldReturnValidName()
  {
    var platformName = PlatformInfo.PlatformName;
    
    platformName.Should().NotBeNullOrEmpty();
    platformName.Should().BeOneOf("Windows", "Linux", "macOS", "Raspberry Pi", "Unknown");
  }

  [Fact]
  public void OSDescription_ShouldReturnDescription()
  {
    var osDescription = PlatformInfo.OSDescription;
    
    osDescription.Should().NotBeNullOrEmpty();
  }

  [Fact]
  public void ProcessArchitecture_ShouldBeValid()
  {
    var arch = PlatformInfo.ProcessArchitecture;
    
    arch.Should().NotBe(System.Runtime.InteropServices.Architecture.Wasm);
  }

  [Fact]
  public void IsRaspberryPi_ShouldNotThrow()
  {
    // This should not throw even if files don't exist
    var action = () => _ = PlatformInfo.IsRaspberryPi;
    
    action.Should().NotThrow();
  }
}
