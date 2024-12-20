﻿using Blum.Core;
using Blum.Exceptions;
using Blum.Models;
using Blum.Utilities;
using System.Text;
using static Blum.Utilities.RandomUtility.Random;

namespace Blum.Services
{
    internal class FarmingService
    {
        private static bool _initializedSW = false;
        private static string? _logFilePath;
        internal static readonly object _consoleLock = new object();
        private static StreamWriter? logFileStreamWriter;
        private static bool _disposed = false;

        private static readonly Logger logger = new(
            (message) =>
            {
                lock (_consoleLock)
                {
                    Console.Write(message);
                    logFileStreamWriter?.Write(message);
                }
            });

        private static void InitStreamWriter()
        {
            if (!_initializedSW)
            {
                _initializedSW = true;
                _logFilePath = AppFilepaths.LogsFilePath;
                AppFilepaths.EnsureDirectories(_logFilePath);
                File.Create(_logFilePath).Close();
                logFileStreamWriter = new StreamWriter(_logFilePath, append: false, encoding: Encoding.UTF8, bufferSize: 1024);
            }
        }

        internal static void OnProcessExit(object sender, EventArgs e)
        {
            lock (_consoleLock)
            {
                if (!_disposed && _initializedSW)
                {
                    logFileStreamWriter?.Flush();
                    logFileStreamWriter?.Dispose();
                    _disposed = true;
                }
            }
        }

        internal static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            OnProcessExit(sender, e);
            Environment.Exit(0);
        }

        public static async Task AutoStartBlumFarming()
        {
            await Program.PrintNewVersionIfAvailable();
            InitStreamWriter();

            try
            {
                AccountService accountManager = new();
                AccountService.Logger = logger;

                var accounts = accountManager.GetAccounts();
                int accountTotal = accounts.Accounts.Count;

                logger.Info($"Found {accountTotal} sessions.");

                accountManager.ProcessAccountsData(ref accounts);
                int validAccountsCount = accounts.Accounts.Count;

                var tasks = new List<Task>();

                foreach (var account in accounts.Accounts)
                {
                    tasks.Add(Task.Run(() => StartBlumFarming(account.Name, account.PhoneNumber)));
                }
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                logger.PrintAllExeceptionsData(ex.InnerException);
            }
        }

        public static async Task StartBlumFarming(string account, string phoneNumber)
        {
            logger.Info((account, ConsoleColor.DarkCyan), ("\u2665 Program is running, please be patient \u2665", null));

            bool exitFlag = false;
            bool playedGameIn8h = false;
            bool processedTasksIn8h = false;

            try
            {
                BlumBot? blumBot = null;

                while (!exitFlag)
                {
                    try
                    {
                        FakeWebClient fakeWebClient = new();
                        blumBot = new(fakeWebClient, account, phoneNumber, logger, debugMode: false);

                        int maxTries = 2;

                        await blumBot.LoginAsync();

                        while (!exitFlag)
                        {
                            try
                            {
                                await Task.Delay(RandomDelayMilliseconds(Delay.BeforeRequest));
                                var msg = await blumBot.ClaimDailyRewardAsync();
                                if (msg.Item1)
                                    logger.Info((account, ConsoleColor.DarkCyan), ($"Claimed daily reward! {msg.Item2}", null));

                                await Task.Delay(RandomDelayMilliseconds(Delay.BeforeRequest));

                                var (timestamp, startTime, endTime, playPasses, isFastFarmingEnabled) = await blumBot.GetBalanceAsync();

                                if (playPasses > 0 && TelegramSettings.MaxPlays > 0 && !playedGameIn8h)
                                {
                                    int usePasses = (playPasses ?? 0) > TelegramSettings.MaxPlays ? TelegramSettings.MaxPlays : (playPasses ?? 0);
                                    logger.Info((account, ConsoleColor.DarkCyan), ($"Starting the game. Passes to be used: {usePasses}.", null));

                                    await Task.Delay(RandomDelayMilliseconds(Delay.BeforeRequest));

                                    await blumBot.PlayGameAsync(usePasses);

                                    playedGameIn8h = true;
                                }

                                await Task.Delay(RandomDelayMilliseconds(3, 10));

                                try
                                {
                                    await Task.Delay(RandomDelayMilliseconds(Delay.BeforeRequest));

                                    timestamp = startTime = endTime = playPasses = null;
                                    (timestamp, startTime, endTime, playPasses, isFastFarmingEnabled) = await blumBot.GetBalanceAsync();

                                    if (startTime == null && endTime == null && maxTries > 0)
                                    {
                                        await Task.Delay(RandomDelayMilliseconds(Delay.BeforeRequest));

                                        if (await blumBot.StartFarmingAsync())
                                            logger.Success((account, ConsoleColor.DarkCyan), ($"Started farming!", null));
                                        else
                                            logger.Warning((account, ConsoleColor.DarkCyan), ($"Couldn't start farming!", null));

                                        maxTries--;
                                    }
                                    else if (startTime != null && endTime != null && timestamp != null && timestamp >= endTime && maxTries > 0)
                                    {
                                        await Task.Delay(RandomDelayMilliseconds(Delay.BeforeRequest));
                                        await blumBot.RefreshUsingTokenAsync();

                                        await Task.Delay(RandomDelayMilliseconds(Delay.BeforeRequest));

                                        var (claimTimestamp, balance) = await blumBot.ClaimFarmAsync();

                                        if (claimTimestamp == null || balance == null)
                                            logger.Warning((account, ConsoleColor.DarkCyan), ($"Seems that it is failed to claim the farm reward", null));
                                        else
                                            logger.Success((account, ConsoleColor.DarkCyan), ($"Claimed the farm reward! balance: {balance}", null));

                                        maxTries--;
                                    }

                                    else if (TelegramSettings.ShouldCompleteTasks && !processedTasksIn8h)
                                    {
                                        await blumBot.ProcessAndCompleteAvailableTasksAsync();
                                        processedTasksIn8h = true;
                                    }

                                    else if (endTime != null && timestamp != null)
                                    {
                                        long sleepTimeSeconds = endTime - timestamp ?? 0;
                                        TimeSpan sleepDuration = TimeSpan.FromSeconds(sleepTimeSeconds);
                                        string durationFormatted = string.Format("{0:D2} hours, {1:D2} minutes, {2:D2} seconds",
                                             sleepDuration.Hours,
                                             sleepDuration.Minutes,
                                             sleepDuration.Seconds);

                                        logger.Info((account, ConsoleColor.DarkCyan), ($"Sleep for {durationFormatted}.", null));

                                        lock (_consoleLock)
                                        {
                                            logFileStreamWriter?.FlushAsync().Wait();
                                        }

                                        maxTries++;

                                        int milliseconds = sleepTimeSeconds > int.MaxValue / 1000 ? int.MaxValue : (int)(sleepTimeSeconds * 1000);

                                        await Task.Delay(milliseconds);
                                        playedGameIn8h = false;
                                        processedTasksIn8h = false;

                                        if (!await blumBot.RefreshUsingTokenAsync())
                                        {
                                            logger.Error((account, ConsoleColor.DarkCyan), ("Failed to refresh, exiting thread...", null));
                                            return;
                                        }
                                    }
                                    else if (maxTries <= 0)
                                    {
                                        break;
                                    }
                                }
                                catch (BlumFatalError ex)
                                {
                                    logger.Error((account, ConsoleColor.DarkCyan), ($"Fatal error: \nStack Trace: {ex.StackTrace} \nMessage: {ex.Message}", null));
                                    logger.PrintAllExeceptionsData(ex.InnerException);
                                    exitFlag = true;
                                }
                                catch (Exception ex)
                                {
                                    logger.Error((account, ConsoleColor.DarkCyan), ($"Error in farming management: {ex.Message}", null));
                                    logger.PrintAllExeceptionsData(ex.InnerException);
                                }
                                finally
                                {
                                    lock (_consoleLock)
                                    {
                                        logFileStreamWriter?.FlushAsync().Wait();
                                    }
                                }
                                await Task.Delay(TimeSpan.FromSeconds(10));
                            }
                            catch (BlumFatalError ex)
                            {
                                logger.Error((account, ConsoleColor.DarkCyan), ($"Fatal error: \nStack Trace: {ex.StackTrace} \nMessage: {ex.Message}", null));
                                logger.PrintAllExeceptionsData(ex.InnerException);
                                exitFlag = true;
                            }
                            catch (Exception ex)
                            {
                                logger.Error((account, ConsoleColor.DarkCyan), ($"Error: {ex.Message}", null));
                                logger.PrintAllExeceptionsData(ex.InnerException);
                            }
                            finally
                            {
                                lock (_consoleLock)
                                {
                                    logFileStreamWriter?.FlushAsync().Wait();
                                }
                            }
                        }
                    }
                    catch (BlumFatalError ex)
                    {
                        logger.Error((account, ConsoleColor.DarkCyan), ($"Fatal error: \nStack Trace:\n {ex.StackTrace} \nMessage:\n {ex.Message}", null));
                        logger.PrintAllExeceptionsData(ex.InnerException);
                        exitFlag = true;
                    }
                    catch (Exception ex)
                    {
                        logger.Error((account, ConsoleColor.DarkCyan), ($"Session error:  {ex.Message}", null));
                        logger.PrintAllExeceptionsData(ex.InnerException);
                    }
                    finally
                    {
                        lock (_consoleLock)
                        {
                            logFileStreamWriter?.FlushAsync().Wait();
                        }
                        if (!exitFlag)
                        {
                            logger.Info((account, ConsoleColor.DarkCyan), ($"Reconnecting, 65 s", null));
                            blumBot?.Dispose();
                            blumBot = null;
                            await Task.Delay(TimeSpan.FromSeconds(65));
                        }
                    }
                }
            }
            catch (BlumFatalError ex)
            {
                logger.Error((account, ConsoleColor.DarkCyan), ($"Fatal error: \nStack Trace: {ex.StackTrace} \nMessage: {ex.Message}", null));
                logger.PrintAllExeceptionsData(ex.InnerException);
                exitFlag = true;
            }
            catch (Exception ex)
            {
                logger.Error((account, ConsoleColor.DarkCyan), ($"Error: {ex.Message}", null));
                logger.PrintAllExeceptionsData(ex.InnerException);
            }
            finally
            {
                lock (_consoleLock)
                {
                    logFileStreamWriter?.FlushAsync().Wait();
                }
            }
        }
    }
}