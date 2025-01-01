using Newtonsoft.Json;

namespace FakeYouSharp.Models.Requests
{
    /// <summary>
    /// Request model for text-to-speech generation
    /// </summary>
    public class TtsRequest
    {
        [JsonProperty("tts_model_token")]
        public string TtsModelToken { get; }

        [JsonProperty("inference_text")]
        public string InferenceText { get; }

        [JsonProperty("uuid_idempotency_token")]
        public string UuidIdempotencyToken { get; }

        public TtsRequest(string modelToken, string text, string idempotencyToken)
        {
            TtsModelToken = modelToken ?? throw new ArgumentNullException(nameof(modelToken));
            InferenceText = text ?? throw new ArgumentNullException(nameof(text));
            UuidIdempotencyToken = idempotencyToken ?? throw new ArgumentNullException(nameof(idempotencyToken));
        }
    }
}