﻿using Blum.Models;
using Blum.Utilities;
using WTelegram;

namespace Blum.Core
{
    partial class BlumBot : IDisposable
    {
        protected readonly FakeWebClient _session;
        protected readonly string _accountName;
        protected readonly Client _client;
        protected readonly string _phoneNumber;
        protected string _refreshToken;
        protected Logger _logger;
        protected WTelegramLogger WTelegramLogger;
        private static readonly object _configLock = new();
        private bool _disposed = false;
        private static readonly object _disposeLock = new();

        public BlumBot(FakeWebClient session, string account, string phoneNumber, Logger logger, bool debugMode = false)
        {
            _session = session;
            _session.SetTimeout(TimeSpan.FromSeconds(60));
            _accountName = account;
            _phoneNumber = phoneNumber;
            _refreshToken = string.Empty;
            _logger = logger;
            _logger.DebugMode = debugMode;
            WTelegramLogger = new WTelegramLogger(account);
            WTelegramLogger.GetLogFunction().Invoke(-1, $"{new string('-', 128)}\n{new string('\t', 6)}{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} Bot Started\n{new string('-', 128)}");
            Helpers.Log = WTelegramLogger.GetLogFunction();
            _client = new Client(Config);
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

            lock (_disposeLock)
            {
                if (_disposed) return;

                if (disposing)
                {
                    _session?.Dispose();
                    _client?.Dispose();
                    WTelegramLogger?.Dispose();

                    _refreshToken = null;
                    _logger = null;
                    WTelegramLogger = null;

                    if (_logger != null && _logger.DebugMode)
                    {
                        WTelegramLogger?.GetLogFunction()?.Invoke(-1, $"{new string('-', 128)}\n{new string('\t', 6)}{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} Bot Stopped instance disposed\n{new string('-', 128)}");
                    }
                }

                _disposed = true;
            }
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
                        Console.Write($"({_accountName}) Verification code: ");
                        return Console.ReadLine();
                    case "session_pathname": return Path.GetFullPath(Path.Combine(TelegramSessionsPaths.SessionsFolder, _accountName));
                    default: return null;
                }
            }
        }
    }
}
