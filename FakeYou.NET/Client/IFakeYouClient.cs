using FakeYou.NET.Models.Progress;
using FakeYou.NET.Models.Responses;

namespace FakeYou.NET.Client
{
    /// <summary>
    /// Interface for interacting with the FakeYou TTS API
    /// </summary>
    public interface IFakeYouClient : IDisposable
    {
        /// <summary>
        /// Event that provides progress updates during operations
        /// </summary>
        event Action<FakeYouProgress>? OnProgress;

        /// <summary>
        /// Generates audio from text using the specified voice model
        /// </summary>
        /// <param name="modelToken">The FakeYou model token (e.g., TM:1234)</param>
        /// <param name="text">The text to convert to speech</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>The generated audio data as a byte array</returns>
        Task<byte[]> GenerateAudioAsync(string modelToken, string text, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets a list of available voice models
        /// </summary>
        Task<IReadOnlyList<VoiceModel>> GetVoiceModelsAsync(CancellationToken cancellationToken = default);
    }
}