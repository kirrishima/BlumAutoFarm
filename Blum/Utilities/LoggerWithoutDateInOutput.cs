namespace Blum.Utilities
{
    internal class LoggerWithoutDateInOutput : Logger
    {
        protected override void Log(string message, LogMessageType type)
        {
            lock (_consoleLock)
            {
                SetConsoleColor(logColors[type], () => _loggingAction(LogMessageTypeName[type]));

                _loggingAction(" | ");
                _loggingAction(message);
                _loggingAction("\n");
            }
        }

        protected override void Log(LogMessageType type, string separator = " | ", params (string message, ConsoleColor? color)[] messages)
        {
            lock (_consoleLock)
            {
                SetConsoleColor(logColors[type], () => _loggingAction(LogMessageTypeName[type]));
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
    }
}
