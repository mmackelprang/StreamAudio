using SoundFlow.Structs;

namespace StreamAudio.Core.Audio;

/// <summary>
/// Provides sample rate conversion utilities for audio streams.
/// Detects sample rate mismatches and provides resampling when needed.
/// </summary>
public static class SampleRateConverter
{
  /// <summary>
  /// Checks if two audio formats have matching sample rates.
  /// </summary>
  /// <param name="format1">First audio format.</param>
  /// <param name="format2">Second audio format.</param>
  /// <returns>True if sample rates match, false otherwise.</returns>
  public static bool HasMatchingSampleRate(AudioFormat format1, AudioFormat format2)
  {
    return format1.SampleRate == format2.SampleRate;
  }

  /// <summary>
  /// Checks if two audio formats are compatible for mixing.
  /// Compatible formats have the same sample rate, channel count, and format.
  /// </summary>
  /// <param name="format1">First audio format.</param>
  /// <param name="format2">Second audio format.</param>
  /// <returns>True if formats are compatible, false otherwise.</returns>
  public static bool AreFormatsCompatible(AudioFormat format1, AudioFormat format2)
  {
    return format1.SampleRate == format2.SampleRate &&
           format1.Channels == format2.Channels &&
           format1.Format == format2.Format;
  }

  /// <summary>
  /// Gets a recommended audio format for mixing multiple sources.
  /// Uses the highest sample rate among the sources for best quality.
  /// </summary>
  /// <param name="formats">Array of audio formats to consider.</param>
  /// <returns>Recommended audio format for mixing.</returns>
  public static AudioFormat GetRecommendedMixingFormat(params AudioFormat[] formats)
  {
    if (formats == null || formats.Length == 0)
    {
      // Return default DVD HQ quality
      return AudioFormat.DvdHq;
    }

    // Find the highest sample rate
    var maxSampleRate = formats.Max(f => f.SampleRate);

    // Use the format from the source with the highest sample rate
    var sourceFormat = formats.First(f => f.SampleRate == maxSampleRate);

    return sourceFormat;
  }

  /// <summary>
  /// Validates that an audio source can be mixed with the specified target format.
  /// Note: SoundFlow handles resampling automatically when formats don't match,
  /// but this method helps identify potential quality issues.
  /// </summary>
  /// <param name="sourceFormat">The source audio format.</param>
  /// <param name="targetFormat">The target mixing format.</param>
  /// <returns>Validation result with any warnings or issues.</returns>
  public static ValidationResult ValidateForMixing(AudioFormat sourceFormat, AudioFormat targetFormat)
  {
    var result = new ValidationResult { IsValid = true };

    if (sourceFormat.SampleRate != targetFormat.SampleRate)
    {
      result.Warnings.Add(
        $"Sample rate mismatch: source is {sourceFormat.SampleRate} Hz, target is {targetFormat.SampleRate} Hz. " +
        "Automatic resampling will occur, which may affect audio quality.");
    }

    if (sourceFormat.Channels != targetFormat.Channels)
    {
      result.Warnings.Add(
        $"Channel count mismatch: source has {sourceFormat.Channels} channels, target has {targetFormat.Channels}. " +
        "Automatic channel conversion will occur.");
    }

    if (sourceFormat.Format != targetFormat.Format)
    {
      result.Warnings.Add(
        $"Format mismatch: source is {sourceFormat.Format}, target is {targetFormat.Format}. " +
        "Automatic format conversion will occur.");
    }

    return result;
  }
}

/// <summary>
/// Result of audio format validation.
/// </summary>
public class ValidationResult
{
  /// <summary>
  /// Gets or sets whether the validation passed.
  /// </summary>
  public bool IsValid { get; set; }

  /// <summary>
  /// Gets the list of warnings found during validation.
  /// </summary>
  public List<string> Warnings { get; } = new();

  /// <summary>
  /// Gets whether there are any warnings.
  /// </summary>
  public bool HasWarnings => Warnings.Count > 0;
}
