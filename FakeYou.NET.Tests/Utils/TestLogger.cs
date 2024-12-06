using System.Diagnostics;

namespace FakeYou.NET.Tests.Utils
{
    public class TestLogger
    {
        private readonly List<string> _logs = new();
        private readonly object _lock = new();

        public void Log(string message)
        {
            lock (_lock)
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                var logMessage = $"{timestamp} - {message}";
                _logs.Add(logMessage);
                Debug.WriteLine(logMessage); // Immediate output for debugging
            }
        }

        public void WriteAllLogs()
        {
            lock (_lock)
            {
                Debug.WriteLine("\n=== Test Logs ===");
                foreach (var log in _logs)
                {
                    Debug.WriteLine(log);
                }
                Debug.WriteLine("================\n");
            }
        }
    }
}