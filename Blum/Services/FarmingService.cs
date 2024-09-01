using System.Text.Json;
using Blum.Core;
using Blum.Models;
using Blum.Utilities;
using static Blum.Utilities.RandomUtility.Random;

namespace Blum.Services
{
    internal class FarmingService
    {
        private static readonly Logger logger = new();
        public static int maxPlays = 7;

        public static async Task AutoStartBlumFarming()
        {
            try
            {
                var aes = new Encryption(TelegramSettings.ApiHash);
                AccountService accountManager = new(aes);

                var accounts = accountManager.GetAccounts();
                int accountTotal = accounts.Accounts.Count;

                AccountService.ValidateAccountsData(ref accounts);

                int validAccountsCount = accounts.Accounts.Count;

                logger.Info($"Found {accountTotal} sessions. {(accountTotal > 0 ? $"Valid: {validAccountsCount}" : "")}");
                var tasks = new List<Task>();

                foreach (var account in accounts.Accounts)
                {
                    tasks.Add(Task.Run(() => StartBlumFarming(account.SessionName, account.PhoneNumber)));
                }
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                if (ex.InnerException != null)
                {
                    logger.Error(ex.InnerException.Message);
                }
            }
        }

        public static async Task StartBlumFarming(string account, string phoneNumber)
        {
            logger.Info((account, ConsoleColor.DarkCyan), ("\u2665 Program is running, please be patient \u2665", null));
            try
            {
                RandomUtility.Random random = new();
                FakeWebClient fakeWebClient = new();

                while (true)
                {
                    try
                    {
                        BlumBot blumBot = new(fakeWebClient, account, phoneNumber, debugMode: false);

                        int maxTries = 2;
                        bool wasPrintedClaimInfo = false;

                        await Task.Delay(RandomUtility.Random.RandomDelayMilliseconds(RandomUtility.Random.Delay.Account));

                        await blumBot.LoginAsync();

                        while (true)
                        {
                            try
                            {
                                var msg = await blumBot.ClaimDailyRewardAsync();
                                if (msg.Item1)
                                {
                                    logger.Info((account, ConsoleColor.DarkCyan), ("Claimed daily reward!", null));
                                    wasPrintedClaimInfo = true;
                                }
                                else if (!wasPrintedClaimInfo)
                                {
                                    logger.Info((account, ConsoleColor.DarkCyan), ("Daily reward already claimed!", null));
                                    wasPrintedClaimInfo = true;
                                }

                                var (timestamp, startTime, endTime, playPasses) = await blumBot.GetBalanceAsync();

                                if (playPasses > 0)
                                {
                                    logger.Info((account, ConsoleColor.DarkCyan), ($"Starting play game! Play passes: {playPasses ?? 0}. Limit: {maxPlays} per 8h", null));
                                    await blumBot.PlayGameAsync((playPasses ?? 0) > maxPlays ? maxPlays : (playPasses ?? 0));
                                }

                                await Task.Delay(RandomDelayMilliseconds(3, 10));

                                try
                                {
                                    timestamp = startTime = endTime = playPasses = null;

                                    (timestamp, startTime, endTime, playPasses) = await blumBot.GetBalanceAsync();
                                    if (startTime == null && endTime == null && maxTries > 0)
                                    {
                                        if (await blumBot.StartFarmingAsync())
                                            logger.Info((account, ConsoleColor.DarkCyan), ($"Started farming!", null));
                                        else
                                            logger.Warning((account, ConsoleColor.DarkCyan), ($"Couldn't start farming for unknown reason!", null));
                                        maxTries--;
                                    }
                                    else if (startTime != null && endTime != null && timestamp != null && timestamp >= endTime && maxTries > 0)
                                    {
                                        await blumBot.RefreshUsingTokenAsync();
                                        (timestamp, object? balance) = await blumBot.ClaimFarmAsync();

                                        string? strBalance = null;
                                        {
                                            if (strBalance is string str)
                                                strBalance = str;
                                            else if (balance is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.String)
                                                strBalance = jsonElement.GetString();
                                        }
                                        if (timestamp is null || balance is null)
                                        {
                                            logger.Warning((account, ConsoleColor.DarkCyan), ($"Seems that it's failed to claim the farm reward", null));
                                        }
                                        maxTries--;
                                    }
                                    else if (endTime != null && timestamp != null)
                                    {
                                        long sleepTimeSeconds = endTime - timestamp ?? 0;
                                        TimeSpan sleepDuration = TimeSpan.FromSeconds(sleepTimeSeconds);
                                        string durationFormatted = string.Format("{0:D2} hours, {1:D2} minutes, {2:D2} seconds",
                                             sleepDuration.Hours,
                                             sleepDuration.Minutes,
                                             sleepDuration.Seconds);

                                        DateTime now = DateTime.Now;
                                        DateTime futureTime = now.Add(sleepDuration);
                                        string futureTimeFormatted = futureTime.ToString("MM.dd, dddd, HH:mm:ss");
                                        logger.Info((account, ConsoleColor.DarkCyan), ($"Sleep for {durationFormatted}. Ends at {futureTimeFormatted}", null));
                                        maxTries++;

                                        int milliseconds = sleepTimeSeconds > int.MaxValue / 1000 ? int.MaxValue : (int)(sleepTimeSeconds * 1000);

                                        async Task Refreshing() => await blumBot.RefreshUsingTokenAsync();

                                        var cts = new CancellationTokenSource();
                                        Task refreshingLoopTask = RefreshConnection(Refreshing, cts.Token);

                                        await Task.Delay(milliseconds);
                                        cts.Cancel();
                                        await refreshingLoopTask;

                                        wasPrintedClaimInfo = false;
                                        await blumBot.RefreshUsingTokenAsync();
                                    }
                                    else if (maxTries <= 0)
                                    {
                                        break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    logger.Error((account, ConsoleColor.DarkCyan), ($"Error in farming management: {ex.Message}", null));
                                    if (ex.InnerException != null)
                                    {
                                        logger.Error((account, ConsoleColor.DarkCyan), ($"Error in farming management: {ex.InnerException.Message}", null));
                                    }
                                }
                                await Task.Delay(10000);
                            }
                            catch (Exception ex)
                            {
                                logger.Error((account, ConsoleColor.DarkCyan), ($"Error: {ex.Message}", null));
                                if (ex.InnerException != null)
                                {
                                    logger.Error((account, ConsoleColor.DarkCyan), ($"Error: {ex.InnerException.Message}", null));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error((account, ConsoleColor.DarkCyan), ($"Session error:  {ex.Message}", null));
                        if (ex.InnerException != null)
                        {
                            logger.Error((account, ConsoleColor.DarkCyan), ($"Session error: {ex.InnerException.Message}", null));
                        }
                    }
                    finally
                    {
                        logger.Info((account, ConsoleColor.DarkCyan), ($"Reconnecting, 65 s", null));
                        await Task.Delay(65000);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error((account, ConsoleColor.DarkCyan), ($"Error: {ex.Message}", null));
                if (ex.InnerException != null)
                {
                    logger.Error((account, ConsoleColor.DarkCyan), ($"Error: {ex.InnerException.Message}", null));
                }
            }
        }

        private static async Task RefreshConnection(Func<Task> func, CancellationToken token)
        {
            try
            {
                while (true)
                {
                    if (token.IsCancellationRequested)
                        break;
                    else
                    {
                        int x = RandomDelayMilliseconds(TimeSpan.FromMinutes(25), TimeSpan.FromMinutes(35));
                        //string durationFormatted = string.Format("{0:D2} minutes, {1:D2} seconds",
                        //TimeSpan.FromMilliseconds(x).Minutes,
                        //TimeSpan.FromMilliseconds(x).Seconds);
                        //Console.WriteLine($"Before resfreh: {durationFormatted}");
                        await Task.Delay(x, token);
                    }
                    await func();
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                logger.Error($"Unexpected error happened: {ex.Message}");
            }
        }
    }
}
