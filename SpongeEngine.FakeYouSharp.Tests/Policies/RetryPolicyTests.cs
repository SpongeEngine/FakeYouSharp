using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SpongeEngine.FakeYouSharp.Models;
using SpongeEngine.FakeYouSharp.Policies;
using Xunit;

namespace SpongeEngine.FakeYouSharp.Tests.Policies
{
    public class RetryPolicyTests
    {
        private readonly Mock<ILogger> _loggerMock;
        private readonly RetryPolicy _policy;

        public RetryPolicyTests()
        {
            _loggerMock = new Mock<ILogger>();
            _policy = new RetryPolicy(3, TimeSpan.FromMilliseconds(10), _loggerMock.Object);
        }

        [Fact]
        public async Task ExecuteAsync_SucceedsFirstTry_ReturnsResult()
        {
            // Arrange
            var expected = "success";

            // Act
            var result = await _policy.ExecuteAsync(() => Task.FromResult(expected));

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public async Task ExecuteAsync_SucceedsAfterRetries_ReturnsResult()
        {
            // Arrange
            var attempts = 0;
            var maxAttempts = 2;

            async Task<string> TestOperation()
            {
                attempts++;
                if (attempts < maxAttempts)
                {
                    throw new HttpRequestException("Temporary failure");
                }
                return "success";
            }

            // Act
            var result = await _policy.ExecuteAsync(TestOperation);

            // Assert
            result.Should().Be("success");
            attempts.Should().Be(maxAttempts);
        }

        [Fact]
        public async Task ExecuteAsync_ExceedsMaxAttempts_ThrowsException()
        {
            // Arrange
            var attempts = 0;

            async Task<string> TestOperation()
            {
                attempts++;
                throw new HttpRequestException("Persistent failure");
            }

            // Act & Assert
            await _policy.Invoking(p => p.ExecuteAsync(TestOperation))
                .Should()
                .ThrowAsync<FakeYouSharpException>()
                .Where(ex => ex.Message.Contains("Operation failed after") &&
                             ex.InnerException is HttpRequestException);
    
            // Verify 3 attempts were made (using the value we passed to constructor)
            attempts.Should().Be(3);
        }

        [Theory]
        [InlineData(429)] // Too Many Requests
        [InlineData(503)] // Service Unavailable
        [InlineData(502)] // Bad Gateway
        [InlineData(504)] // Gateway Timeout
        public async Task ExecuteAsync_RetriableStatusCode_Retries(int statusCode)
        {
            // Arrange
            var attempts = 0;
            var maxAttempts = 2;

            async Task<string> TestOperation()
            {
                attempts++;
                if (attempts < maxAttempts)
                {
                    throw new FakeYouSharpException("Retry needed", statusCode);
                }
                return "success";
            }

            // Act
            var result = await _policy.ExecuteAsync(TestOperation);

            // Assert
            result.Should().Be("success");
            attempts.Should().Be(maxAttempts);
        }
    }
}