using FakeYou.NET.Audio;

namespace FakeYou.NET.Extensions
{
    public static class AudioProcessingExtensions 
    {
        public static byte[] ValidateAndConvert(this byte[] audioData, AudioProcessor processor, WavFormat targetFormat)
        {
            if (!processor.ValidateWavFormat(audioData))
                throw new ArgumentException("Invalid WAV format");

            var converted = processor.ConvertToWav(audioData, targetFormat);
            return processor.EnsureValidWavHeader(converted, targetFormat);
        }
    }
}