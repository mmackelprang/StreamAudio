namespace StreamAudio.Core.Events;

/// <summary>
/// Event arguments for audio-related events.
/// </summary>
public class AudioEventArgs : EventArgs
{
  /// <summary>
  /// Gets the stream ID associated with this event.
  /// </summary>
  public string StreamId { get; init; }

  /// <summary>
  /// Gets the error message, if any.
  /// </summary>
  public string? Message { get; init; }

  /// <summary>
  /// Gets the exception, if any.
  /// </summary>
  public Exception? Exception { get; init; }

  public AudioEventArgs(string streamId, string? message = null, Exception? exception = null)
  {
    StreamId = streamId ?? throw new ArgumentNullException(nameof(streamId));
    Message = message;
    Exception = exception;
  }
}

/// <summary>
/// Event arguments for device-related events.
/// </summary>
public class DeviceEventArgs : EventArgs
{
  /// <summary>
  /// Gets the device name.
  /// </summary>
  public string DeviceName { get; init; }

  /// <summary>
  /// Gets the error message, if any.
  /// </summary>
  public string? Message { get; init; }

  /// <summary>
  /// Gets the exception, if any.
  /// </summary>
  public Exception? Exception { get; init; }

  public DeviceEventArgs(string deviceName, string? message = null, Exception? exception = null)
  {
    DeviceName = deviceName ?? throw new ArgumentNullException(nameof(deviceName));
    Message = message;
    Exception = exception;
  }
}
