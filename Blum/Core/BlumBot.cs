using Blum.Models;
using Blum.Utilities;
using WTelegram;

namespace Blum.Core
{
    partial class BlumBot
    {
        protected readonly FakeWebClient _session;
        protected readonly string _accountName;
        protected readonly Client _client;
        protected readonly string _phoneNumber;
        protected string _refreshToken;
        protected Logger _logger;
        protected RandomUtility.Random _random;
        protected readonly WTelegramLogger WTelegramLogger;
        private static readonly object _configLock = new();
        private bool _disposed = false;

        public BlumBot(FakeWebClient session, string account, string phoneNumber, Logger.LoggingAction? loggingAction = null, bool debugMode = false)
        {
            _session = session;
            _session.SetTimeout(TimeSpan.FromSeconds(60));
            _accountName = account;
            _phoneNumber = phoneNumber;
            _refreshToken = string.Empty;
            _logger = new Logger(loggingAction ?? Console.Write);
            _logger.DebugMode = debugMode;
            _random = new RandomUtility.Random();
            WTelegramLogger = new WTelegramLogger(account);
            WTelegramLogger.GetLogFunction()(-1, $"{new string('-', 128)}\n{new string('\t', 6)}{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} Bot Started\n{new string('-', 128)}");
            _client = new Client(Config);
            Helpers.Log = WTelegramLogger.GetLogFunction();
        }

        ~BlumBot()
        {
            Dispose(false);
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
                _session?.Dispose();
                _client?.Dispose();
                _random = null;
                _refreshToken = null;
            }

            _disposed = true;
        }

        string? Config(string what)
        {
            lock (_configLock)
            {
                switch (what)
                {
                    case "api_id": return TelegramSettings.ApiId;
                    case "api_hash": return TelegramSettings.ApiHash;
                    case "phone_number": return _phoneNumber;
                    case "verification_code":
                        Console.Write($"({_accountName}) Verification code: "); return Console.ReadLine();
                    case "session_pathname": return Path.GetFullPath(Path.Combine(TelegramSessionsPaths.SessionsFolder, _accountName));
                    default: return null;
                }
            }
        }
    }
}
