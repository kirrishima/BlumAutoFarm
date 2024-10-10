using Blum.Models;
using Blum.Utilities;
using System.Text.Json;
using static Blum.Utilities.RandomUtility.Random;

namespace Blum.Core
{
    partial class BlumBot
    {
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

                    if (!results.isOK)
                    {
                        await Task.Delay(RandomDelayMilliseconds(Delay.BeforeRequest));
                        results = (false, null, null);
                        results = await ClaimGameAsync(gameIdString);

                        _logger.Debug(
                            (_accountName, ConsoleColor.DarkCyan),
                            ($"PlayGame Status: {results.isOK} | Response responseText: {results.TextResponse} | Points: {results.Points}", null)
                        );
                    }

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

        public async Task<(bool IsReturnedOK, string? ResponseAsText, int? Points)> ClaimGameAsync(string gameId)
        {
            _logger.Debug(Logger.LogMessageType.Warning, messages: ("ClaimGameAsync()", null));
            int points = RandomPoints();
            BlumGameJson data = new()
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
    }
}
