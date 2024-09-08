public static async Task StartBlumFarming(string account, string phoneNumber)
{
    // here where other code... this snippet is from 
    async Task Refreshing()
    {
        try
        {
            if (!await blumBot.RefreshUsingTokenAsync())
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                await blumBot.RefreshUsingTokenAsync();
            }
        }
        catch (BlumFatalError ex)
        {
            logger.Error((account, ConsoleColor.DarkCyan), ($"Fatal error: {ex.StackTrace} | {ex.Message}", null));
            logger.PrintAllExeceptionsData(ex.InnerException);
            exitFlag = true;
        }
    };

    var cts = new CancellationTokenSource();
    Task refreshingLoopTask = RefreshConnection(Refreshing, cts.Token);

    await Task.Delay(milliseconds);

    cts.Cancel();
    await refreshingLoopTask;
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