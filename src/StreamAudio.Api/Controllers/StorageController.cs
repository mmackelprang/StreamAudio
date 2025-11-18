using Microsoft.AspNetCore.Mvc;
using StreamAudio.Core.Storage;

namespace StreamAudio.Api.Controllers;

/// <summary>
/// API endpoints for storage management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StorageController : ControllerBase
{
  private readonly IStorage _storage;
  private readonly ILogger<StorageController> _logger;

  public StorageController(ILogger<StorageController> logger)
  {
    _logger = logger;
    _storage = StorageManager.Instance.Storage;
  }

  /// <summary>
  /// Save data to storage
  /// </summary>
  [HttpPost("{table}/{key}")]
  public async Task<IActionResult> Save(string table, string key, [FromBody] object data)
  {
    try
    {
      await _storage.SaveAsync(table, key, data);
      _logger.LogInformation("Saved data to {Table}:{Key}", table, key);
      return Ok(new { success = true, message = "Data saved successfully" });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to save data to {Table}:{Key}", table, key);
      return BadRequest(new { success = false, error = ex.Message });
    }
  }

  /// <summary>
  /// Load data from storage
  /// </summary>
  [HttpGet("{table}/{key}")]
  public async Task<IActionResult> Load(string table, string key)
  {
    try
    {
      var data = await _storage.LoadAsync<object>(table, key);
      if (data == null)
      {
        return NotFound(new { success = false, error = "Data not found" });
      }
      return Ok(new { success = true, data });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to load data from {Table}:{Key}", table, key);
      return BadRequest(new { success = false, error = ex.Message });
    }
  }

  /// <summary>
  /// Check if data exists
  /// </summary>
  [HttpHead("{table}/{key}")]
  [HttpGet("{table}/{key}/exists")]
  public async Task<IActionResult> Exists(string table, string key)
  {
    try
    {
      var exists = await _storage.ExistsAsync(table, key);
      return Ok(new { success = true, exists });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to check existence for {Table}:{Key}", table, key);
      return BadRequest(new { success = false, error = ex.Message });
    }
  }

  /// <summary>
  /// Delete data from storage
  /// </summary>
  [HttpDelete("{table}/{key}")]
  public async Task<IActionResult> Delete(string table, string key)
  {
    try
    {
      await _storage.DeleteAsync(table, key);
      _logger.LogInformation("Deleted {Table}:{Key}", table, key);
      return Ok(new { success = true, message = "Data deleted successfully" });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to delete {Table}:{Key}", table, key);
      return BadRequest(new { success = false, error = ex.Message });
    }
  }

  /// <summary>
  /// Get all keys from a table
  /// </summary>
  [HttpGet("{table}/keys")]
  public async Task<IActionResult> GetKeys(string table)
  {
    try
    {
      var keys = await _storage.GetKeysAsync(table);
      return Ok(new { success = true, keys });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to get keys from {Table}", table);
      return BadRequest(new { success = false, error = ex.Message });
    }
  }

  /// <summary>
  /// Get all data from a table
  /// </summary>
  [HttpGet("{table}/all")]
  public async Task<IActionResult> GetAll(string table)
  {
    try
    {
      var data = await _storage.GetAllAsync<object>(table);
      return Ok(new { success = true, data });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to get all data from {Table}", table);
      return BadRequest(new { success = false, error = ex.Message });
    }
  }

  /// <summary>
  /// Get all table names
  /// </summary>
  [HttpGet("tables")]
  public async Task<IActionResult> GetTables()
  {
    try
    {
      var tables = await _storage.GetTablesAsync();
      return Ok(new { success = true, tables });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to get tables");
      return BadRequest(new { success = false, error = ex.Message });
    }
  }

  /// <summary>
  /// Create a backup of all storage data
  /// </summary>
  [HttpPost("backup")]
  public async Task<IActionResult> Backup()
  {
    try
    {
      var backupPath = await _storage.BackupAsync();
      _logger.LogInformation("Created backup at {BackupPath}", backupPath);
      return Ok(new { success = true, backupPath });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to create backup");
      return BadRequest(new { success = false, error = ex.Message });
    }
  }

  /// <summary>
  /// Restore storage from a backup
  /// </summary>
  [HttpPost("restore")]
  public async Task<IActionResult> Restore([FromBody] RestoreRequest request)
  {
    try
    {
      await _storage.RestoreAsync(request.BackupPath);
      _logger.LogInformation("Restored from backup {BackupPath}", request.BackupPath);
      return Ok(new { success = true, message = "Storage restored successfully" });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to restore from backup");
      return BadRequest(new { success = false, error = ex.Message });
    }
  }
}

public class RestoreRequest
{
  public string BackupPath { get; set; } = string.Empty;
}
