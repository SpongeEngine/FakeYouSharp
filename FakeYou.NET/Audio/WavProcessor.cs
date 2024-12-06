using System;
using System.Buffers.Binary;
using System.IO;

namespace FakeYou.NET.Audio
{
    /// <summary>
    /// Platform-agnostic WAV audio processor that can be used with any game engine or application
    /// </summary>
    public class WavProcessor
    {
        private const int WAV_HEADER_SIZE = 44;

        /// <summary>
        /// Processes WAV audio data, ensuring it's in a valid 16-bit PCM format
        /// </summary>
        /// <param name="wavData">Raw WAV file bytes</param>
        /// <returns>Processed WAV data in 16-bit PCM format</returns>
        public byte[] ProcessAudio(byte[] wavData)
        {
            ValidateInput(wavData);
            var format = ReadWavFormat(wavData);

            // If already 16-bit PCM, just validate header
            if (format.BitsPerSample == 16 && format.AudioFormat == 1)
            {
                return EnsureValidHeader(wavData, format);
            }

            // Extract and convert audio data
            var pcmData = ExtractAudioData(wavData);
            var converted = ConvertTo16BitPcm(pcmData, format);

            return CreateWavFile(converted, format);
        }

        /// <summary>
        /// Gets the format information from WAV data without modifying it
        /// </summary>
        public WavFormat GetWavFormat(byte[] wavData)
        {
            ValidateInput(wavData);
            return ReadWavFormat(wavData);
        }

        private void ValidateInput(byte[] data)
        {
            if (data == null || data.Length < WAV_HEADER_SIZE)
                throw new WavProcessingException("Invalid WAV data: Too short or null");

            if (!IsValidWavHeader(data))
                throw new WavProcessingException("Invalid WAV header");
        }

        private bool IsValidWavHeader(byte[] data)
        {
            return VerifyChunkId(data, 0, "RIFF") &&
                   VerifyChunkId(data, 8, "WAVE") &&
                   VerifyChunkId(data, 12, "fmt ");
        }

        private bool VerifyChunkId(byte[] data, int offset, string expected)
        {
            return System.Text.Encoding.ASCII.GetString(data, offset, expected.Length) == expected;
        }

        private WavFormat ReadWavFormat(byte[] data)
        {
            return new WavFormat
            {
                AudioFormat = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(20, 2)),
                Channels = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(22, 2)),
                SampleRate = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(24, 4)),
                BitsPerSample = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(34, 2))
            };
        }

        private byte[] ExtractAudioData(byte[] wavFile)
        {
            int dataChunkPos = FindDataChunk(wavFile);
            if (dataChunkPos < 0)
                throw new WavProcessingException("Could not find data chunk in WAV file");

            int dataSize = BinaryPrimitives.ReadInt32LittleEndian(wavFile.AsSpan(dataChunkPos + 4, 4));
            var audioData = new byte[dataSize];
            Array.Copy(wavFile, dataChunkPos + 8, audioData, 0, dataSize);
            
            return audioData;
        }

        private int FindDataChunk(byte[] wavFile)
        {
            int pos = 12; // Skip RIFF header
            while (pos < wavFile.Length - 8)
            {
                var chunkId = System.Text.Encoding.ASCII.GetString(wavFile, pos, 4);
                var chunkSize = BinaryPrimitives.ReadInt32LittleEndian(wavFile.AsSpan(pos + 4, 4));
                
                if (chunkId == "data")
                    return pos;
                
                pos += 8 + chunkSize;
            }
            return -1;
        }

        private byte[] ConvertTo16BitPcm(byte[] audioData, WavFormat format)
        {
            switch (format.BitsPerSample)
            {
                case 16:
                    return audioData;

                case 8:
                    return Convert8BitTo16Bit(audioData);

                case 24:
                    return Convert24BitTo16Bit(audioData);

                case 32:
                    return Convert32BitTo16Bit(audioData);

                default:
                    throw new WavProcessingException($"Unsupported bits per sample: {format.BitsPerSample}");
            }
        }

        private byte[] Convert8BitTo16Bit(byte[] input)
        {
            var output = new byte[input.Length * 2];
            for (int i = 0; i < input.Length; i++)
            {
                short sample = (short)((input[i] - 128) * 256);
                BinaryPrimitives.WriteInt16LittleEndian(output.AsSpan(i * 2), sample);
            }
            return output;
        }

        private byte[] Convert24BitTo16Bit(byte[] input)
        {
            var output = new byte[input.Length / 3 * 2];
            for (int i = 0; i < input.Length / 3; i++)
            {
                int sample = (input[i * 3 + 2] << 16) | (input[i * 3 + 1] << 8) | input[i * 3];
                if ((sample & 0x800000) != 0) // Sign extend
                    sample |= ~0xFFFFFF;
                short shortSample = (short)(sample >> 8);
                BinaryPrimitives.WriteInt16LittleEndian(output.AsSpan(i * 2), shortSample);
            }
            return output;
        }

        private byte[] Convert32BitTo16Bit(byte[] input)
        {
            var output = new byte[input.Length / 2];
            for (int i = 0; i < input.Length / 4; i++)
            {
                float sample = BinaryPrimitives.ReadSingleLittleEndian(input.AsSpan(i * 4));
                short shortSample = (short)(sample * short.MaxValue);
                BinaryPrimitives.WriteInt16LittleEndian(output.AsSpan(i * 2), shortSample);
            }
            return output;
        }

        private byte[] EnsureValidHeader(byte[] wavFile, WavFormat format)
        {
            if (IsValidWavHeader(wavFile))
                return wavFile;

            var audioData = ExtractAudioData(wavFile);
            return CreateWavFile(audioData, format);
        }

        private byte[] CreateWavFile(byte[] audioData, WavFormat format)
        {
            var header = new byte[WAV_HEADER_SIZE];
            var fileSize = WAV_HEADER_SIZE + audioData.Length - 8;

            // RIFF header
            System.Text.Encoding.ASCII.GetBytes("RIFF").CopyTo(header, 0);
            BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(4), fileSize);
            System.Text.Encoding.ASCII.GetBytes("WAVE").CopyTo(header, 8);

            // fmt chunk
            System.Text.Encoding.ASCII.GetBytes("fmt ").CopyTo(header, 12);
            BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(16), 16); // Chunk size
            BinaryPrimitives.WriteInt16LittleEndian(header.AsSpan(20), 1); // PCM = 1
            BinaryPrimitives.WriteInt16LittleEndian(header.AsSpan(22), (short)format.Channels);
            BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(24), format.SampleRate);
            BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(28), format.SampleRate * format.Channels * 2);
            BinaryPrimitives.WriteInt16LittleEndian(header.AsSpan(32), (short)(format.Channels * 2));
            BinaryPrimitives.WriteInt16LittleEndian(header.AsSpan(34), 16); // Always 16-bit output

            // data chunk
            System.Text.Encoding.ASCII.GetBytes("data").CopyTo(header, 36);
            BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(40), audioData.Length);

            var result = new byte[WAV_HEADER_SIZE + audioData.Length];
            header.CopyTo(result, 0);
            audioData.CopyTo(result, WAV_HEADER_SIZE);

            return result;
        }
    }
}