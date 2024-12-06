using NAudio.Wave;
using Microsoft.Extensions.Logging;

namespace FakeYou.NET.Audio
{
    public class AudioProcessor
    {
        private readonly ILogger? _logger;

        public AudioProcessor(ILogger? logger = null)
        {
            _logger = logger;
        }

        public byte[] ConvertToWav(byte[] audioData, WavFormat format)
        {
            _logger?.LogDebug("Starting audio conversion to {SampleRate}Hz, {BitsPerSample}-bit, {Channels} channels",
                format.SampleRate, format.BitsPerSample, format.Channels);

            using var inputStream = new MemoryStream(audioData);
            using var reader = new WaveFileReader(inputStream);

            _logger?.LogDebug("Input format: {Format}", reader.WaveFormat);

            if (!NeedsConversion(reader.WaveFormat, format))
            {
                _logger?.LogDebug("Audio format matches target, no conversion needed");
                return audioData;
            }

            var targetFormat = new WaveFormat(
                format.SampleRate,
                format.BitsPerSample,
                format.Channels);

            _logger?.LogDebug("Converting to format: {Format}", targetFormat);

            using var converter = new WaveFormatConversionStream(targetFormat, reader);
            using var outputStream = new MemoryStream();
            WaveFileWriter.WriteWavFileToStream(outputStream, converter);

            var result = outputStream.ToArray();
            _logger?.LogDebug("Audio conversion complete, output size: {Size} bytes", result.Length);

            return result;
        }

        public bool ValidateWavFormat(byte[] audioData)
        {
            try
            {
                if (audioData == null || audioData.Length < 44)
                    return false;

                // Check RIFF header
                if (!VerifyBytes(audioData, 0, "RIFF") ||
                    !VerifyBytes(audioData, 8, "WAVE") ||
                    !VerifyBytes(audioData, 12, "fmt "))
                    return false;

                // Verify format chunk size
                var fmtSize = BitConverter.ToInt32(audioData, 16);
                if (fmtSize < 16) // Minimum fmt chunk size
                    return false;

                // Verify format code (PCM = 1)
                var formatCode = BitConverter.ToInt16(audioData, 20);
                if (formatCode != 1)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "WAV format validation failed");
                return false;
            }
        }

        public byte[] EnsureValidWavHeader(byte[] data, WavFormat format)
        {
            var header = new byte[44];

            // RIFF header
            System.Text.Encoding.ASCII.GetBytes("RIFF").CopyTo(header, 0);
            BitConverter.GetBytes((uint)(data.Length - 8)).CopyTo(header, 4);
            System.Text.Encoding.ASCII.GetBytes("WAVE").CopyTo(header, 8);

            // Format chunk
            System.Text.Encoding.ASCII.GetBytes("fmt ").CopyTo(header, 12);
            BitConverter.GetBytes((uint)16).CopyTo(header, 16);
            BitConverter.GetBytes((ushort)1).CopyTo(header, 20);
            BitConverter.GetBytes((ushort)format.Channels).CopyTo(header, 22);
            BitConverter.GetBytes((uint)format.SampleRate).CopyTo(header, 24);
            BitConverter.GetBytes((uint)(format.SampleRate * format.Channels * (format.BitsPerSample / 8)))
                .CopyTo(header, 28);
            BitConverter.GetBytes((ushort)(format.Channels * (format.BitsPerSample / 8)))
                .CopyTo(header, 32);
            BitConverter.GetBytes((ushort)format.BitsPerSample).CopyTo(header, 34);

            // Data chunk
            System.Text.Encoding.ASCII.GetBytes("data").CopyTo(header, 36);
            BitConverter.GetBytes((uint)(data.Length - 44)).CopyTo(header, 40);

            var result = new byte[data.Length];
            header.CopyTo(result, 0);
            Array.Copy(data, 44, result, 44, data.Length - 44);

            return result;
        }
        
        private bool VerifyBytes(byte[] data, int offset, string expected)
        {
            var actual = System.Text.Encoding.ASCII.GetString(data, offset, expected.Length);
            return actual == expected;
        }

        private bool NeedsConversion(WaveFormat current, WavFormat target) =>
            current.SampleRate != target.SampleRate ||
            current.BitsPerSample != target.BitsPerSample ||
            current.Channels != target.Channels;
    }
}