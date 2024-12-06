using Xunit;
using FluentAssertions;
using FakeYou.NET.Client;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Xunit.Abstractions;
using FakeYou.NET.Audio;

namespace FakeYou.NET.Tests.Integration
{
    [Collection("FakeYou Tests")]
    public class FakeYouClientIntegrationTests : IDisposable
    {
        private readonly ILogger<FakeYouClientIntegrationTests> _logger;
        private readonly FakeYouClient _client;
        private readonly CancellationTokenSource _cts;

        public FakeYouClientIntegrationTests(ITestOutputHelper output)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
                builder.AddConsole()
                    .AddXUnit(output)
                    .SetMinimumLevel(LogLevel.Debug));
                      
            _logger = loggerFactory.CreateLogger<FakeYouClientIntegrationTests>();
            _cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        
            _client = new FakeYouClient(options =>
            {
                options.Logger = _logger;
                options.Timeout = TimeSpan.FromSeconds(30);
                options.MaxRetryAttempts = 3;
                options.RetryDelay = TimeSpan.FromSeconds(2);
                options.ValidateResponseData = true;
            });

            // Initial delay before any tests
            Thread.Sleep(1000);
        }

        [Fact]
        public async Task GenerateAudioAsync_ProducesValidWavFile()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation("Starting audio generation test");

                // Get voice models
                var models = await _client.GetVoiceModelsAsync(_cts.Token);
                _logger.LogInformation($"Retrieved {models.Count} models");

                // Use a known working model token for testing
                const string TEST_MODEL_TOKEN = "TM:8j2n9yj86f4y"; // Example SpongeBob model token
                var voice = models.FirstOrDefault(m => m.ModelToken == TEST_MODEL_TOKEN) 
                    ?? models.First();

                _logger.LogInformation("Selected voice model: {@Voice}", 
                    new { voice.ModelToken, voice.Title });

                var testText = "This is a test of text to speech.";
                
                _logger.LogInformation($"Generating audio with voice token: {voice.ModelToken}");
                
                var audioData = await _client.GenerateAudioAsync(
                    voice.ModelToken, 
                    testText, 
                    _cts.Token
                );

                audioData.Should().NotBeNull();
                audioData.Length.Should().BeGreaterThan(44, "WAV file should be larger than header size");

                // Read WAV header information
                var processor = new AudioProcessor(_logger);
                processor.ValidateWavFormat(audioData).Should().BeTrue("Should be valid WAV format");

                // Get format details directly from the WAV header
                var sampleRate = BitConverter.ToInt32(audioData, 24);
                var channels = BitConverter.ToUInt16(audioData, 22);
                var bitsPerSample = BitConverter.ToUInt16(audioData, 34);
                var dataSize = BitConverter.ToInt32(audioData, 40);

                _logger.LogInformation("Generated audio format:");
                _logger.LogInformation($"- Sample Rate: {sampleRate}");
                _logger.LogInformation($"- Channels: {channels}");
                _logger.LogInformation($"- Bits Per Sample: {bitsPerSample}");
                _logger.LogInformation($"- Audio Length: {dataSize} bytes");

                // Verify format
                var formatCode = BitConverter.ToInt16(audioData, 20);
                formatCode.Should().Be(1, "Should be PCM format");
                bitsPerSample.Should().Be(16);
                channels.Should().BeInRange(1, 2);
                sampleRate.Should().BeOneOf(44100, 48000);

                var outputPath = Path.Combine(Path.GetTempPath(), "fakeyou_test_output.wav");
                await File.WriteAllBytesAsync(outputPath, audioData);
                _logger.LogInformation($"Saved test audio to: {outputPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Test failed after {sw.ElapsedMilliseconds}ms");
                throw;
            }
        }

        public void Dispose()
        {
            try
            {
                _cts?.Dispose();
                _client?.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during test cleanup");
            }
        }
    }
}