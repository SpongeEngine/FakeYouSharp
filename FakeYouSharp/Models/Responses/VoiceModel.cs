using Newtonsoft.Json;

namespace FakeYouSharp.Models.Responses
{
    /// <summary>
    /// Represents a voice model available in the FakeYou API
    /// </summary>
    public class VoiceModel
    {
        [JsonProperty("model_token")]
        public string ModelToken { get; init; } = string.Empty;

        [JsonProperty("tts_model_type")]
        public string ModelType { get; init; } = string.Empty;

        [JsonProperty("title")]
        public string Title { get; init; } = string.Empty;

        [JsonProperty("ietf_language_tag")]
        public string LanguageTag { get; init; } = string.Empty;

        [JsonProperty("maybe_suggested_unique_bot_command")]
        public string? BotCommand { get; init; }
    }
}