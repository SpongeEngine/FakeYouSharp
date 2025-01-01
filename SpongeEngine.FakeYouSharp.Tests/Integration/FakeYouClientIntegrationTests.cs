using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using SpongeEngine.FakeYouSharp.Audio;
using SpongeEngine.FakeYouSharp.Client;
using Xunit;
using Xunit.Abstractions;

namespace SpongeEngine.FakeYouSharp.Tests.Integration
{
    [Collection("FakeYou Tests")]
    public class FakeYouClientIntegrationTests : IDisposable
    {
        private readonly ILogger<FakeYouClientIntegrationTests> _logger;
        private readonly FakeYouClient _client;
        private readonly WavProcessor _wavProcessor;
        private readonly CancellationTokenSource _cts;

        // Common audio sample rates in Hz
        private readonly int[] ValidSampleRates = { 32000, 44100, 48000 };

        public FakeYouClientIntegrationTests(ITestOutputHelper output)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
                builder.AddConsole()
                    .AddXUnit(output)
                    .SetMinimumLevel(LogLevel.Debug));
                      
            _logger = loggerFactory.CreateLogger<FakeYouClientIntegrationTests>();
            _cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            _wavProcessor = new WavProcessor();
        
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

                _logger.LogInformation("Selected voice model: {Title} ({Token})", 
                    voice.Title, voice.ModelToken);

                var testText = "This is a test of text to speech.";
                
                _logger.LogInformation("Generating audio with voice token: {Token}", voice.ModelToken);
                
                var audioData = await _client.GenerateAudioAsync(
                    voice.ModelToken, 
                    testText, 
                    _cts.Token
                );

                // Validate audio data
                audioData.Should().NotBeNull();
                audioData.Length.Should().BeGreaterThan(44, "WAV file should be larger than header size");

                // Verify WAV format
                var format = _wavProcessor.GetWavFormat(audioData);
                
                _logger.LogInformation("Generated audio format: {SampleRate}Hz {BitsPerSample}-bit {Channels}ch", 
                    format.SampleRate,
                    format.BitsPerSample,
                    format.Channels);

                // Verify format meets our requirements
                format.AudioFormat.Should().Be(1, "Should be PCM format");
                format.BitsPerSample.Should().Be(16, "Should be 16-bit");
                format.Channels.Should().BeInRange(1, 2, "Should be mono or stereo");
                format.SampleRate.Should().BeOneOf(ValidSampleRates, "Should be a standard sample rate");

                // Save test output if SAVE_TEST_OUTPUT environment variable is set
                if (Environment.GetEnvironmentVariable("SAVE_TEST_OUTPUT") == "1")
                {
                    var outputPath = Path.Combine(Path.GetTempPath(), "fakeyou_test_output.wav");
                    await File.WriteAllBytesAsync(outputPath, audioData);
                    _logger.LogInformation("Saved test audio to: {Path}", outputPath);
                }

                sw.Stop();
                _logger.LogInformation("Test completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Test failed after {ElapsedMs}ms", sw.ElapsedMilliseconds);
                throw;
            }
        }

        [Fact]
        public async Task GenerateAudioAsync_ProcessesMultipleFormats()
        {
            // Get a few different voice models
            var models = await _client.GetVoiceModelsAsync(_cts.Token);
            var testModels = models.Take(2).ToList();
            var testText = "Testing multiple formats.";

            foreach (var model in testModels)
            {
                _logger.LogInformation("Testing model: {Title} ({Token})", 
                    model.Title, model.ModelToken);

                var audioData = await _client.GenerateAudioAsync(
                    model.ModelToken,
                    testText,
                    _cts.Token
                );

                var format = _wavProcessor.GetWavFormat(audioData);
                _logger.LogInformation("Audio format: {SampleRate}Hz {BitsPerSample}-bit {Channels}ch", 
                    format.SampleRate,
                    format.BitsPerSample,
                    format.Channels);

                // Verify common requirements across all models
                format.AudioFormat.Should().Be(1, "All formats should be PCM");
                format.BitsPerSample.Should().Be(16, "All formats should be 16-bit");
                format.Channels.Should().BeInRange(1, 2, "Should be mono or stereo");
                format.SampleRate.Should().BeOneOf(ValidSampleRates, "Should be a standard sample rate");
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