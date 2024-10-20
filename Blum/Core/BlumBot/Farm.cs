using Blum.Models;
using Blum.Models.Json;
using Blum.Utilities;
using System.Text.Json;

namespace Blum.Core
{
    internal partial class BlumBot
    {
        public async Task<(bool, string?)> ClaimDailyRewardAsync()
        {
            _logger.Debug(Logger.LogMessageType.Warning, messages: ("ClaimDailyRewardAsync()", null));
            string? responseText = null;
            try
            {
                var response = await _session.TryGetAsync(BlumUrls.ClaimDailyReward);

                var json = response.ResponseContent;
                var res = JsonSerializer.Deserialize<BlumDailyRewardJson>(json ?? "{}");
                string reward;

                try
                {
                    reward = $"Day: {res?.Days[1].Ordinal}; Passes: {res?.Days[1].Reward.Passes}; Points: {res?.Days[1].Reward.Points}";
                }
                catch (Exception)
                {
                    reward = string.Empty;
                }

                if (response.RestResponse?.IsSuccessStatusCode == true)
                {
                    await Task.Delay(1000);
                    response = await _session.TryPostAsync(BlumUrls.ClaimDailyReward);
                }
                responseText = response.ResponseContent;
                return responseText == "OK" ? (true, reward) : (false, responseText);
            }
            catch (Exception)
            {
                return (false, responseText);
            }
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

            var json = JsonSerializer.Deserialize<BlumBalanceJson>(result.responseContent);

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
    }
}
