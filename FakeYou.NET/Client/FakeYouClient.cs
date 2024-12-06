using System.Net;
using System.Net.Http.Headers;
using FakeYou.NET.Audio;
using FakeYou.NET.Models.Configuration;
using FakeYou.NET.Models.Progress;
using FakeYou.NET.Models.Requests;
using FakeYou.NET.Models.Responses;
using FakeYou.NET.Models;
using FakeYou.NET.Policies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace FakeYou.NET.Client
{
    public class FakeYouClient : IFakeYouClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerSettings _jsonSettings;
        private readonly ILogger? _logger;
        private readonly RetryPolicy _retryPolicy;
        private readonly AudioProcessor _audioProcessor;
        private readonly FakeYouOptions _options;
        private bool _disposed;

        private const string BaseUrl = "https://api.fakeyou.com";
        private const string CdnUrl = "https://cdn-2.fakeyou.com";

        public event Action<FakeYouProgress>? OnProgress;

        public FakeYouClient(Action<FakeYouOptions>? configure = null)
        {
            _options = new FakeYouOptions();
            configure?.Invoke(_options);
            _logger = _options.Logger;

            _jsonSettings = _options.JsonSettings ?? new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            };

            _httpClient = CreateHttpClient();
            _retryPolicy = new RetryPolicy(_options.MaxRetryAttempts, _options.RetryDelay, _logger);
            _audioProcessor = new AudioProcessor(_logger);
        }

        private HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true
            };

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri(_options.BaseUrl),
                Timeout = _options.Timeout
            };

            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrEmpty(_options.ApiKey))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            }

            return client;
        }

        public async Task<byte[]> GenerateAudioAsync(string modelToken, string text, CancellationToken cancellationToken = default)
        {
            try
            {
                ReportProgress(FakeYouProgressState.Starting, "Starting generation...");

                var ttsRequest = new TtsRequest(modelToken, text, Guid.NewGuid().ToString("N"));
                var ttsResponse = await RequestTtsAsync(ttsRequest, cancellationToken);

                if (!ttsResponse.Success)
                    throw new FakeYouException("Failed to start TTS generation");

                ReportProgress(FakeYouProgressState.Queued, "Request queued...");

                var jobToken = ttsResponse.InferenceJobToken;
                var startTime = DateTime.UtcNow;
                var attempt = 0;

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    attempt++;

                    var jobResponse = await GetTtsJobStatusAsync(jobToken, cancellationToken);
                    
                    if (!jobResponse.Success)
                        throw new FakeYouException("Failed to get job status");

                    var status = jobResponse.State?.Status?.ToLower();
                    var message = jobResponse.State?.StatusDescription ?? status;
                    var elapsed = DateTime.UtcNow - startTime;

                    ReportProgress(FakeYouProgressState.Processing,
                        $"Processing: {message} ({elapsed.TotalSeconds:F1}s)", attempt);

                    _logger?.LogDebug("Job status: {Status}, Audio path: {Path}, Description: {Description}",
                        status, jobResponse.State?.AudioPath, message);

                    switch (status)
                    {
                        case "complete_success" when !string.IsNullOrEmpty(jobResponse.State?.AudioPath):
                            var audioUrl = $"{CdnUrl}{jobResponse.State.AudioPath}";
                            ReportProgress(FakeYouProgressState.Downloading, "Downloading audio...");
                            var audioData = await DownloadAudioAsync(audioUrl, cancellationToken);
                            
                            ReportProgress(FakeYouProgressState.Converting, "Processing audio format...");
                            var processedAudio = ProcessAudioData(audioData);
                            
                            ReportProgress(FakeYouProgressState.Complete, "Generation complete");
                            return processedAudio;

                        case "complete_failure" or "dead":
                            throw new FakeYouException("TTS generation failed: " + message);

                        default:
                            if (elapsed > _options.TtsTimeout)
                                throw new TimeoutException($"TTS generation timed out after {elapsed.TotalSeconds:F1}s");

                            await Task.Delay(_options.PollingInterval, cancellationToken);
                            continue;
                    }
                }
            }
            catch (Exception ex)
            {
                ReportProgress(FakeYouProgressState.Failed, $"Generation failed: {ex.Message}");
                throw;
            }
        }

        private async Task<string> GenerateAudioUrlAsync(string modelToken, string text,
            CancellationToken cancellationToken)
        {
            var ttsRequest = new TtsRequest(modelToken, text, Guid.NewGuid().ToString("N"));
            var ttsResponse = await RequestTtsAsync(ttsRequest, cancellationToken);

            if (!ttsResponse.Success)
                throw new FakeYouException("Failed to start TTS generation");

            var jobToken = ttsResponse.InferenceJobToken;
            var startTime = DateTime.UtcNow;
            var attempt = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                attempt++;

                var jobResponse = await GetTtsJobStatusAsync(jobToken, cancellationToken);
                if (!jobResponse.Success)
                    throw new FakeYouException("Failed to get job status");

                var status = jobResponse.State?.Status?.ToLower();
                var message = jobResponse.State?.StatusDescription ?? status;
                var elapsed = DateTime.UtcNow - startTime;

                ReportProgress(FakeYouProgressState.Processing,
                    $"Processing: {message} ({elapsed.TotalSeconds:F1}s)", attempt);

                switch (status)
                {
                    case "complete_success" when !string.IsNullOrEmpty(jobResponse.State?.AudioPath):
                        return $"{CdnUrl}{jobResponse.State.AudioPath}";

                    case "complete_failure" or "dead":
                        throw new FakeYouException("TTS generation failed: " + message);

                    default:
                        if (elapsed > _options.TtsTimeout)
                            throw new TimeoutException($"TTS generation timed out after {elapsed.TotalSeconds:F1}s");

                        _logger?.LogDebug("Job still processing after {Elapsed:F1}s. Status: {Status}",
                            elapsed.TotalSeconds, message);

                        await Task.Delay(_options.PollingInterval, cancellationToken);
                        continue;
                }
            }
        }

        private byte[] ProcessAudioData(byte[] audioData)
        {
            if (!_audioProcessor.ValidateWavFormat(audioData))
            {
                _logger?.LogWarning("Invalid WAV format, attempting to fix header...");
                audioData = _audioProcessor.EnsureValidWavHeader(audioData, WavFormat.Default);
            }

            try
            {
                var converted = _audioProcessor.ConvertToWav(audioData, WavFormat.Default);
                return converted;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to process audio data");
                throw new FakeYouException("Failed to process audio data: " + ex.Message, ex);
            }
        }

        private async Task<byte[]> DownloadAudioAsync(string audioUrl, CancellationToken cancellationToken)
        {
            _logger?.LogDebug("Downloading audio from {Url}", audioUrl);

            var response = await _httpClient.GetAsync(audioUrl, cancellationToken);
            _logger?.LogDebug("Download response status: {StatusCode}", response.StatusCode);
    
            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadAsByteArrayAsync(cancellationToken);
    
            _logger?.LogDebug("Downloaded {Bytes} bytes of audio data", data.Length);
    
            if (data.Length == 0)
                throw new FakeYouException("Received empty audio data");

            return data;
        }

        private async Task<TtsResponse> RequestTtsAsync(TtsRequest request, CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogDebug("Sending TTS request for model {ModelToken} with text: {Text}", 
                    request.TtsModelToken, request.InferenceText);

                var json = JsonConvert.SerializeObject(request, _jsonSettings);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/tts/inference", content, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                _logger?.LogDebug("TTS response: {StatusCode}, Content: {Content}",
                    response.StatusCode, responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    throw new FakeYouException(
                        $"TTS request failed: {response.StatusCode}",
                        (int)response.StatusCode,
                        responseContent);
                }

                var result = JsonConvert.DeserializeObject<TtsResponse>(responseContent, _jsonSettings);
                if (result == null || string.IsNullOrEmpty(result.InferenceJobToken))
                {
                    throw new FakeYouException("Invalid TTS response - missing job token");
                }

                _logger?.LogDebug("Received job token: {JobToken}", result.InferenceJobToken);
                return result;
            }
            catch (Exception ex) when (ex is not FakeYouException)
            {
                throw new FakeYouException("TTS request failed", ex);
            }
        }

        private async Task<TtsJobResponse> GetTtsJobStatusAsync(string jobToken, CancellationToken cancellationToken)
        {
            try
            {
                var url = $"/tts/job/{jobToken}";
                _logger?.LogDebug("Checking job status at: {Url}", url);
        
                var response = await _httpClient.GetAsync(url, cancellationToken);
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                _logger?.LogDebug("Job status response: {StatusCode}, Content: {Content}",
                    response.StatusCode, content);

                if (!response.IsSuccessStatusCode)
                {
                    throw new FakeYouException(
                        $"Failed to get job status with code {(int)response.StatusCode}",
                        (int)response.StatusCode,
                        content);
                }

                var result = JsonConvert.DeserializeObject<TtsJobResponse>(content, _jsonSettings);
                if (result == null)
                {
                    throw new FakeYouException("Failed to deserialize job response");
                }

                _logger?.LogDebug("Job status: {Status}, Audio path: {Path}, Description: {Description}",
                    result.State?.Status,
                    result.State?.AudioPath,
                    result.State?.StatusDescription ?? result.State?.Status);

                return result;
            }
            catch (Exception ex) when (ex is not FakeYouException)
            {
                _logger?.LogError(ex, "Failed to get job status for token {JobToken}", jobToken);
                throw new FakeYouException("Failed to get job status", ex);
            }
        }

        public async Task<IReadOnlyList<VoiceModel>> GetVoiceModelsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger?.LogDebug("Fetching available voice models");

                // Use /tts/list endpoint as in the working implementation
                var request = new HttpRequestMessage(HttpMethod.Get, "/tts/list");
                var response = await _httpClient.SendAsync(request, cancellationToken);
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                _logger?.LogDebug("Raw API Response: {Content}", content);

                if (!response.IsSuccessStatusCode)
                {
                    throw new FakeYouException(
                        $"Request failed: {response.StatusCode}",
                        (int)response.StatusCode,
                        content);
                }

                // Use the same serializer settings as the working implementation
                var result = JsonConvert.DeserializeObject<VoiceModelResponse>(content, new JsonSerializerSettings 
                {
                    ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
                    Error = (sender, args) => 
                    {
                        _logger?.LogError("JSON Error: {Error}", args.ErrorContext.Error.Message);
                        args.ErrorContext.Handled = true;
                    },
                    MissingMemberHandling = MissingMemberHandling.Ignore
                });

                if (result == null || !result.Success)
                {
                    throw new FakeYouException("Failed to get voice models");
                }

                // Log the first few models to see what we're getting
                foreach (var model in result.Models.Take(5))
                {
                    _logger?.LogDebug("Model: {Title}, Token: {Token}, Type: {Type}", 
                        model.Title, model.ModelToken, model.ModelType);
                }

                if (!result.Models.Any())
                {
                    throw new FakeYouException("No voice models found in response");
                }

                return result.Models;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to fetch voice models");
                throw;
            }
        }

        private void ReportProgress(FakeYouProgressState state, string message, int attempt = 0)
        {
            OnProgress?.Invoke(new FakeYouProgress
            {
                State = state,
                Message = message,
                Attempt = attempt,
                MaxAttempts = _options.MaxRetryAttempts,
                ElapsedTime = DateTime.UtcNow - DateTime.UtcNow
            });
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _httpClient.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}