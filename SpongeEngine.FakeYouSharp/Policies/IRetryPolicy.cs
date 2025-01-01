namespace SpongeEngine.FakeYouSharp.Policies
{
    /// <summary>
    /// Defines retry behavior for operations
    /// </summary>
    public interface IRetryPolicy
    {
        /// <summary>
        /// Executes an operation with retry logic
        /// </summary>
        /// <typeparam name="T">Return type of the operation</typeparam>
        /// <param name="action">The operation to execute</param>
        /// <returns>The result of the operation</returns>
        Task<T> ExecuteAsync<T>(Func<Task<T>> action);
    }
}