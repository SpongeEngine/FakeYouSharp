namespace FakeYou.NET.Models
{
    /// <summary>
    /// Exception thrown when an error occurs during FakeYou API operations
    /// </summary>
    public class FakeYouException : Exception
    {
        /// <summary>
        /// HTTP status code if applicable
        /// </summary>
        public int? StatusCode { get; }

        /// <summary>
        /// Raw response body if available
        /// </summary>
        public string? ResponseBody { get; }

        public FakeYouException(string message) : base(message)
        {
        }

        public FakeYouException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }

        public FakeYouException(string message, int statusCode, string? responseBody = null) 
            : base(message)
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }
    }
}