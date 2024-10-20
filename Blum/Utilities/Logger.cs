namespace Blum.Utilities
{
    public class Logger
    {
        public delegate void LoggingAction(string message);

        public enum LogMessageType
        {
            Info = 1,
            Success,
            Warning,
            Error
        }

        protected readonly LoggingAction _loggingAction;

        public readonly Dictionary<LogMessageType, ConsoleColor> logColors = new()
        {
            { LogMessageType.Info,    ConsoleColor.Blue },
            { LogMessageType.Success, ConsoleColor.Cyan },
            { LogMessageType.Warning, ConsoleColor.DarkYellow },
            { LogMessageType.Error,   ConsoleColor.DarkRed }
        };

        protected static readonly Dictionary<LogMessageType, string> LogMessageTypeName = new()
        {
            { LogMessageType.Info,    "INFO" },
            { LogMessageType.Success, "SUCCESS" },
            { LogMessageType.Warning, "WARNING" },
            { LogMessageType.Error,   "ERROR" }
        };

        public const string DefaultSeparator = " | ";
        protected readonly ConsoleColor _currentTimeColor = ConsoleColor.Green;
        protected static string _currentTime => DateTime.Now.ToString("MM-dd HH:mm:ss:ffff");
        protected static readonly int maxLogMessageLength = LogMessageTypeName.Values.Max(v => v.Length);
        protected readonly object _debugModeLock = new();
        protected bool _debugMode;
        protected static readonly object _consoleLock = new();
        public bool DebugMode
        {
            get
            {
                lock (_debugModeLock)
                {
                    return _debugMode;
                }
            }
            set
            {
                lock (_debugModeLock)
                {
                    _debugMode = value;
                }
            }
        }

        public Logger(LoggingAction? loggingAction = null)
        {
            _loggingAction = loggingAction ?? ((string message) =>
            {
                lock (_consoleLock)
                {
                    Console.Write(message);
                }
            });
        }

        protected static void SetConsoleColor(ConsoleColor color, Action action)
        {
            lock (_consoleLock)
            {
                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                action();
                Console.ForegroundColor = originalColor;
            }
        }

        protected static string FormatLogMessage(string message)
        {
            int padding = (maxLogMessageLength - message.Length) / 2;
            return message.PadLeft(message.Length + padding).PadRight(maxLogMessageLength);
        }

        protected virtual void Log(string message, LogMessageType type)
        {
            lock (_consoleLock)
            {
                SetConsoleColor(_currentTimeColor, () => _loggingAction(_currentTime));
                _loggingAction(" | ");

                string formattedMessage = FormatLogMessage(LogMessageTypeName[type]);
                SetConsoleColor(logColors[type], () => _loggingAction(formattedMessage));

                _loggingAction(" | ");
                _loggingAction(message);
                _loggingAction("\n");
            }
        }

        protected virtual void Log(LogMessageType type, string separator = " | ", params (string message, ConsoleColor? color)[] messages)
        {
            lock (_consoleLock)
            {
                SetConsoleColor(_currentTimeColor, () => _loggingAction(_currentTime));
                _loggingAction(separator);

                string formattedMessage = FormatLogMessage(LogMessageTypeName[type]);
                SetConsoleColor(logColors[type], () => _loggingAction(formattedMessage));
                _loggingAction(separator);

                for (int i = 0; i < messages.Length; i++)
                {
                    var (message, color) = messages[i];
                    if (color.HasValue)
                        SetConsoleColor(color.Value, () => _loggingAction(message));
                    else
                        _loggingAction(message);

                    if (i < messages.Length - 1)
                        _loggingAction(separator);
                }
                _loggingAction("\n");
            }
        }

        public void PrintAllExeceptionsData(Exception? exception)
        {
            int level = 1;

            while (exception != null)
            {
                _loggingAction(new string('-', Console.WindowWidth));

                SetConsoleColor(ConsoleColor.Cyan, () => _loggingAction($"Error Nested Level: "));
                _loggingAction($"{level++}\n");

                SetConsoleColor(ConsoleColor.Cyan, () => _loggingAction($"Stack Trace: "));
                _loggingAction($"\n{exception.StackTrace}\n");

                SetConsoleColor(ConsoleColor.Cyan, () => _loggingAction($"Message: "));
                _loggingAction($"{exception.Message}\n");

                exception = exception.InnerException;
            }
        }

        public void LogMessage(string message, LogMessageType type = LogMessageType.Info, string separator = DefaultSeparator)
        {
            Log(type: type, separator: separator, messages: (message, null));
        }

        public void LogMessage(LogMessageType type = LogMessageType.Info, string separator = DefaultSeparator, params (string message, ConsoleColor? color)[] messages)
        {
            Log(type: type, separator: separator, messages: messages);
        }
        public void Debug(LogMessageType type, string message)
        {
            if (DebugMode)
            {
                Log(type: type, separator: DefaultSeparator, messages: (message, null));
            }
        }
        public void Debug(LogMessageType type = LogMessageType.Info, string separator = DefaultSeparator, params (string message, ConsoleColor? color)[] messages)
        {
            if (DebugMode)
            {
                Log(type: type, separator: separator, messages: messages);
            }
        }
        public void Debug(LogMessageType type, params (string message, ConsoleColor? color)[] messages)
        {
            if (DebugMode)
            {
                Log(type: type, separator: DefaultSeparator, messages: messages);
            }
        }
        public void Debug(params (string message, ConsoleColor? color)[] messages)
        {
            if (DebugMode)
            {
                Log(type: LogMessageType.Info, separator: DefaultSeparator, messages: messages);
            }
        }

        public void DebugDictionary<T>(Dictionary<T, object>? dictionary) where T : IComparable
        {
            lock (_consoleLock)
            {
                if (dictionary == null)
                    return;

                if (DebugMode)
                {
                    Debug(messages: ("Dictionary pairs: key value", ConsoleColor.Yellow));
                    foreach (var kvp in dictionary)
                    {
                        string keyValueString = $"{kvp.Key}: {Convert.ToString(kvp.Value)}";
                        Console.WriteLine(keyValueString);
                    }
                }
            }
        }

        public void Info(string separator = DefaultSeparator, params (string message, ConsoleColor? color)[] messages)
        {
            Log(type: LogMessageType.Info, separator: separator, messages: messages);
        }

        public void Info(params (string message, ConsoleColor? color)[] messages)
        {
            Log(type: LogMessageType.Info, separator: DefaultSeparator, messages: messages);
        }
        public void Info(string message)
        {
            Log(message, LogMessageType.Info);
        }


        public void Success(string separator = DefaultSeparator, params (string message, ConsoleColor? color)[] messages)
        {
            Log(type: LogMessageType.Success, separator: separator, messages: messages);
        }

        public void Success(params (string message, ConsoleColor? color)[] messages)
        {
            Log(type: LogMessageType.Success, separator: DefaultSeparator, messages: messages);
        }

        public void Success(string message)
        {
            Log(message, LogMessageType.Success);
        }


        public void Error(string separator = DefaultSeparator, params (string message, ConsoleColor? color)[] messages)
        {
            Log(type: LogMessageType.Error, separator: separator, messages: messages);
        }

        public void Error(params (string message, ConsoleColor? color)[] messages)
        {
            Log(type: LogMessageType.Error, separator: DefaultSeparator, messages: messages);
        }

        public void Error(string message)
        {
            Log(message, LogMessageType.Error);
        }

        public void Warning(string separator = DefaultSeparator, params (string message, ConsoleColor? color)[] messages)
        {
            Log(type: LogMessageType.Warning, separator: separator, messages: messages);
        }

        public void Warning(params (string message, ConsoleColor? color)[] messages)
        {
            Log(type: LogMessageType.Warning, separator: DefaultSeparator, messages: messages);
        }

        public void Warning(string message)
        {
            Log(message, LogMessageType.Warning);
        }
    }
}
