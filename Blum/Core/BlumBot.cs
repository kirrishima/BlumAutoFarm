using Blum.Exceptions;
using Blum.Models;
using Blum.Utilities;
using System.Linq.Expressions;
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
        protected readonly WTelegramLogger WTelegramLogger;
        private static readonly object _configLock = new();

        private bool _disposed = false;

        protected readonly struct BlumUrls
        {
            /// <summary>https://game-domain.blum.codes/api/v1/user/balance</summary>
            public static readonly string Balance = "https://game-domain.blum.codes/api/v1/user/balance";

            /// <summary>https://user-domain.blum.codes/api/v1/auth/provider/PROVIDER_TELEGRAM_MINI_APP</summary>
            public static readonly string ProviderMiniApp = "https://user-domain.blum.codes/api/v1/auth/provider/PROVIDER_TELEGRAM_MINI_APP";

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

        public async Task CreaateSessionIfNeeded()
        {
            await _client.LoginUserIfNeeded();
            _client.Reset();
        }

        public async Task PlayGameAsync(int playPasses)
        {
            _logger.Debug(Logger.LogMessageType.Warning, messages: ("PlayGameAsync()", null));
            int fails = 0;
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
                        if (fails == 0)
                        {
                            _logger.Info(($"{_accountName}", ConsoleColor.DarkCyan), ("Waiting for 60 seconds before retry...", null));
                            await Task.Delay(TimeSpan.FromSeconds(60));
                            fails++;
                            continue;
                        }
                        else
                            break;
                    }

                    await Task.Delay(RandomDelayMilliseconds(Delay.ClaimGame));

                    (bool isOK, string? TextResponse, int? Points) results = (false, null, null);
                    results = await ClaimGameAsync(gameIdString);

                    _logger.Debug(
                        (_accountName, ConsoleColor.DarkCyan),
                        ($"PlayGame Status: {results.isOK} | Response responseText: {results.TextResponse} | Points: {results.Points}", null)
                    );

                    if (results.isOK)
                    {
                        _logger.Success(($"{_accountName}", ConsoleColor.DarkCyan),
                            ($"Finish play in game! Reward: {results.Points}", null));
                    }
                    else
                    {
                        _logger.Error(($"{_accountName}", ConsoleColor.DarkCyan),
                            ($"Couldn't claim game! Message: {results.TextResponse ?? "Null"}, play passes: {playPasses}", null));
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

        public async Task<object?> StartGameAsync()
        {
            _logger.Debug(Logger.LogMessageType.Warning, messages: ("StartGameAsync()", null));

            var result = await _session.TryPostAsync(BlumUrls.GameStart);
            var responseJson = JsonSerializer.Deserialize<Dictionary<string, object>>(result.responseContent ?? "{}");
            await Task.Delay(3000);

            _logger.Debug((_accountName, ConsoleColor.DarkCyan), ($"StartGameAsync raw json: {result.restResponse}\nAs string: {result.responseContent}", null));
            _logger.Debug((_accountName, ConsoleColor.DarkCyan), ($"StartGameAsync responseJson:", null));
            _logger.DebugDictionary(responseJson);

            await Task.Delay(1000);

            if (responseJson?.TryGetValue("gameId", out object? obj) == true)
            {
                if (obj is string strValue)
                    return strValue;
                else if (obj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.String)
                    return jsonElement.GetString();
            }

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

            var result = await _session.TryPostAsync(BlumUrls.GameClaim, jsonData);
            if (result.restResponse?.IsSuccessStatusCode != true)
            {
                await Task.Delay(1000);
                result = await _session.TryPostAsync(BlumUrls.GameClaim, jsonData);
            }

            _logger.Debug((_accountName, ConsoleColor.DarkCyan), ($"\nClaimGame raw json: {result.restResponse}\nAs string: {result.responseContent}", null));

            if (result.restResponse == null || result.responseContent == null)
                return (false, result.restResponse?.Content ?? null, null);
            else if (result.responseContent.Equals("OK"))
                return (true, null, points);
            else
                return (false, result.responseContent, points);
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
            string? responseText = null;
            try
            {
                var response = await _session.TryGetAsync(BlumUrls.ClaimDailyReward);
                if (response.RestResponse?.IsSuccessStatusCode == true)
                {
                    await Task.Delay(1000);
                    response = await _session.TryPostAsync(BlumUrls.ClaimDailyReward);
                }
                responseText = response.ResponseContent;
                await Task.Delay(1000);
                return responseText == "OK" ? (true, null) : (false, responseText);
            }
            catch (Exception)
            {
                return (false, responseText);
            }
        }

        public async Task ReloginAsync()
        {
            _session.RecreateRestClient();
            await LoginAsync();
        }

        public async Task<bool> RefreshUsingTokenAsync()
        {
            _logger.Debug(Logger.LogMessageType.Warning, messages: ("RefreshAsync()", null));

            _session.ClearHeaders();

            var data = new { refresh = _refreshToken };
            var jsonData = JsonSerializer.Serialize(data);
            var (_, response, _) = await _session.TryPostAsync(BlumUrls.Refresh, jsonData);
            var responseJson = JsonSerializer.Deserialize<Dictionary<string, object>>(response ?? "{}");

            bool success = false;

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
                success = true;
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
                success = true;
            }

            await Task.Delay(1000);

            return success;
        }

        class BalanceClaimJson
        {
            [JsonPropertyName("availableBalance")]
            public string? AvailableBalance { get; set; }

            [JsonPropertyName("playPasses")]
            public int? PlayPasses { get; set; }

            [JsonPropertyName("isFastFarmingEnabled")]
            public bool? IsFastFarmingEnabled { get; set; }

            [JsonPropertyName("timestamp")]
            public long? Timestamp { get; set; }
        }

        public async Task<(long?, string?)> ClaimFarmAsync()
        {
            _logger.Debug(Logger.LogMessageType.Warning, messages: ("ClaimFarmAsync()", null));

            var result = await _session.TryPostAsync(BlumUrls.FarmingClaim);

            _logger.Debug((_accountName, ConsoleColor.DarkCyan), ($"Claim raw response: {result.restResponse} | As string: {result.responseContent}", null));
            if (result.restResponse?.IsSuccessStatusCode != true)
            {
                _logger.Debug((_accountName, ConsoleColor.DarkCyan), ("ClaimFarmAsync request failed, retry...", null));

                await Task.Delay(1000);
                result = await _session.TryPostAsync(BlumUrls.FarmingClaim);

                _logger.Debug((_accountName, ConsoleColor.DarkCyan), ($"Claim raw response: {result.restResponse} | As string: {result.responseContent}", null));
            }

            if (string.IsNullOrWhiteSpace(result.responseContent))
                return (null, null);

            var json = JsonSerializer.Deserialize<BalanceClaimJson>(result.responseContent);

            long? timestamp = json?.Timestamp;
            string? balance = json?.AvailableBalance;

            return (timestamp / 1000, balance);
        }

        public async Task<bool> StartFarmingAsync()
        {
            _logger.Debug(Logger.LogMessageType.Warning, messages: ("StartFarmingAsync()", null));

            var result = await _session.TryPostAsync(BlumUrls.FarmingStart);
            if (result.restResponse?.IsSuccessStatusCode != true)
            {
                await Task.Delay(1000);
                await _session.TryPostAsync(BlumUrls.FarmingStart);
            }

            await Task.Delay(1000);
            _logger.Debug((_accountName, ConsoleColor.DarkCyan), ($"Start raw json: {result.restResponse} | As string: {result.responseContent}", null));

            if (result.restResponse?.IsSuccessStatusCode != true)
            {
                return false;
            }
            return true;
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
                var (RawResponse, ResponseContent, exception) = await _session.TryPostAsync(BlumUrls.ProviderMiniApp, json);

                if (RawResponse?.IsSuccessful != true || exception != null)
                {
                    await Task.Delay(3000);
                    (RawResponse, ResponseContent, exception) = await _session.TryPostAsync(BlumUrls.ProviderMiniApp, json);
                }

                if (RawResponse?.IsSuccessful != true || exception != null)
                {
                    throw new BlumFatalError($"Error while logging in blum: request to blum api failed.", exception);
                }

                _logger.Debug((_accountName, ConsoleColor.DarkCyan), ($"LoginAsync json: {ResponseContent}", null));

                if (string.IsNullOrWhiteSpace(ResponseContent))
                {
                    throw new BlumFatalError($"Error while logging in blum: json content is empty");
                }

                var accessToken = JsonSerializer.Deserialize<JsonAccessToken>(ResponseContent);
                if (accessToken == null || accessToken.Token == null || string.IsNullOrWhiteSpace(accessToken.Token.Access) || string.IsNullOrWhiteSpace(accessToken.Token.Refresh))
                {
                    throw new BlumFatalError($"Error while logging in blum: no access token recived");
                }

                _session.SetHeader("Authorization", string.Format("Bearer {0}", accessToken.Token.Access));
                _refreshToken = accessToken.Token.Refresh;

                _logger.Debug(
                    (_accountName, ConsoleColor.DarkCyan),
                    ($"LoginAsync Token.Access: {accessToken.Token.Access}", null),
                    ($"LoginAsync Token.RefreshAsync: {accessToken.Token.Refresh}", null)
                    );

                return true;
            }
            catch (Exception ex)
            {
                string message = $"An error occurred during logging in: {ex.Message}";
                _logger.Error((_accountName, ConsoleColor.DarkCyan), (message, null));
                throw new BlumFatalError(message, ex.InnerException);
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
                    throw new BlumFatalError($"Wrong result's data type parsing RequestWebView: got {webViewResult.GetType()}, expected {typeof(WebViewResultUrl)}");
            }
            catch (Exception ex)
            {
                throw new BlumFatalError("Unexpected error while getting data from telegram", ex);
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

            var (_, response, _) = await _session.TryGetAsync(BlumUrls.Balance);

            _logger.Debug((_accountName, ConsoleColor.DarkCyan), ($"GetBalanceAsync json: {response}", null));

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
