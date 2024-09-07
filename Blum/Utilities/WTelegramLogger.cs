using Blum.Models;

namespace Blum.Utilities
{
    internal class WTelegramLogger
    {
        private static readonly object _fileLock = new();
        private static readonly object _consoleLock = new();
        private static readonly object _logLock = new();
        private readonly StreamWriter _streamWriter;

        private readonly string _accountName;

        private static readonly ConsoleColor[] WTelegramConsoleColor = [ ConsoleColor.DarkGray, ConsoleColor.DarkCyan,
            ConsoleColor.Cyan, ConsoleColor.Yellow, ConsoleColor.Red, ConsoleColor.Magenta, ConsoleColor.DarkBlue ];

        public Action<int, string> LogFunction { get; }

        public WTelegramLogger(string accountName)
        {
            _accountName = accountName = accountName ?? "undefined";

            _streamWriter = new StreamWriter(Path.Combine(TelegramSessionsPaths.SessionsFolder, TelegramSessionsPaths.GetWTelegramLogFilePath(accountName)), append: true);

            LogFunction = (lvl, str) =>
            {
                lock (_logLock)
                {
                    lock (_fileLock)
                    {
                        if (lvl < 0)
                        {
                            _streamWriter.WriteLine(str);
                        }
                        else
                        {
                            _streamWriter.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | Log Level: {lvl} | {str}");
                            _streamWriter.Flush();
                        }
                    }
/*
                    if (lvl > 2)
                    {
                        lock (_consoleLock)
                        {
                            ConsoleColor color = Console.ForegroundColor;
                            Console.ForegroundColor = WTelegramConsoleColor[lvl];
                            Console.WriteLine($"({_accountName}) {str}");
                            Console.ForegroundColor = color;
                        }
                    }*/
                }
            };
        }

        public Action<int, string> GetLogFunction()
        {
            return LogFunction;
        }
    }
}
