namespace FakeYouSharp.Models.Progress
{
    /// <summary>
    /// Represents the progress state of a FakeYou operation
    /// </summary>
    public class FakeYouProgress
    {
        /// <summary>
        /// Current state of the operation
        /// </summary>
        public FakeYouProgressState State { get; init; }
    
        /// <summary>
        /// Progress message
        /// </summary>
        public string Message { get; init; } = string.Empty;
    
        /// <summary>
        /// Current attempt number
        /// </summary>
        public int Attempt { get; init; }
    
        /// <summary>
        /// Maximum number of attempts
        /// </summary>
        public int MaxAttempts { get; init; }
    
        /// <summary>
        /// Time elapsed since operation start
        /// </summary>
        public TimeSpan ElapsedTime { get; init; }
    
        /// <summary>
        /// Optional percentage complete (0-100)
        /// </summary>
        public double? PercentComplete { get; init; }
    }
    
    /// <summary>
    /// Possible states for a FakeYou operation
    /// </summary>
    public enum FakeYouProgressState
    {
        /// <summary>
        /// Operation is starting
        /// </summary>
        Starting,
    
        /// <summary>
        /// Request is queued
        /// </summary>
        Queued,
    
        /// <summary>
        /// Audio is being generated
        /// </summary>
        Processing,
    
        /// <summary>
        /// Audio is being downloaded
        /// </summary>
        Downloading,
    
        /// <summary>
        /// Audio format conversion
        /// </summary>
        Converting,
    
        /// <summary>
        /// Operation completed successfully
        /// </summary>
        Complete,
    
        /// <summary>
        /// Operation failed
        /// </summary>
        Failed
    }
}