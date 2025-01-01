using Newtonsoft.Json;

namespace FakeYouSharp.Models.Responses
{
    public class VoiceModelResponse 
    {
        [JsonProperty("success")]
        public bool Success { get; init; }

        [JsonProperty("models")]
        public List<VoiceModel> Models { get; init; } = new();
    }
}