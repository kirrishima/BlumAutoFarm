using Blum.Models;

namespace Blum.Utilities
{
    internal class WTelegramLogger : IDisposable
    {
        private static readonly object _logLock = new();
        private readonly StreamWriter _streamWriter;
        private bool _disposed = false;

        private static readonly ConsoleColor[] WTelegramConsoleColor =
        {
            ConsoleColor.DarkGray,
            ConsoleColor.DarkCyan,
            ConsoleColor.Cyan,
            ConsoleColor.Yellow,
            ConsoleColor.Red,
            ConsoleColor.Magenta,
            ConsoleColor.DarkBlue
        };

        public WTelegramLogger(string accountName)
        {
            string path = Path.Combine(TelegramSessionsPaths.SessionsFolder, TelegramSessionsPaths.GetWTelegramLogFilePath(accountName));
            _streamWriter = new StreamWriter(path, append: true);
        }

        static WTelegramLogger()
        {
            if (!Directory.Exists(TelegramSessionsPaths.SessionsFolder))
            {
                Directory.CreateDirectory(TelegramSessionsPaths.SessionsFolder);
            }
        }

        public void LogFunction(int lvl, string str)
        {
            lock (_logLock)
            {
                if (lvl < 0)
                {
                    _streamWriter.WriteLine(str);
                    _streamWriter.Flush();
                }
                else
                {
                    _streamWriter.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | Log Level: {lvl} | {str}");
                    _streamWriter.Flush();
                }

                if (lvl > 2)
                {
                    ConsoleColor color = Console.ForegroundColor;
                    Console.ForegroundColor = WTelegramConsoleColor[lvl];
                    Console.WriteLine(str);
                    Console.ForegroundColor = color;
                }
            }
        }

        public Action<int, string> GetLogFunction()
        {
            return LogFunction;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _streamWriter?.Dispose();
            }

            _disposed = true;
        }

        ~WTelegramLogger()
        {
            Dispose(false);
        }
    }
}
