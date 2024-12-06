using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace FakeYou.NET.Models.Responses
{
    /// <summary>
    /// Initial response from TTS request
    /// </summary>
    internal class TtsResponse
    {
        /// <summary>
        /// Whether the request was successful
        /// </summary>
        [JsonProperty("success")]
        public bool Success { get; init; }
    
        /// <summary>
        /// Token for tracking the inference job
        /// </summary>
        [JsonProperty("inference_job_token")]
        public string InferenceJobToken { get; init; } = string.Empty;
    }
    
    /// <summary>
    /// Response for TTS job status
    /// </summary>
    internal class TtsJobResponse
    {
        /// <summary>
        /// Whether the request was successful
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; init; }
    
        /// <summary>
        /// Current state of the job
        /// </summary>
        [JsonPropertyName("state")]
        public TtsJobState? State { get; init; }
    }
    
    /// <summary>
    /// State information for a TTS job
    /// </summary>
    internal class TtsJobState
    {
        /// <summary>
        /// Current status of the job
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; init; } = string.Empty;
    
        /// <summary>
        /// Additional status description if available
        /// </summary>
        [JsonProperty("maybe_extra_status_description")]
        public string? StatusDescription { get; init; }
    
        /// <summary>
        /// Path to the generated audio file if complete
        /// </summary>
        [JsonProperty("maybe_public_bucket_wav_audio_path")]
        public string? AudioPath { get; init; }
    }
}