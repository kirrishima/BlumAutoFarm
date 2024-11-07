using Blum.Models;
using Blum.Models.Json;
using Blum.Utilities;
using System.Text.Encodings.Web;
using System.Text.Json;
using static Blum.Utilities.RandomUtility.Random;

namespace Blum.Core
{
    internal partial class BlumBot
    {
        public async Task PlayGameAsync(int playPasses)
        {
            _logger.Debug(Logger.LogMessageType.Warning, messages: ("PlayGameAsync()", null));

            int fails = 0;
            int gamesPlayed = 0;

            while (playPasses > 0)
            {
                try
                {
                    if (gamesPlayed == 21)
                    {
                        gamesPlayed = 0;
                        await RefreshUsingTokenAsync();
                    }

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

                    gamesPlayed++;

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
                        fails += fails > 0 ? -1 : 0;
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

            var result = await _session.TryPostAsync(BlumUrls.GAME_START);
            var responseJson = JsonSerializer.Deserialize<Dictionary<string, object>>(result.responseContent ?? "{}");

            _logger.Debug((_accountName, ConsoleColor.DarkCyan), ($"StartGameAsync raw json: {result.restResponse}\nAs string: {result.responseContent}", null));
            _logger.Debug((_accountName, ConsoleColor.DarkCyan), ($"StartGameAsync responseJson:", null));
            _logger.DebugDictionary(responseJson);

            if (responseJson?.TryGetValue("gameId", out object? obj) == true)
            {
                if (obj is string strValue)
                    return strValue;
                else if (obj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.String)
                    return jsonElement.GetString();
            }

            return null;
        }

        public async Task<(bool IsReturnedOK, string? ResponseAsText, int? Points)> ClaimGameAsync(string gameId)
        {
            _logger.Debug(Logger.LogMessageType.Warning, messages: ("ClaimGameAsync()", null));

            int points = RandomBlumPoints();
            int dogs = 0;

            if (await IsDogsEligible())
            {
                dogs = RandomDogsPoints(points);
            }

            var payload = await CreatePayloadAsync(gameId, points, dogs);

            if (payload is not null)
            {
                var jsonData = JsonSerializer.Serialize(new { payload }, new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                var result = await _session.TryPostAsync(BlumUrls.GAME_CLAIM, jsonData);
                if (result.restResponse?.IsSuccessStatusCode != true)
                {
                    await Task.Delay(3000);
                    result = await _session.TryPostAsync(BlumUrls.GAME_CLAIM, jsonData);
                }

                _logger.Debug((_accountName, ConsoleColor.DarkCyan), ($"\nClaimGame raw json: {result.restResponse}\nAs string: {result.responseContent}", null));

                if (result.restResponse == null || result.responseContent == null)
                    return (false, result.restResponse?.Content ?? null, null);
                else if (result.responseContent.Equals("OK"))
                    return (true, null, points);
                else
                    return (false, result.responseContent, points);
            }

            return (false, null, null);
        }

        public async Task<bool> IsDogsEligible()
        {
            bool eligible = false;

            try
            {
                var resp = await _session.TryGetAsync(BlumUrls.DOGS_ELIGIBILITY);

                if (resp.ResponseContent is not null)
                {
                    var respJson = JsonSerializer.Deserialize<Dictionary<string, object>>(resp.ResponseContent);
                    if (respJson?.TryGetValue("eligible", out object? isEligible) == true)
                    {
                        eligible = (bool)isEligible;
                    }
                }
            }
            catch { }

            return eligible;
        }

        protected async Task<string?> CreatePayloadAsync(string gameID, int points, int dogs = 0)
        {

            int freeze = points / 50 + (int)Random.Shared.NextDouble() * 2; // e.g, if points are 210, then FREEZE is from 4 to 6
            int bombs = points < 150 ? (int)Random.Shared.NextDouble() * 2 : 0; // if points are lower than 150, will add from 0 to 2 bombs clicked

            BlumPayloadJson data = new()
            {
                GameId = "ad7cd4cd-29d1-4548-89a3-91301996ef31",
                /*                Challenge = new Challenge
                                {
                                    Id = "a9a6fdff-69c5-43a3-8bb7-fdf0f875766f",
                                    Nonce = 74114,
                                    Hash = "00001976638fc4d4aea3cdcbeaae2b17eb99306c814dc7e78e89ef448a1895ff"
                                },*/
                EarnedPoints = new EarnedPoints
                {
                    BP = new Point { Amount = points }
                },
                AssetClicks = new Dictionary<string, AssetClick>
                {
                    { "CLOVER", new AssetClick { Clicks = points } },
                    { "FREEZE", new AssetClick { Clicks = freeze} },
                    { "BOMB", new AssetClick { Clicks = bombs } },
                }
            };

            var jsonData = JsonSerializer.Serialize(data);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            ErrorResponse? errorResponse = null;

            var (restResponse, responseContent, exception) = await _session.TryPostAsync(TelegramSettings.PayloadServer, jsonData); // Hosting local server for generating payloads

            Console.WriteLine(restResponse?.Content);
            /*          
             Console.WriteLine(responseContent);
                      Console.WriteLine(exception);
            */

            if (restResponse?.IsSuccessStatusCode == true)
            {
                //using JsonDocument doc = JsonDocument.Parse(responseContent ?? "");
                /*                if (doc.RootElement.TryGetProperty("payload", out JsonElement value))
                                {
                                    Console.WriteLine("\n\n");
                                    Console.WriteLine(value.ToString());
                                    return value.GetString();
                                }*/
                var s = JsonSerializer.Deserialize<PayloadServerResponseJson>(responseContent);

                Console.WriteLine();
                Console.WriteLine(s?.Payload);
                return s?.Payload;
            }

            return "";
        }
    }
}
