using Microsoft.AspNetCore.Mvc;
using StreamAudio.Core.Devices;

namespace StreamAudio.Api.Controllers;

/// <summary>
/// API endpoints for device management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DevicesController : ControllerBase
{
  private readonly DeviceManager _deviceManager;
  private readonly ILogger<DevicesController> _logger;

  public DevicesController(ILogger<DevicesController> logger)
  {
    _logger = logger;
    _deviceManager = new DeviceManager();
  }

  /// <summary>
  /// Get all available audio input sources
  /// </summary>
  [HttpGet("sources")]
  public async Task<IActionResult> GetAudioSources()
  {
    try
    {
      var sources = await _deviceManager.GetAudioSourcesAsync();
      return Ok(new { success = true, sources });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to get audio sources");
      return BadRequest(new { success = false, error = ex.Message });
    }
  }

  /// <summary>
  /// Get all available audio playback devices
  /// </summary>
  [HttpGet("playback")]
  public async Task<IActionResult> GetPlaybackDevices()
  {
    try
    {
      var devices = await _deviceManager.GetAudioPlaybackDevicesAsync();
      return Ok(new { success = true, devices });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to get playback devices");
      return BadRequest(new { success = false, error = ex.Message });
    }
  }

  /// <summary>
  /// Get all device configurations
  /// </summary>
  [HttpGet("configurations")]
  public async Task<IActionResult> GetConfigurations([FromQuery] string? category = null)
  {
    try
    {
      var configs = await _deviceManager.GetDeviceConfigurationsAsync(category);
      return Ok(new { success = true, configurations = configs });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to get device configurations");
      return BadRequest(new { success = false, error = ex.Message });
    }
  }

  /// <summary>
  /// Get a specific device configuration
  /// </summary>
  [HttpGet("configurations/{id}")]
  public async Task<IActionResult> GetConfiguration(string id)
  {
    try
    {
      var config = await _deviceManager.GetDeviceConfigurationAsync(id);
      if (config == null)
      {
        return NotFound(new { success = false, error = "Configuration not found" });
      }
      return Ok(new { success = true, configuration = config });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to get device configuration {Id}", id);
      return BadRequest(new { success = false, error = ex.Message });
    }
  }

  /// <summary>
  /// Save a device configuration
  /// </summary>
  [HttpPost("configurations")]
  public async Task<IActionResult> SaveConfiguration([FromBody] DeviceConfiguration config)
  {
    try
    {
      await _deviceManager.SaveDeviceConfigurationAsync(config);
      _logger.LogInformation("Saved device configuration {Id}", config.Id);
      return Ok(new { success = true, message = "Configuration saved successfully" });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to save device configuration");
      return BadRequest(new { success = false, error = ex.Message });
    }
  }

  /// <summary>
  /// Delete a device configuration
  /// </summary>
  [HttpDelete("configurations/{id}")]
  public async Task<IActionResult> DeleteConfiguration(string id)
  {
    try
    {
      await _deviceManager.DeleteDeviceConfigurationAsync(id);
      _logger.LogInformation("Deleted device configuration {Id}", id);
      return Ok(new { success = true, message = "Configuration deleted successfully" });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to delete device configuration {Id}", id);
      return BadRequest(new { success = false, error = ex.Message });
    }
  }

  /// <summary>
  /// Create a TTS auto source
  /// </summary>
  [HttpPost("auto/tts")]
  public IActionResult CreateTtsAutoSource([FromBody] AutoSourceConfiguration config)
  {
    try
    {
      var source = _deviceManager.CreateTtsAutoSource(config);
      _logger.LogInformation("Created TTS auto source");
      return Ok(new { success = true, sourceName = source.Name });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to create TTS auto source");
      return BadRequest(new { success = false, error = ex.Message });
    }
  }

  /// <summary>
  /// Create a file auto source
  /// </summary>
  [HttpPost("auto/file")]
  public IActionResult CreateFileAutoSource([FromBody] AutoSourceConfiguration config)
  {
    try
    {
      var source = _deviceManager.CreateFileAutoSource(config);
      _logger.LogInformation("Created file auto source from {Content}", config.Content);
      return Ok(new { success = true, sourceName = source.Name });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to create file auto source");
      return BadRequest(new { success = false, error = ex.Message });
    }
  }
}
