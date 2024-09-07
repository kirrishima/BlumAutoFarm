namespace Blum.Exceptions
{
    public class BlumException : Exception
    {
        public BlumException() : base("BlumException occurred.") { }

        public BlumException(string message) : base(message) { }

        public BlumException(string message, Exception? innerException) : base(message, innerException) { }

        public static void ThrowIfNull(object argument, string? argumentName = null)
        {
            if (argument == null)
            {
                if (string.IsNullOrEmpty(argumentName))
                {
                    throw new BlumException("Argument cannot be null.");
                }
                else
                {
                    throw new BlumException($"Argument '{argumentName}' cannot be null.");
                }
            }
        }
    }


    public class BlumFatalError : BlumException
    {
        public BlumFatalError() : base("BlumFatalError occurred.") { }

        public BlumFatalError(string message) : base(message) { }

        public BlumFatalError(string message, Exception? innerException) : base(message, innerException) { }
    }
}