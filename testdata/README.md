# Test Data Files

This directory contains generated audio test files used for unit and integration testing.

## Generating Test Files

Use the ToneGenerator tool to create test audio files:

```bash
dotnet run --project tools/ToneGenerator/ToneGenerator.csproj -- <frequency> <duration> <format> <output-file>
```

### Standard Test Files

The following test files should be generated for testing:

- `50hz.wav` - 50 Hz sine wave, 1 second duration
- `100hz.wav` - 100 Hz sine wave, 1 second duration
- `200hz.wav` - 200 Hz sine wave, 1 second duration

To generate all standard test files:

```bash
dotnet run --project tools/ToneGenerator/ToneGenerator.csproj -- 50 1 WAV testdata/50hz.wav
dotnet run --project tools/ToneGenerator/ToneGenerator.csproj -- 100 1 WAV testdata/100hz.wav
dotnet run --project tools/ToneGenerator/ToneGenerator.csproj -- 200 1 WAV testdata/200hz.wav
```

These files are used in the test suite to verify audio mixing, playback, and processing functionality.
