using Microsoft.Extensions.Logging;
using SpongeEngine.FakeYouSharp.Models;

namespace SpongeEngine.FakeYouSharp.Policies
{
    public class RetryPolicy : IRetryPolicy
    {
        private readonly int _maxAttempts;
        private readonly TimeSpan _delay;
        private readonly ILogger? _logger;

        public RetryPolicy(int maxAttempts, TimeSpan delay, ILogger? logger = null)
        {
            _maxAttempts = maxAttempts > 0 ? maxAttempts : throw new ArgumentException("Max attempts must be greater than 0");
            _delay = delay > TimeSpan.Zero ? delay : throw new ArgumentException("Delay must be greater than 0");
            _logger = logger;
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
        {
            Exception? lastException = null;
            var startTime = DateTime.UtcNow;

            for (int attempt = 1; attempt <= _maxAttempts; attempt++)
            {
                try
                {
                    _logger?.LogDebug("Attempt {Attempt} of {MaxAttempts}", attempt, _maxAttempts);
                    return await action();
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    
                    if (attempt == _maxAttempts || !ShouldRetry(ex))
                    {
                        var elapsed = DateTime.UtcNow - startTime;
                        throw new FakeYouSharpException(
                            $"Operation failed after {attempt} attempts over {elapsed}: {ex.Message}",
                            ex);
                    }

                    _logger?.LogWarning(ex, "Attempt {Attempt} failed, retrying after delay", attempt);
                    await Task.Delay(CalculateDelay(attempt));
                }
            }

            // This should never be reached due to the throw in the loop
            throw new FakeYouSharpException(
                $"Operation failed after {_maxAttempts} attempts",
                lastException ?? new Exception("Unknown error"));
        }

        private bool ShouldRetry(Exception ex)
        {
            return ex switch
            {
                HttpRequestException => true,
                TimeoutException => true,
                FakeYouSharpException fyEx => fyEx.StatusCode.HasValue && IsRetryableStatusCode(fyEx.StatusCode.Value),
                _ => false
            };
        }

        private bool IsRetryableStatusCode(int statusCode) => statusCode switch
        {
            404 => true, // Not Found (during polling)
            429 => true, // Too Many Requests
            502 => true, // Bad Gateway
            503 => true, // Service Unavailable
            504 => true, // Gateway Timeout
            _ => false
        };

        private TimeSpan CalculateDelay(int attempt)
        {
            // Exponential backoff with jitter
            var exponentialDelay = _delay * Math.Pow(2, attempt - 1);
            var maxDelay = TimeSpan.FromSeconds(30); // Cap maximum delay
            var jitter = Random.Shared.NextDouble() * 0.3 + 0.85; // 85-115% of base delay
            
            return TimeSpan.FromMilliseconds(Math.Min(
                exponentialDelay.TotalMilliseconds * jitter,
                maxDelay.TotalMilliseconds));
        }
    }
}