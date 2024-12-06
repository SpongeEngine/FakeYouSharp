using Xunit;
using FluentAssertions;
using FakeYou.NET.Audio;
using Microsoft.Extensions.Logging;
using Moq;

namespace FakeYou.NET.Tests.Audio
{
    public class AudioProcessorTests
    {
        private readonly Mock<ILogger> _loggerMock;
        private readonly AudioProcessor _processor;

        public AudioProcessorTests()
        {
            _loggerMock = new Mock<ILogger>();
            _processor = new AudioProcessor(_loggerMock.Object);
        }

        [Fact]
        public void ValidateWavFormat_WithValidWavData_ReturnsTrue()
        {
            // Arrange
            var validWavHeader = new byte[]
            {
                // RIFF header
                0x52, 0x49, 0x46, 0x46, // "RIFF"
                0x24, 0x00, 0x00, 0x00, // Chunk size
                0x57, 0x41, 0x56, 0x45, // "WAVE"
                // fmt chunk
                0x66, 0x6D, 0x74, 0x20, // "fmt "
                0x10, 0x00, 0x00, 0x00, // Chunk size
                0x01, 0x00,             // Format code (PCM)
                0x02, 0x00,             // Channels (2)
                0x44, 0xAC, 0x00, 0x00, // Sample rate (44100)
                0x10, 0xB1, 0x02, 0x00, // Byte rate
                0x04, 0x00,             // Block align
                0x10, 0x00,             // Bits per sample (16)
                // data chunk
                0x64, 0x61, 0x74, 0x61, // "data"
                0x00, 0x00, 0x00, 0x00  // Chunk size
            };

            // Act
            var result = _processor.ValidateWavFormat(validWavHeader);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ValidateWavFormat_WithInvalidData_ReturnsFalse()
        {
            // Arrange
            var invalidData = new byte[] { 0x00, 0x01, 0x02 }; // Too short

            // Act
            var result = _processor.ValidateWavFormat(invalidData);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ConvertToWav_WithMatchingFormat_ReturnsSameData()
        {
            // Arrange
            var format = WavFormat.Default;
            var input = CreateValidWavData(format);

            // Act
            var result = _processor.ConvertToWav(input, format);

            // Assert
            result.Should().BeEquivalentTo(input);
        }

        [Fact]
        public void EnsureValidWavHeader_CreatesCorrectHeader()
        {
            // Arrange
            var format = new WavFormat 
            { 
                SampleRate = 44100,
                BitsPerSample = 16,
                Channels = 2
            };
            var audioData = new byte[1024]; // Dummy audio data

            // Act
            var result = _processor.EnsureValidWavHeader(audioData, format);

            // Assert
            result.Length.Should().Be(audioData.Length);
            
            // Verify RIFF header
            result[0].Should().Be(0x52); // 'R'
            result[1].Should().Be(0x49); // 'I'
            result[2].Should().Be(0x46); // 'F'
            result[3].Should().Be(0x46); // 'F'

            // Verify WAVE identifier
            result[8].Should().Be(0x57);  // 'W'
            result[9].Should().Be(0x41);  // 'A'
            result[10].Should().Be(0x56); // 'V'
            result[11].Should().Be(0x45); // 'E'
        }

        private byte[] CreateValidWavData(WavFormat format)
        {
            // Create a complete WAV file structure
            var headerSize = 44;
            var dataSize = 1024;
            var totalSize = headerSize + dataSize;
    
            var data = new byte[totalSize];
    
            // RIFF header
            System.Text.Encoding.ASCII.GetBytes("RIFF").CopyTo(data, 0);
            BitConverter.GetBytes(totalSize - 8).CopyTo(data, 4);
            System.Text.Encoding.ASCII.GetBytes("WAVE").CopyTo(data, 8);
    
            // fmt chunk
            System.Text.Encoding.ASCII.GetBytes("fmt ").CopyTo(data, 12);
            BitConverter.GetBytes(16).CopyTo(data, 16); // Chunk size
            BitConverter.GetBytes((short)1).CopyTo(data, 20); // PCM format
            BitConverter.GetBytes((short)format.Channels).CopyTo(data, 22);
            BitConverter.GetBytes(format.SampleRate).CopyTo(data, 24);
            BitConverter.GetBytes(format.SampleRate * format.Channels * (format.BitsPerSample / 8)).CopyTo(data, 28);
            BitConverter.GetBytes((short)(format.Channels * (format.BitsPerSample / 8))).CopyTo(data, 32);
            BitConverter.GetBytes((short)format.BitsPerSample).CopyTo(data, 34);
    
            // data chunk
            System.Text.Encoding.ASCII.GetBytes("data").CopyTo(data, 36);
            BitConverter.GetBytes(dataSize).CopyTo(data, 40);
    
            // Add some dummy audio data
            for (int i = 44; i < totalSize; i++)
                data[i] = (byte)(i % 256);
        
            return data;
        }
    }
}