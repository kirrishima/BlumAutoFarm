using Blum.Core;
using Blum.Models;
using Blum.Utilities;

class Program
{
    private static Logger logger = new();
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