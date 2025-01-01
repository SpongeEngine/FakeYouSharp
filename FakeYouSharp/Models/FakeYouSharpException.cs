namespace FakeYouSharp.Models
{
    /// <summary>
    /// Exception thrown when an error occurs during FakeYou API operations
    /// </summary>
    public class FakeYouSharpException : Exception
    {
        /// <summary>
        /// HTTP status code if applicable
        /// </summary>
        public int? StatusCode { get; }

        /// <summary>
        /// Raw response body if available
        /// </summary>
        public string? ResponseBody { get; }

        public FakeYouSharpException(string message) : base(message)
        {
        }

        public FakeYouSharpException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }

        public FakeYouSharpException(string message, int statusCode, string? responseBody = null) 
            : base(message)
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }
    }
}