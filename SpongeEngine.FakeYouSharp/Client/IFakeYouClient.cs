using SpongeEngine.FakeYouSharp.Models.Progress;
using SpongeEngine.FakeYouSharp.Models.Responses;

namespace SpongeEngine.FakeYouSharp.Client
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
        /// Generates audio from text using the specified voice model.
        /// </summary>
        /// <param name="modelToken">The FakeYou model token (e.g., TM:1234)</param>
        /// <param name="text">The text to convert to speech</param>
        /// <returns>
        /// WAV audio data as received from FakeYou API (44.1 kHz, stereo, 8-bit PCM).
        /// Most modern applications expect 16-bit PCM, so format conversion may be 
        /// required using audio processing libraries like NAudio.
        /// </returns>
        Task<byte[]> GenerateAudioAsync(string modelToken, string text, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets a list of available voice models
        /// </summary>
        Task<IReadOnlyList<VoiceModel>> GetVoiceModelsAsync(CancellationToken cancellationToken = default);
    }
}