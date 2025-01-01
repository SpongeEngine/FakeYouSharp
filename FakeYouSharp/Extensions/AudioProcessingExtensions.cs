using FakeYouSharp.Audio;

namespace FakeYouSharp.Extensions
{
    /// <summary>
    /// Extension methods for audio processing operations
    /// </summary>
    public static class AudioProcessingExtensions 
    {
        /// <summary>
        /// Validates and ensures WAV audio data is in the correct format
        /// </summary>
        /// <param name="audioData">Raw WAV audio data</param>
        /// <param name="processor">WavProcessor instance</param>
        /// <returns>Processed WAV data in 16-bit PCM format</returns>
        public static byte[] ValidateAndConvert(this byte[] audioData, WavProcessor processor)
        {
            // Get current format for logging/validation
            var currentFormat = processor.GetWavFormat(audioData);
            
            // Process the audio to ensure 16-bit PCM format
            return processor.ProcessAudio(audioData);
        }

        /// <summary>
        /// Validates WAV format and provides format information
        /// </summary>
        /// <param name="audioData">Raw WAV audio data</param>
        /// <param name="processor">WavProcessor instance</param>
        /// <param name="format">Output parameter containing the WAV format information</param>
        /// <returns>True if the format is valid, false otherwise</returns>
        public static bool TryGetWavFormat(this byte[] audioData, WavProcessor processor, out WavFormat format)
        {
            try
            {
                format = processor.GetWavFormat(audioData);
                return true;
            }
            catch (WavProcessingException)
            {
                format = null!;
                return false;
            }
        }

        /// <summary>
        /// Checks if the audio data is already in 16-bit PCM format
        /// </summary>
        /// <param name="audioData">Raw WAV audio data</param>
        /// <param name="processor">WavProcessor instance</param>
        /// <returns>True if the audio is already in 16-bit PCM format</returns>
        public static bool IsValidPcmFormat(this byte[] audioData, WavProcessor processor)
        {
            try
            {
                var format = processor.GetWavFormat(audioData);
                return format.BitsPerSample == 16 && format.AudioFormat == 1;
            }
            catch (WavProcessingException)
            {
                return false;
            }
        }
    }
}