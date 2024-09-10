namespace TestTask
{
    public class Logger : IDisposable
    {
        private readonly StreamWriter _logWriter;

        public Logger(string logFilePath)
        {
            _logWriter = new StreamWriter(logFilePath, append: true);
        }

        public virtual void Write(string message)
        {
            var logMessage = $"{DateTime.Now}: {message}";
            Console.WriteLine(logMessage);
            _logWriter.WriteLine(logMessage);
        }

        public void Dispose()
        {
            _logWriter?.Dispose();
        }

        public void Close()
        {
            _logWriter?.Close();
        }
    }
}