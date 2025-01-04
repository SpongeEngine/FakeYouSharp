# FakeYouSharp
[![NuGet](https://img.shields.io/nuget/v/SpongeEngine.FakeYouSharp.svg)](https://www.nuget.org/packages/SpongeEngine.FakeYouSharp)
[![NuGet Downloads](https://img.shields.io/nuget/dt/SpongeEngine.FakeYouSharp.svg)](https://www.nuget.org/packages/SpongeEngine.FakeYouSharp)
[![Tests](https://github.com/SpongeEngine/FakeYouSharp/actions/workflows/test.yml/badge.svg)](https://github.com/SpongeEngine/FakeYouSharp/actions/workflows/test.yml)
[![License](https://img.shields.io/github/license/SpongeEngine/FakeYouSharp)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-6.0%20%7C%207.0%20%7C%208.0%2B-512BD4)](https://dotnet.microsoft.com/download)

C# client for the FakeYou TTS API.

📦 [View Package on NuGet](https://www.nuget.org/packages/SpongeEngine.FakeYouSharp)

## Features
- Easy-to-use API for text-to-speech generation
- Cross-platform WAV audio processing
- Built-in format conversion to 16-bit PCM
- Engine-agnostic design (works with Unity, Godot, and other frameworks)
- Configurable retry policies for improved reliability
- Full async/await support
- Comprehensive logging capabilities
- Strong type safety

## Installation
Install FakeYouSharp via NuGet:
```bash
dotnet add package SpongeEngine.FakeYouSharp
```

## Quick Start
```csharp
using FakeYouSharp.Client;

// Create a client instance
var client = new FakeYouClient(options =>
{
    options.ApiKey = "your_api_key"; // Optional
    options.Timeout = TimeSpan.FromMinutes(2);
});

// Generate speech
var modelToken = "TM:1234"; // Replace with actual model token
var text = "Hello, world!";
byte[] audioData = await client.GenerateAudioAsync(modelToken, text);

// List available voice models
var models = await client.GetVoiceModelsAsync();
foreach (var model in models)
{
    Console.WriteLine($"Voice: {model.Title} ({model.ModelToken})");
}
```

## Audio Format
FakeYouSharp automatically handles audio format conversion. The library:
- Accepts various input formats from FakeYou API
- Automatically converts to 16-bit PCM WAV
- Preserves original sample rate (typically 32kHz, 44.1kHz, or 48kHz)
- Maintains channel configuration (mono/stereo)

### Using with Different Frameworks

#### Unity
```csharp
byte[] audioData = await client.GenerateAudioAsync(modelToken, text);

// Convert to Unity AudioClip
var audioClip = AudioClip.Create("TTS", /* samples */ ...);
audioClip.SetData(/* your conversion code */);
```

#### Godot
```csharp
byte[] audioData = await client.GenerateAudioAsync(modelToken, text);

// Convert to Godot AudioStreamWav
var stream = new AudioStreamWav();
stream.Data = audioData;
stream.Format = AudioStreamWav.FormatEnum.Format16Bits;
// Set other properties as needed
```

#### Raw Audio Processing
```csharp
byte[] audioData = await client.GenerateAudioAsync(modelToken, text);

// Get format information
var wavProcessor = new WavProcessor();
var format = wavProcessor.GetWavFormat(audioData);
Console.WriteLine($"Sample Rate: {format.SampleRate}Hz");
Console.WriteLine($"Bits Per Sample: {format.BitsPerSample}");
Console.WriteLine($"Channels: {format.Channels}");
```

## Advanced Usage

### Custom Configuration
```csharp
var client = new FakeYouClient(options =>
{
    options.ApiKey = "your_api_key";
    options.MaxRetryAttempts = 3;
    options.RetryDelay = TimeSpan.FromSeconds(2);
    options.Logger = yourLoggerInstance;
    options.ValidateResponseData = true;
});
```

### Progress Tracking
```csharp
client.OnProgress += (progress) =>
{
    switch (progress.State)
    {
        case FakeYouProgressState.Starting:
            Console.WriteLine("Starting generation...");
            break;
        case FakeYouProgressState.Processing:
            Console.WriteLine($"Processing: {progress.Message}");
            break;
        case FakeYouProgressState.Complete:
            Console.WriteLine("Generation complete!");
            break;
    }
};
```

## Error Handling
```csharp
try
{
    var audioData = await client.GenerateAudioAsync(modelToken, text);
}
catch (FakeYouException ex)
{
    Console.WriteLine($"FakeYou API error: {ex.Message}");
    if (ex.StatusCode.HasValue)
    {
        Console.WriteLine($"Status code: {ex.StatusCode}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"General error: {ex.Message}");
}
```

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing
Contributions are welcome! Please feel free to submit a Pull Request.

Please see [CONTRIBUTING.md](CONTRIBUTING.md) for details on:
- How to publish to NuGet
- Development guidelines
- Code style
- Testing requirements
- Pull request process

## Support
For issues and feature requests, please use the [GitHub issues page](https://github.com/SpongeEngine/FakeYouSharp/issues).
