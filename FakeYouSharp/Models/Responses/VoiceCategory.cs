using System.Text.Json.Serialization;

namespace FakeYouSharp.Models.Responses
{
    /// <summary>
    /// Represents a category of voice models
    /// </summary>
    public class VoiceCategory
    {
        /// <summary>
        /// Unique token identifying the category
        /// </summary>
        [JsonPropertyName("category_token")]
        public string CategoryToken { get; init; } = string.Empty;

        /// <summary>
        /// Name of the category
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Display name used in dropdown menus
        /// </summary>
        [JsonPropertyName("name_for_dropdown")]
        public string DropdownName { get; init; } = string.Empty;
    }
}