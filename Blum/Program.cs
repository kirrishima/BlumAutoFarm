using Blum.Core;
using Blum.Models;

class Program
{
    static async Task Main(string[] args)
    {
        TelegramSettings.TryParseConfig(false);

        if (args.Length == 0)
        {
            await ArgumentParser.ParseArgs(["start-farm"]);
            return;
        }
        await ArgumentParser.ParseArgs(args);
    }
}