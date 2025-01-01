using System.Buffers.Binary;
using FluentAssertions;
using SpongeEngine.FakeYouSharp.Audio;
using Xunit;

namespace SpongeEngine.FakeYouSharp.Tests.Audio
{
    public class WavProcessorTests
    {
        private readonly WavProcessor _processor;

        public WavProcessorTests()
        {
            _processor = new WavProcessor();
        }

        [Fact]
        public void GetWavFormat_WithValidWav_ReturnsCorrectFormat()
        {
            // Arrange
            var expectedFormat = new WavFormat
            {
                AudioFormat = 1, // PCM
                Channels = 2,
                SampleRate = 44100,
                BitsPerSample = 16
            };
            var wavData = CreateWavData(expectedFormat);

            // Act
            var format = _processor.GetWavFormat(wavData);

            // Assert
            format.Should().BeEquivalentTo(expectedFormat);
        }

        [Fact]
        public void ProcessAudio_WithValidPcmFormat_ReturnsSameData()
        {
            // Arrange
            var format = new WavFormat
            {
                AudioFormat = 1,
                Channels = 2,
                SampleRate = 44100,
                BitsPerSample = 16
            };
            var wavData = CreateWavData(format);

            // Act
            var result = _processor.ProcessAudio(wavData);

            // Assert
            result.Should().BeEquivalentTo(wavData);
        }

        [Fact]
        public void ProcessAudio_With8BitFormat_ConvertsTo16Bit()
        {
            // Arrange
            var format = new WavFormat
            {
                AudioFormat = 1,
                Channels = 1,
                SampleRate = 44100,
                BitsPerSample = 8
            };
            var wavData = CreateWavData(format);

            // Act
            var result = _processor.ProcessAudio(wavData);
            var resultFormat = _processor.GetWavFormat(result);

            // Assert
            resultFormat.BitsPerSample.Should().Be(16);
            resultFormat.AudioFormat.Should().Be(1); // Still PCM
            resultFormat.Channels.Should().Be(format.Channels);
            resultFormat.SampleRate.Should().Be(format.SampleRate);
            
            // Verify data section is twice as long (8->16 bit)
            var originalDataSize = BitConverter.ToInt32(wavData.AsSpan(40, 4));
            var newDataSize = BitConverter.ToInt32(result.AsSpan(40, 4));
            newDataSize.Should().Be(originalDataSize * 2);
        }

        [Theory]
        [InlineData(0)] // Empty array
        [InlineData(43)] // Too short
        public void ProcessAudio_WithInvalidData_ThrowsException(int size)
        {
            // Arrange
            var invalidData = new byte[size];

            // Act & Assert
            _processor.Invoking(p => p.ProcessAudio(invalidData))
                .Should().Throw<WavProcessingException>()
                .WithMessage("Invalid WAV data*");
        }

        [Fact]
        public void ProcessAudio_WithInvalidHeader_ThrowsException()
        {
            // Arrange - Create data with invalid RIFF signature
            var data = new byte[1024];
            System.Text.Encoding.ASCII.GetBytes("INVALID").CopyTo(data, 0);

            // Act & Assert
            _processor.Invoking(p => p.ProcessAudio(data))
                .Should().Throw<WavProcessingException>()
                .WithMessage("Invalid WAV header");
        }

        private byte[] CreateWavData(WavFormat format)
        {
            const int dataSize = 1024;
            const int headerSize = 44;
            var totalSize = headerSize + dataSize;
            var data = new byte[totalSize];

            // RIFF header
            System.Text.Encoding.ASCII.GetBytes("RIFF").CopyTo(data, 0);
            BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(4), totalSize - 8);
            System.Text.Encoding.ASCII.GetBytes("WAVE").CopyTo(data, 8);

            // fmt chunk
            System.Text.Encoding.ASCII.GetBytes("fmt ").CopyTo(data, 12);
            BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(16), 16); // Chunk size
            BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(20), (short)format.AudioFormat);
            BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(22), (short)format.Channels);
            BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(24), format.SampleRate);
            BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(28), 
                format.SampleRate * format.Channels * (format.BitsPerSample / 8));
            BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(32), 
                (short)(format.Channels * (format.BitsPerSample / 8)));
            BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(34), (short)format.BitsPerSample);

            // data chunk
            System.Text.Encoding.ASCII.GetBytes("data").CopyTo(data, 36);
            BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(40), dataSize);

            // Generate sample audio data
            for (int i = headerSize; i < totalSize; i++)
            {
                data[i] = (byte)(i % 256);
            }

            return data;
        }
    }
}