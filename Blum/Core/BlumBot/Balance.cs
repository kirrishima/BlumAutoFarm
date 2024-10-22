using Blum.Models;
using Blum.Models.Json;
using Blum.Utilities;
using System.Text.Json;

namespace Blum.Core
{
    internal partial class BlumBot
    {
        public async Task<(long? timeStamp, long? timeStart, long? timeEnd, int? playPasses, bool? IsFastFarmingEnabled)> GetBalanceAsync()
        {
            _logger.Debug(Logger.LogMessageType.Warning, messages: ("GetBalanceAsync()", null));

            var (_, response, _) = await _session.TryGetAsync(BlumUrls.BALANCE);

            _logger.Debug((_accountName, ConsoleColor.DarkCyan), ($"GetBalanceAsync json: {response}", null));

            if (string.IsNullOrWhiteSpace(response))
            {
                await Task.Delay(2500);
                return (null, null, null, null, null);
            }

            var responseJson = JsonSerializer.Deserialize<BlumBalanceJson>(response);
            if (responseJson == null)
            {
                _logger.Warning("Failed to fetch balance: GET request returned null");
                return (null, null, null, null, null);
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

            bool? isFastFarmingEnabled = responseJson.IsFastFarmingEnabled;

            _logger.Debug((_accountName, ConsoleColor.DarkCyan),
                ("GetBalanceAsync data: ", null),
                ($"timeStamp: {timeStamp}", null),
                ($"playPasses: {playPasses}", null),
                ($"timeStart: {timeStart}", null),
                ($"timeEnd: {timeEnd}", null),
                ($"isFastFarmingEnabled: {responseJson.IsFastFarmingEnabled}", null)
                );

            return (timeStamp / 1000, timeStart / 1000, timeEnd / 1000, playPasses, isFastFarmingEnabled);
        }
    }
}
