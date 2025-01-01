namespace SpongeEngine.FakeYouSharp.Audio
{
    /// <summary>
    /// Represents WAV audio format parameters
    /// </summary>
    public class WavFormat
    {
        public int AudioFormat { get; set; } // 1 = PCM
        public int Channels { get; set; }
        public int SampleRate { get; set; }
        public int BitsPerSample { get; set; }

        public override string ToString() =>
            $"{SampleRate}Hz {BitsPerSample}-bit {Channels}ch" + (AudioFormat == 1 ? " PCM" : "");
    }

    public class WavProcessingException : Exception
    {
        public WavProcessingException(string message) : base(message) { }
        public WavProcessingException(string message, Exception inner) : base(message, inner) { }
    }
}