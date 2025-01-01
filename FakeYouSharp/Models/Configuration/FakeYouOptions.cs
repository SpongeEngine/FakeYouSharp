using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FakeYouSharp.Models.Configuration
{
    /// <summary>
    /// Configuration options for the FakeYou client
    /// </summary>
    public class FakeYouOptions
    {
        /// <summary>
        /// Optional API key for authenticated requests
        /// </summary>
        public string? ApiKey { get; set; }
        
        /// <summary>
        /// Base URL for the FakeYou API
        /// </summary>
        public string BaseUrl { get; set; } = "https://api.fakeyou.com";

        /// <summary>
        /// Base URL for the FakeYou CDN
        /// </summary>
        public string CdnUrl { get; set; } = "https://cdn-2.fakeyou.com";

        /// <summary>
        /// Timeout for HTTP requests (default: 30 seconds)
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Maximum number of retry attempts for failed requests
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 10;

        /// <summary>
        /// Delay between retry attempts
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Whether to validate response data
        /// </summary>
        public bool ValidateResponseData { get; set; } = true;

        /// <summary>
        /// Optional logger instance
        /// </summary>
        public ILogger? Logger { get; set; }

        /// <summary>
        /// Custom JSON serializer settings
        /// </summary>
        public JsonSerializerSettings? JsonSettings { get; set; }
        
        /// <summary>
        /// Maximum time to wait for TTS generation (default: 3 minutes)
        /// </summary>
        public TimeSpan TtsTimeout { get; set; } = TimeSpan.FromMinutes(3);

        /// <summary>
        /// Interval between polling attempts (default: 2 seconds)
        /// </summary>
        public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(2);
    }
}