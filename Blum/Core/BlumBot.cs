using Blum.Exceptions;
using Blum.Models;
using Blum.Utilities;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using TL;
using WTelegram;
using static Blum.Utilities.RandomUtility.Random;

namespace Blum.Core
{
    class BlumBot
    {
        protected readonly FakeWebClient _session;
        protected readonly string _accountName;
        protected readonly Client _client;
        protected readonly string _phoneNumber;
        protected string _refreshToken;
        protected Logger _logger;
        protected RandomUtility.Random _random;
        protected static readonly string SessionsFolder = "sessions";
        private readonly StreamWriter _streamWriter;
        private static readonly object _fileLock = new object();
        private static readonly object _consoleLock = new object();
        private static readonly object _configLock = new object();

        protected readonly struct BlumUrls
        {
            /// <summary>https://game-domain.blum.codes/api/v1/user/balance</summary>
            public static readonly string Balance = "https://game-domain.blum.codes/api/v1/user/balance";

            /// <summary>https://gateway.blum.codes/v1/auth/provider/PROVIDER_TELEGRAM_MINI_APP</summary>
            public static readonly string ProviderMiniApp = "https://gateway.blum.codes/v1/auth/provider/PROVIDER_TELEGRAM_MINI_APP";

            /// <summary>https://game-domain.blum.codes/api/v1/farming/claim</summary>
            public static readonly string FarmingClaim = "https://game-domain.blum.codes/api/v1/farming/claim";

            /// <summary>https://game-domain.blum.codes/api/v1/farming/start</summary>
            public static readonly string FarmingStart = "https://game-domain.blum.codes/api/v1/farming/start";

            /// <summary>https://game-domain.blum.codes/api/v1/game/claim</summary>
            public static readonly string GameClaim = "https://game-domain.blum.codes/api/v1/game/claim";

            /// <summary>https://game-domain.blum.codes/api/v1/game/play</summary>
            public static readonly string GameStart = "https://game-domain.blum.codes/api/v1/game/play";

            /// <summary>https://gateway.blum.codes/v1/auth/refresh</summary>
            public static readonly string Refresh = "https://gateway.blum.codes/v1/auth/refresh";

            /// <summary>https://game-domain.blum.codes/api/v1/daily-reward?offset=-180</summary>
            public static readonly string ClaimDailyReward = "https://game-domain.blum.codes/api/v1/daily-reward?offset=-180";
        }

        private static readonly ConsoleColor[] WTelegramConsoleColor = [ ConsoleColor.DarkGray, ConsoleColor.DarkCyan,
            ConsoleColor.Cyan, ConsoleColor.Yellow, ConsoleColor.Red, ConsoleColor.Magenta, ConsoleColor.DarkBlue ];

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

            if (!Directory.Exists(SessionsFolder))
            {
                Directory.CreateDirectory(SessionsFolder);
            }
            _streamWriter = new StreamWriter($"{Path.Combine(SessionsFolder, $"{_accountName} WTelegram")} logs.txt", append: true);
            _client = new Client(Config);
            Helpers.Log = (lvl, str) =>
            {
                lock (_fileLock)
                {
                    _streamWriter.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | Log Level: {lvl} | {str}");
                    _streamWriter.Flush();
                }
                if (lvl > 2)
                {
                    lock (_consoleLock)
                    {
                        ConsoleColor color = Console.ForegroundColor;
                        Console.ForegroundColor = WTelegramConsoleColor[lvl];
                        Console.WriteLine(str);
                        Console.ForegroundColor = color;
                    }
                }
            };
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
                    case "session_pathname": return Path.GetFullPath(Path.Combine(SessionsFolder, _accountName));
                    default: return null;
                }
            }
        }

        public async Task CreaateSessionIfNeeded()
        {
            await _client.LoginUserIfNeeded();
            _client.Reset();
        }

        public async Task PlayGameAsync(int playPasses)
        {
            _logger.Debug(Logger.LogMessageType.Warning, messages: ("PlayGameAsync()", null));
            while (playPasses > 0)
            {
                try
                {
                    await Task.Delay(RandomDelayMilliseconds(Delay.Play));

                    object? gameId = await StartGameAsync();
                    string? gameIdString = ValidateGameId(gameId);

                    if (string.IsNullOrEmpty(gameIdString))
                    {
                        _logger.Error(($"{_accountName}", ConsoleColor.DarkCyan),
                            ($"Couldn't start play in game! play passes: {playPasses}", null));
                        _logger.Debug(Logger.LogMessageType.Error,
                            ($"gameId: {gameId}, is string: {gameId is string}, is Json String: {gameId is JsonElement j && j.ValueKind == JsonValueKind.String}",
                            ConsoleColor.Yellow));
                        break;
                    }

                    await Task.Delay(RandomDelayMilliseconds(Delay.ClaimGame));

                    (bool isOK, string? TextResponse, int? Points) results = (false, null, null);
                    results = await ClaimGameAsync(gameIdString);

                    _logger.Debug(
                        (_accountName, ConsoleColor.DarkCyan),
                        ($"PlayGame Status: {results.isOK} | Response text: {results.TextResponse} | Points: {results.Points}", null)
                    );

                    if (results.isOK)
                    {
                        _logger.Success(($"{_accountName}", ConsoleColor.DarkCyan),
                            ($"Finish play in game! Reward: {results.Points}", null));
                    }
                    else
                    {
                        _logger.Error(($"{_accountName}", ConsoleColor.DarkCyan),
                            ($"Couldn't play game! Message: {results.TextResponse ?? "Null"}, play passes: {playPasses}", null));
                        break;
                    }
                    playPasses--;
                }
                catch (Exception ex)
                {
                    _logger.Error((_accountName, ConsoleColor.DarkCyan),
                        ($"Error occurred during playing game: {ex.Message}", null));
                    await Task.Delay(RandomDelayMilliseconds(Delay.ErrorPlay));
                }
            }
        }

        private static string? ValidateGameId(object? gameId)
        {
            if (gameId is null)
                return null;

            if (gameId is string str)
                return str;
            else if (gameId is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.String)
                return jsonElement.GetString();
            else
                return null;
        }

        public async Task<(bool, string?)> ClaimDailyRewardAsync()
        {
            _logger.Debug(Logger.LogMessageType.Warning, messages: ("ClaimDailyRewardAsync()", null));
            string? text = null;
            try
            {
                var response = await _session.GetAsync(BlumUrls.ClaimDailyReward);
                text = response;
                await Task.Delay(1000);
                return text == "OK" ? (true, null) : (false, text);
            }
            catch (Exception)
            {
                return (false, text);
            }
        }

        public async Task RefreshAsync()
        {
            _session.RecreateRestClient();
            await LoginAsync();
        }

        public async Task RefreshUsingTokenAsync()
        {
            _logger.Debug(Logger.LogMessageType.Warning, messages: ("RefreshAsync()", null));
            var data = new { refresh = _refreshToken };
            var jsonData = JsonSerializer.Serialize(data);
            _logger.Warning($"JSON DATA: {jsonData}");
            // _session.RecreateRestClient();
            var response = await _session.PostAsync(BlumUrls.Refresh, jsonData);
            Console.WriteLine($"response.ResponseContent: {response.ResponseContent}");
            var responseJson = JsonSerializer.Deserialize<Dictionary<string, object>>(response.ResponseContent ?? "{}");
            var (r, s) = response;

            if (responseJson?.TryGetValue("access", out object? obj) == true)
            {
                if (obj is string strValue)
                {
                    _session.SetHeader("Authorization", $"Bearer {strValue}");
                }
                else if (obj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.String)
                {
                    _session.SetHeader("Authorization", $"Bearer {jsonElement.GetString()}");
                }
            }
            else
            {
                Console.WriteLine("no access token");
            }

            if (responseJson?.TryGetValue("refresh", out obj) == true)
            {
                if (obj is string strValue)
                {
                    _refreshToken = strValue;
                }
                else if (obj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.String)
                {
                    if (!string.IsNullOrWhiteSpace(jsonElement.GetString()))
                    {
                        _refreshToken = jsonElement.GetString();
                    }
                }
            }
            else
            {
                Console.WriteLine("no refresh token");
            }
            await Task.Delay(1000);
            _logger.DebugMode = true;
            _logger.Debug((_accountName, ConsoleColor.DarkCyan), ($"RefreshAsync response: ", null));

            _logger.DebugDictionary(responseJson);
            _logger.DebugMode = false;
        }

        public async Task<object?> StartGameAsync()
        {
            _logger.Debug(Logger.LogMessageType.Warning, messages: ("StartGameAsync()", null));

            var result = await _session.PostAsync(BlumUrls.GameStart);
            var responseJson = JsonSerializer.Deserialize<Dictionary<string, object>>(result.ResponseContent ?? "{}");
            await Task.Delay(3000);
            //_logger.DebugMode = true;
            _logger.Debug((_accountName, ConsoleColor.DarkCyan), ($"StartGameAsync raw response: {result.RawResponse}\nAs string: {result.ResponseContent}", null));
            _logger.Debug((_accountName, ConsoleColor.DarkCyan), ($"StartGameAsync responseJson:", null));
            _logger.DebugDictionary(responseJson);
            //_logger.DebugMode = false;

            if (responseJson?.TryGetValue("gameId", out object? obj) == true)
            {
                if (obj is string strValue)
                    return strValue;
                else if (obj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.String)
                    return jsonElement.GetString();
            }
            else if (responseJson?.TryGetValue("message", out obj) == true)
            {
                if (obj is string strValue)
                    return strValue;
                else if (obj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.String)
                    return jsonElement.GetString();
            }
            await Task.Delay(1000);

            return null;
        }

        class JsonGame
        {
            [JsonPropertyName("gameId")]
            public string? GameId { get; set; } = null;
            [JsonPropertyName("points")]
            public int? Points { get; set; } = null;
        }

        public async Task<(bool IsReturnedOK, string? ResponseAsText, int? Points)> ClaimGameAsync(string gameId)
        {
            _logger.Debug(Logger.LogMessageType.Warning, messages: ("ClaimGameAsync()", null));
            int points = RandomPoints();
            JsonGame data = new()
            {
                GameId = gameId,
                Points = points
            };
            var jsonData = JsonSerializer.Serialize(data);

            var result = await _session.PostAsync(BlumUrls.GameClaim, jsonData);
            if (result.RawResponse?.IsSuccessStatusCode != true)
            {
                await Task.Delay(1000);
                result = await _session.PostAsync(BlumUrls.GameClaim, jsonData);
            }

            _logger.Debug((_accountName, ConsoleColor.DarkCyan), ($"\nClaimGame raw response: {result.RawResponse}\nAs string: {result.ResponseContent}", null));

            if (result.RawResponse == null || result.ResponseContent == null)
                return (false, null, null);
            else if (result.ResponseContent == "OK")
                return (true, null, points);
            else
                return (false, result.ResponseContent, points);

        }

        public class JsonClaim
        {
            [JsonPropertyName("timestamp")]
            public long? Timestamp { get; set; } = null;

            [JsonPropertyName("availableBalance")]
            public object? AvailableBalance { get; set; } = null;
        }

        public async Task<(long?, object?)> ClaimFarmAsync()
        {
            _logger.Debug(Logger.LogMessageType.Warning, messages: ("ClaimFarmAsync()", null));
            var result = await _session.PostAsync(BlumUrls.FarmingClaim);
            if (result.RawResponse?.IsSuccessStatusCode != true)
            {
                await Task.Delay(1000);
                result = await _session.PostAsync(BlumUrls.FarmingClaim);
            }

            if (result.RawResponse?.IsSuccessStatusCode != true || string.IsNullOrWhiteSpace(result.ResponseContent))
                return (null, null);

            var response = JsonSerializer.Deserialize<Dictionary<string, object>>(result.ResponseContent);

            long? timestamp = null;
            object? balance = null;
            response?.TryGetValue("availableBalance", out balance);

            if (response?.TryGetValue("timestamp", out object? value) == true)
            {
                if (value is long val)
                {
                    timestamp = val;
                }
                else if (value is long?)
                {
                    timestamp = (long?)value;
                }
            }
            await Task.Delay(1000);
            _logger.Debug((_accountName, ConsoleColor.DarkCyan), ($"\nClaim raw response: {result.RawResponse}\nAs string: {result.ResponseContent}", null));
            return (timestamp / 1000, balance);
        }

        public async Task StartFarmingAsync()
        {
            _logger.Debug(Logger.LogMessageType.Warning, messages: ("StartFarmingAsync()", null));
            var result = await _session.PostAsync(BlumUrls.FarmingStart);
            if (result.RawResponse?.IsSuccessStatusCode != true)
            {
                await Task.Delay(1000);
                await _session.PostAsync(BlumUrls.FarmingStart);
            }
            await Task.Delay(1000);

            _logger.Debug((_accountName, ConsoleColor.DarkCyan), ($"\nStart raw response: {result.RawResponse}\nAs string: {result.ResponseContent}", null));
        }

        private class JsonAccessToken
        {
            public class TokenInfo
            {
                [JsonPropertyName("access")]
                public string? Access { get; set; }

                [JsonPropertyName("refresh")]
                public string? Refresh { get; set; }
            }
            [JsonPropertyName("token")]
            public TokenInfo? Token { get; set; }
        }

        public async Task<bool> LoginAsync()
        {
            _logger.Debug(Logger.LogMessageType.Warning, messages: ("LoginAsync()", null));
            try
            {
                var jsonData = new { query = await GetTGWebDataAsync() };
                var json = JsonSerializer.Serialize(jsonData);

                var (RawResponse, ResponseContent) = await _session.PostAsync(BlumUrls.ProviderMiniApp, json);

                _logger.Debug((_accountName, ConsoleColor.DarkCyan), ($"LoginAsync response: {ResponseContent}", null));

                if (string.IsNullOrWhiteSpace(ResponseContent))
                    return false;

                var accessToken = JsonSerializer.Deserialize<JsonAccessToken>(ResponseContent);
                if (accessToken == null || accessToken.Token == null || string.IsNullOrWhiteSpace(accessToken.Token.Access) || string.IsNullOrWhiteSpace(accessToken.Token.Refresh))
                    return false;
                _session.SetHeader("Authorization", string.Format("Bearer {0}", accessToken.Token.Access));
                _refreshToken = accessToken.Token.Refresh;

                _logger.Debug(
                    (_accountName, ConsoleColor.DarkCyan),
                    ($"LoginAsync Token.Access: {accessToken.Token.Access}", null),
                    ($"LoginAsync Token.RefreshAsync: {accessToken.Token.Refresh}", null)
                    );
                await Task.Delay(1000);

                return true;
            }
            catch (Exception ex)
            {
                string message = $"An error occurred during registration in telegram: {ex.Message}";
                if (ex.InnerException != null)
                    message += "  |  " + ex.InnerException.Message;
                _logger.Error((_accountName, ConsoleColor.DarkCyan), (message, null));
                return false;
            }
        }

        public void CloseSession()
        {
            ((IDisposable)_session).Dispose();
        }

        private async Task<string?> GetTGWebDataAsync()
        {
            _logger.Debug(Logger.LogMessageType.Warning, messages: ("GetTGWebDataAsync()", null));
            try
            {
                var myself = await _client.LoginUserIfNeeded();
                var botPeer = await _client.Contacts_ResolveUsername("BlumCryptoBot");
                var bot = botPeer.User;

                var inputPeer = new InputPeerUser(user_id: bot.ID, access_hash: bot.access_hash);
                var inputUserBase = new InputUser(user_id: bot.ID, access_hash: bot.access_hash);

                var webViewResult = await _client.Messages_RequestWebView(
                    peer: inputPeer,
                    bot: inputUserBase,
                    platform: "android",
                    from_bot_menu: false,
                    url: "https://telegram.blum.codes/"
                );

                _client.Reset();

                _logger.Debug(
                    (_accountName, ConsoleColor.DarkCyan),
                    ($"GetTGWebDataAsync: ", null),
                    ($"webViewResult type: {webViewResult.GetType()}", null)
                    );

                await Task.Delay(1000);
                if (webViewResult is WebViewResultUrl webViewResultUrl)
                    return SplitAndProcessURL(webViewResultUrl.url);
                else
                    throw new BlumException($"Wrong result's data type parsing RequestWebView: got {webViewResult.GetType()}, expected {typeof(WebViewResultUrl)}");
            }
            catch (Exception ex)
            {
                throw new BlumException("Unexpected error while getting data from telegram", ex);
            }
        }

        private static string SplitAndProcessURL(string URL)
        {
            string[] parts = URL.Split(new[] { "tgWebAppData=" }, StringSplitOptions.None);
            if (parts.Length > 1)
            {
                string dataPart = parts[1].Split(new[] { "&tgWebAppVersion" }, StringSplitOptions.None)[0];
                string decodedData = HttpUtility.UrlDecode(HttpUtility.UrlDecode(URL.Split("tgWebAppData=")[1].Split("&tgWebAppVersion")[0]));
                return decodedData;
            }
            return URL;
        }

        private class JsonBalanceResponse
        {
            public class JsonTimeResponse
            {
                [JsonPropertyName("startTime")]
                public long? StartTime { get; set; }
                [JsonPropertyName("endTime")]
                public long? EndTime { get; set; }
            }

            [JsonPropertyName("timestamp")]
            public long? Timestamp { get; set; }
            [JsonPropertyName("playPasses")]
            public int? PlayPasses { get; set; }
            [JsonPropertyName("farming")]
            public JsonTimeResponse? Farming { get; set; }
        }

        public async Task<(long? timeStamp, long? timeStart, long? timeEnd, int? playPasses)> GetBalanceAsync()
        {
            _logger.Debug(Logger.LogMessageType.Warning, messages: ("GetBalanceAsync()", null));
            string? response = await _session.GetAsync(BlumUrls.Balance);

            _logger.Debug((_accountName, ConsoleColor.DarkCyan), ($"GetBalanceAsync response: {response}", null));

            if (string.IsNullOrWhiteSpace(response))
            {
                await Task.Delay(2500);
                return (null, null, null, null);
            }

            var responseJson = JsonSerializer.Deserialize<JsonBalanceResponse>(response);
            if (responseJson == null)
            {
                _logger.Warning("Failed to fetch balance: GET request returned null");
                return (null, null, null, null);
            }

            long? timeStamp = responseJson.Timestamp;
            int? playPasses = responseJson.PlayPasses;

            timeStamp = timeStamp != 0 ? timeStamp : null;
            playPasses = playPasses != 0 ? playPasses : null;

            long? timeStart = null, timeEnd = null;
            if (responseJson.Farming != null)
            {
                timeStart = responseJson.Farming.StartTime;
                timeEnd = responseJson.Farming.EndTime;

                timeStart = timeStart != 0 ? timeStart : null;
                timeEnd = timeEnd != 0 ? timeEnd : null;
            }

            _logger.Debug((_accountName, ConsoleColor.DarkCyan),
                ("GetBalanceAsync data: ", null),
                ($"timeStamp: {timeStamp}", null),
                ($"playPasses: {playPasses}", null),
                ($"timeStart: {timeStart}", null),
                ($"timeEnd: {timeEnd}", null)
                );

            return (timeStamp / 1000, timeStart / 1000, timeEnd / 1000, playPasses);
        }
    }
}
