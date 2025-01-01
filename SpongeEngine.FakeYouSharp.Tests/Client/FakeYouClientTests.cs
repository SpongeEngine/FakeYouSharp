using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using SpongeEngine.FakeYouSharp.Client;
using SpongeEngine.FakeYouSharp.Models;
using SpongeEngine.FakeYouSharp.Models.Configuration;
using WireMock.Logging;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace SpongeEngine.FakeYouSharp.Tests.Client
{
    public class FakeYouClientTests : IDisposable
    {
        private readonly WireMockServer _mockServer;
        private readonly FakeYouClient _client;
        private readonly Mock<ILogger> _loggerMock;

        public FakeYouClientTests()
        {
            _mockServer = WireMockServer.Start();
            _loggerMock = new Mock<ILogger>();

            var options = new FakeYouOptions
            {
                Logger = _loggerMock.Object,
                ApiKey = "test_key",
                MaxRetryAttempts = 3,
                BaseUrl = _mockServer.Urls[0],
                CdnUrl = _mockServer.Urls[0]
            };

            _client = new FakeYouClient(opt =>
            {
                opt.ApiKey = options.ApiKey;
                opt.Logger = options.Logger;
                opt.MaxRetryAttempts = options.MaxRetryAttempts;
                opt.BaseUrl = options.BaseUrl;
                opt.CdnUrl = options.CdnUrl;
                opt.RetryDelay = TimeSpan.FromMilliseconds(100); // Short delay for tests
            });
        }

        public async Task GenerateAudioAsync_Success_ReturnsAudioData()
        {
            // Arrange
            var modelToken = "TM:1234";
            var text = "Test text";
            var jobToken = "job_123";
            var audioData = CreateValidWavData();

            // Log all requests for debugging
            _mockServer.AllowPartialMapping();
            _mockServer.LogEntriesChanged += (sender, args) =>
            {
                var entry = args.NewItems.Cast<LogEntry>().FirstOrDefault();
                if (entry != null)
                {
                    _loggerMock.Object.LogInformation(
                        "Request: {Method} {Path} -> {StatusCode}",
                        entry.RequestMessage.Method,
                        entry.RequestMessage.Path,
                        entry.ResponseMessage.StatusCode);
                }
            };

            // Setup TTS inference request
            _mockServer
                .Given(Request.Create()
                    .WithPath("/tts/inference")
                    .UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithBody(JsonConvert.SerializeObject(new
                    {
                        success = true,
                        inference_job_token = jobToken
                    })));

            // Setup job status endpoint - Initial status
            _mockServer
                .Given(Request.Create()
                    .WithPath("/tts/job/" + jobToken)
                    .UsingGet())
                .InScenario("Job Processing")
                .WillSetStateTo("Processing")
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithBody(JsonConvert.SerializeObject(new
                    {
                        success = true,
                        state = new
                        {
                            status = "pending",
                            maybe_extra_status_description = "Processing"
                        }
                    })));

            // Setup job status endpoint - Complete status
            _mockServer
                .Given(Request.Create()
                    .WithPath("/tts/job/" + jobToken)
                    .UsingGet())
                .InScenario("Job Processing")
                .WhenStateIs("Processing")
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithBody(JsonConvert.SerializeObject(new
                    {
                        success = true,
                        state = new
                        {
                            status = "complete_success",
                            maybe_public_bucket_wav_audio_path = "/audio/test.wav"
                        }
                    })));

            // Setup audio file download
            _mockServer
                .Given(Request.Create()
                    .WithPath("/audio/test.wav")
                    .UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithBody(audioData));

            try
            {
                // Act
                var result = await _client.GenerateAudioAsync(modelToken, text);

                // Assert
                result.Should().NotBeNull();
                result.Should().BeEquivalentTo(audioData);
            }
            catch (Exception ex)
            {
                // Log all requests that were made
                var requests = _mockServer.LogEntries;
                _loggerMock.Object.LogError("Test failed. Requests made:");
                foreach (var request in requests)
                {
                    _loggerMock.Object.LogError(
                        "Request: {Method} {Path} -> {StatusCode}",
                        request.RequestMessage.Method,
                        request.RequestMessage.Path,
                        request.ResponseMessage.StatusCode);
                }
                throw;
            }
        }

        [Fact]
        public async Task GenerateAudioAsync_Failed_ThrowsException()
        {
            // Arrange
            var modelToken = "TM:1234";
            var text = "Test text";

            _mockServer
                .Given(Request.Create()
                    .WithPath("/tts/inference")
                    .UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(500)
                    .WithBody("Internal Server Error"));

            // Act & Assert
            await _client.Invoking(c => c.GenerateAudioAsync(modelToken, text))
                .Should()
                .ThrowAsync<FakeYouSharpException>()
                .Where(ex => ex.StatusCode == 500);
        }

        private byte[] CreateValidWavData()
        {
            var headerSize = 44;
            var dataSize = 1024;
            var totalSize = headerSize + dataSize;
        
            var data = new byte[totalSize];
        
            // RIFF header
            Encoding.ASCII.GetBytes("RIFF").CopyTo(data, 0);
            BitConverter.GetBytes(totalSize - 8).CopyTo(data, 4);
            Encoding.ASCII.GetBytes("WAVE").CopyTo(data, 8);
        
            // fmt chunk
            Encoding.ASCII.GetBytes("fmt ").CopyTo(data, 12);
            BitConverter.GetBytes(16).CopyTo(data, 16);
            BitConverter.GetBytes((short)1).CopyTo(data, 20);
            BitConverter.GetBytes((short)2).CopyTo(data, 22);
            BitConverter.GetBytes(44100).CopyTo(data, 24);
            BitConverter.GetBytes(176400).CopyTo(data, 28);
            BitConverter.GetBytes((short)4).CopyTo(data, 32);
            BitConverter.GetBytes((short)16).CopyTo(data, 34);
        
            // data chunk
            Encoding.ASCII.GetBytes("data").CopyTo(data, 36);
            BitConverter.GetBytes(dataSize).CopyTo(data, 40);
        
            // Simple sine wave data
            for (int i = 0; i < dataSize; i += 2)
            {
                var t = (double)i / 44100;
                var amplitude = (short)(32760 * Math.Sin(2 * Math.PI * 440 * t));
                var bytes = BitConverter.GetBytes(amplitude);
                bytes.CopyTo(data, headerSize + i);
            }
        
            return data;
        }

        public void Dispose()
        {
            _mockServer?.Dispose();
            _client?.Dispose();
        }
    }
}