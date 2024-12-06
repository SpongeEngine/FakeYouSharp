namespace FakeYou.NET.Audio
{
    /// <summary>
    /// Represents WAV audio format parameters
    /// </summary>
    public record WavFormat
    {
        /// <summary>
        /// Sample rate in Hz
        /// </summary>
        public int SampleRate { get; init; } = 44100;

        /// <summary>
        /// Bits per sample
        /// </summary>
        public int BitsPerSample { get; init; } = 16;

        /// <summary>
        /// Number of channels (1 for mono, 2 for stereo)
        /// </summary>
        public int Channels { get; init; } = 2;

        /// <summary>
        /// Default CD-quality format (44.1kHz, 16-bit, stereo)
        /// </summary>
        public static WavFormat Default => new();

        /// <summary>
        /// CD-quality format (explicit version)
        /// </summary>
        public static WavFormat CD => new() 
        { 
            SampleRate = 44100, 
            BitsPerSample = 16, 
            Channels = 2 
        };

        /// <summary>
        /// High-quality format (48kHz, 24-bit, stereo)
        /// </summary>
        public static WavFormat HighQuality => new()
        {
            SampleRate = 48000,
            BitsPerSample = 24,
            Channels = 2
        };
    }
}