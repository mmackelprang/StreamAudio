using Microsoft.AspNetCore.Mvc;
using StreamAudio.Core.Playback;
using StreamAudio.Core.Sources;
using SoundFlow.Structs;

namespace StreamAudio.Api.Controllers;

/// <summary>
/// API endpoints for stream management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StreamsController : ControllerBase
{
  private static StreamManager? _streamManager;
  private readonly ILogger<StreamsController> _logger;

  public StreamsController(ILogger<StreamsController> logger)
  {
    _logger = logger;
  }

  /// <summary>
  /// Initialize the StreamManager with a playback device
  /// </summary>
  [HttpPost("initialize")]
  public IActionResult Initialize([FromBody] InitializeRequest? request = null)
  {
    try
    {
      if (_streamManager != null)
      {
        return BadRequest(new { success = false, error = "StreamManager already initialized. Call shutdown first." });
      }

      // Create playback device with specified format or default
      AudioFormat? format = null;
      if (request?.SampleRate != null && request?.Channels != null)
      {
        format = new AudioFormat(request.SampleRate.Value, request.Channels.Value);
      }

      var playback = new AudioPlayback(format);
      _streamManager = new StreamManager(playback);

      if (request?.BackgroundVolume != null)
      {
        _streamManager.BackgroundVolume = request.BackgroundVolume.Value;
      }

      if (request?.MaxStreamDuration != null)
      {
        _streamManager.MaxStreamDuration = request.MaxStreamDuration.Value;
      }

      _logger.LogInformation("StreamManager initialized");
      return Ok(new { success = true, message = "StreamManager initialized successfully" });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to initialize StreamManager");
      return BadRequest(new { success = false, error = ex.Message });
    }
  }

  /// <summary>
  /// Shutdown the StreamManager
  /// </summary>
  [HttpPost("shutdown")]
  public IActionResult Shutdown()
  {
    try
    {
      if (_streamManager == null)
      {
        return BadRequest(new { success = false, error = "StreamManager not initialized" });
      }

      _streamManager.Dispose();
      _streamManager = null;

      _logger.LogInformation("StreamManager shutdown");
      return Ok(new { success = true, message = "StreamManager shutdown successfully" });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to shutdown StreamManager");
      return BadRequest(new { success = false, error = ex.Message });
    }
  }

  /// <summary>
  /// Get StreamManager status
  /// </summary>
  [HttpGet("status")]
  public IActionResult GetStatus()
  {
    try
    {
      if (_streamManager == null)
      {
        return Ok(new
        {
          success = true,
          initialized = false,
          message = "StreamManager not initialized"
        });
      }

      return Ok(new
      {
        success = true,
        initialized = true,
        streamCount = _streamManager.StreamCount,
        primaryStreamId = _streamManager.PrimaryStreamId,
        backgroundVolume = _streamManager.BackgroundVolume,
        maxStreamDuration = _streamManager.MaxStreamDuration
      });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to get StreamManager status");
      return BadRequest(new { success = false, error = ex.Message });
    }
  }

  /// <summary>
  /// Add a file audio source to the stream
  /// </summary>
  [HttpPost("sources/file")]
  public IActionResult AddFileSource([FromBody] AddFileSourceRequest request)
  {
    try
    {
      EnsureInitialized();

      var format = _streamManager!.Format;
      var source = new FileAudioSource(request.FilePath, format);
      _streamManager.AddSource(request.StreamId, source, request.IsPrimary);

      if (request.AutoPlay)
      {
        _streamManager.Play(request.StreamId, request.FadeIn);
      }

      _logger.LogInformation("Added file source {StreamId} from {FilePath}", request.StreamId, request.FilePath);
      return Ok(new { success = true, message = "File source added successfully" });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to add file source");
      return BadRequest(new { success = false, error = ex.Message });
    }
  }

  /// <summary>
  /// Remove a source from the stream
  /// </summary>
  [HttpDelete("sources/{streamId}")]
  public IActionResult RemoveSource(string streamId, [FromQuery] bool fadeOut = true)
  {
    try
    {
      EnsureInitialized();
      _streamManager!.RemoveSource(streamId, fadeOut);

      _logger.LogInformation("Removed source {StreamId}", streamId);
      return Ok(new { success = true, message = "Source removed successfully" });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to remove source {StreamId}", streamId);
      return BadRequest(new { success = false, error = ex.Message });
    }
  }

  /// <summary>
  /// Set the primary stream
  /// </summary>
  [HttpPost("primary/{streamId}")]
  public IActionResult SetPrimaryStream(string streamId)
  {
    try
    {
      EnsureInitialized();
      _streamManager!.SetPrimaryStream(streamId);

      _logger.LogInformation("Set primary stream to {StreamId}", streamId);
      return Ok(new { success = true, message = "Primary stream set successfully" });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to set primary stream {StreamId}", streamId);
      return BadRequest(new { success = false, error = ex.Message });
    }
  }

  /// <summary>
  /// Clear the primary stream
  /// </summary>
  [HttpDelete("primary")]
  public IActionResult ClearPrimaryStream()
  {
    try
    {
      EnsureInitialized();
      _streamManager!.ClearPrimaryStream();

      _logger.LogInformation("Cleared primary stream");
      return Ok(new { success = true, message = "Primary stream cleared successfully" });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to clear primary stream");
      return BadRequest(new { success = false, error = ex.Message });
    }
  }

  /// <summary>
  /// Play a stream
  /// </summary>
  [HttpPost("sources/{streamId}/play")]
  public IActionResult Play(string streamId, [FromQuery] bool fadeIn = false)
  {
    try
    {
      EnsureInitialized();
      _streamManager!.Play(streamId, fadeIn);

      _logger.LogInformation("Playing stream {StreamId}", streamId);
      return Ok(new { success = true, message = "Stream playing" });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to play stream {StreamId}", streamId);
      return BadRequest(new { success = false, error = ex.Message });
    }
  }

  /// <summary>
  /// Pause a stream
  /// </summary>
  [HttpPost("sources/{streamId}/pause")]
  public IActionResult Pause(string streamId)
  {
    try
    {
      EnsureInitialized();
      _streamManager!.Pause(streamId);

      _logger.LogInformation("Paused stream {StreamId}", streamId);
      return Ok(new { success = true, message = "Stream paused" });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to pause stream {StreamId}", streamId);
      return BadRequest(new { success = false, error = ex.Message });
    }
  }

  /// <summary>
  /// Stop a stream
  /// </summary>
  [HttpPost("sources/{streamId}/stop")]
  public IActionResult Stop(string streamId, [FromQuery] bool fadeOut = false)
  {
    try
    {
      EnsureInitialized();
      _streamManager!.Stop(streamId, fadeOut);

      _logger.LogInformation("Stopped stream {StreamId}", streamId);
      return Ok(new { success = true, message = "Stream stopped" });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to stop stream {StreamId}", streamId);
      return BadRequest(new { success = false, error = ex.Message });
    }
  }

  /// <summary>
  /// Mute a stream
  /// </summary>
  [HttpPost("sources/{streamId}/mute")]
  public IActionResult Mute(string streamId)
  {
    try
    {
      EnsureInitialized();
      _streamManager!.Mute(streamId);

      _logger.LogInformation("Muted stream {StreamId}", streamId);
      return Ok(new { success = true, message = "Stream muted" });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to mute stream {StreamId}", streamId);
      return BadRequest(new { success = false, error = ex.Message });
    }
  }

  /// <summary>
  /// Unmute a stream
  /// </summary>
  [HttpPost("sources/{streamId}/unmute")]
  public IActionResult Unmute(string streamId)
  {
    try
    {
      EnsureInitialized();
      _streamManager!.Unmute(streamId);

      _logger.LogInformation("Unmuted stream {StreamId}", streamId);
      return Ok(new { success = true, message = "Stream unmuted" });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to unmute stream {StreamId}", streamId);
      return BadRequest(new { success = false, error = ex.Message });
    }
  }

  /// <summary>
  /// Get mute status of a stream
  /// </summary>
  [HttpGet("sources/{streamId}/mute")]
  public IActionResult IsMuted(string streamId)
  {
    try
    {
      EnsureInitialized();
      var isMuted = _streamManager!.IsMuted(streamId);

      return Ok(new { success = true, isMuted });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to get mute status for stream {StreamId}", streamId);
      return BadRequest(new { success = false, error = ex.Message });
    }
  }

  /// <summary>
  /// Fade in a stream
  /// </summary>
  [HttpPost("sources/{streamId}/fadein")]
  public IActionResult FadeIn(string streamId, [FromQuery] int durationMs = 1000)
  {
    try
    {
      EnsureInitialized();
      _streamManager!.FadeIn(streamId, durationMs);

      _logger.LogInformation("Fading in stream {StreamId} over {Duration}ms", streamId, durationMs);
      return Ok(new { success = true, message = "Stream fading in" });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to fade in stream {StreamId}", streamId);
      return BadRequest(new { success = false, error = ex.Message });
    }
  }

  /// <summary>
  /// Fade out a stream
  /// </summary>
  [HttpPost("sources/{streamId}/fadeout")]
  public IActionResult FadeOut(string streamId, [FromQuery] int durationMs = 1000)
  {
    try
    {
      EnsureInitialized();
      _streamManager!.FadeOut(streamId, durationMs);

      _logger.LogInformation("Fading out stream {StreamId} over {Duration}ms", streamId, durationMs);
      return Ok(new { success = true, message = "Stream fading out" });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to fade out stream {StreamId}", streamId);
      return BadRequest(new { success = false, error = ex.Message });
    }
  }

  /// <summary>
  /// Get volume for a stream
  /// </summary>
  [HttpGet("sources/{streamId}/volume")]
  public IActionResult GetVolume(string streamId)
  {
    try
    {
      EnsureInitialized();
      var volume = _streamManager!.GetVolume(streamId);

      return Ok(new { success = true, volume });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to get volume for stream {StreamId}", streamId);
      return BadRequest(new { success = false, error = ex.Message });
    }
  }

  /// <summary>
  /// Set background volume for all non-primary streams
  /// </summary>
  [HttpPost("background-volume")]
  public IActionResult SetBackgroundVolume([FromBody] SetVolumeRequest request)
  {
    try
    {
      EnsureInitialized();
      _streamManager!.BackgroundVolume = request.Volume;

      _logger.LogInformation("Set background volume to {Volume}", request.Volume);
      return Ok(new { success = true, message = "Background volume set successfully" });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to set background volume");
      return BadRequest(new { success = false, error = ex.Message });
    }
  }

  /// <summary>
  /// Get background volume
  /// </summary>
  [HttpGet("background-volume")]
  public IActionResult GetBackgroundVolume()
  {
    try
    {
      EnsureInitialized();
      var volume = _streamManager!.BackgroundVolume;

      return Ok(new { success = true, volume });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to get background volume");
      return BadRequest(new { success = false, error = ex.Message });
    }
  }

  private void EnsureInitialized()
  {
    if (_streamManager == null)
    {
      throw new InvalidOperationException("StreamManager not initialized. Call /api/streams/initialize first.");
    }
  }
}

public class InitializeRequest
{
  public int? SampleRate { get; set; }
  public int? Channels { get; set; }
  public float? BackgroundVolume { get; set; }
  public int? MaxStreamDuration { get; set; }
}

public class AddFileSourceRequest
{
  public string StreamId { get; set; } = string.Empty;
  public string FilePath { get; set; } = string.Empty;
  public bool IsPrimary { get; set; }
  public bool AutoPlay { get; set; } = true;
  public bool FadeIn { get; set; }
}

public class SetVolumeRequest
{
  public float Volume { get; set; }
}
