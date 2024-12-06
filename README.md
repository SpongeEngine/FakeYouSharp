# FakeYou.NET

[![NuGet](https://img.shields.io/nuget/v/FakeYou.NET.svg)](https://www.nuget.org/packages/FakeYou.NET)
[![NuGet Downloads](https://img.shields.io/nuget/dt/FakeYou.NET.svg)](https://www.nuget.org/packages/FakeYou.NET)
[![License](https://img.shields.io/github/license/SpongeEngine/FakeYou.NET)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-6.0%20%7C%207.0%20%7C%208.0%2B-512BD4)](https://dotnet.microsoft.com/download)
[![Tests](https://github.com/SpongeEngine/FakeYou.NET/actions/workflows/test.yml/badge.svg)](https://github.com/SpongeEngine/FakeYou.NET/actions/workflows/test.yml)

A modern .NET client library for the FakeYou text-to-speech API. This library provides a simple and efficient way to interact with FakeYou's TTS services, including voice model management and audio generation.

📦 [View Package on NuGet](https://www.nuget.org/packages/FakeYou.NET)

## Features

- Easy-to-use API for text-to-speech generation
- Configurable retry policies for improved reliability
- Comprehensive audio format handling
- Full async/await support
- Strong type safety
- Extensive logging capabilities

## Installation

Install FakeYou.NET via NuGet:

```bash
dotnet add package FakeYou.NET
```

## Quick Start

```csharp
using FakeYou.NET.Client;

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

## Advanced Usage

### Custom Configuration

```csharp
var client = new FakeYouClient(options =>
{
    options.ApiKey = "your_api_key";
    options.MaxRetryAttempts = 3;
    options.RetryDelay = TimeSpan.FromSeconds(2);
    options.Logger = yourLoggerInstance;
});
```

### Progress Tracking

```csharp
client.OnProgress += (progress) =>
{
    Console.WriteLine($"Status: {progress.State} - {progress.Message}");
};
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

Contributions are welcome! Please feel free to submit a Pull Request.

## Support

For issues and feature requests, please use the GitHub issues page.
