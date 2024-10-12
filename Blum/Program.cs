using Blum.Core;
using Blum.Models;
using System.Text.Json.Serialization;
using System.Text.Json;

class Program
{
    private static readonly string GitHubApiUrl = "https://api.github.com/repos/kirrishima/BlumAutoFarm/releases/latest";
    private static readonly string CurrentVersion = "v1.0.0"; // Замените на текущую версию вашего приложения

    static async Task Main(string[] args)
    {
        TelegramSettings.TryParseConfig(false);

        try
        {
            var releaseInfo = await GetLatestReleaseInfo();
            if (releaseInfo != null && releaseInfo.TagName != CurrentVersion)
            {
                Console.WriteLine($"\nNew version available: {releaseInfo.TagName}");
                Console.WriteLine($"Changelog: {releaseInfo.Body}");
                Console.WriteLine($"Link: {releaseInfo.HtmlUrl}\n");
            }
            else
            {
                Console.WriteLine("The latest version is installed.");
            }
        }
        catch { };

        if (args.Length == 0)
        {
            await ArgumentParser.ParseArgs(["start-farm"]);
            return;
        }
        await ArgumentParser.ParseArgs(args);
    }

    private static async Task<ReleaseInfo> GetLatestReleaseInfo()
    {
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("User-Agent", $"Blum {CurrentVersion}");

            var response = await client.GetStringAsync(GitHubApiUrl);
            return JsonSerializer.Deserialize<ReleaseInfo>(response);
        }
    }

    private class ReleaseInfo
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; }

        [JsonPropertyName("body")]
        public string Body { get; set; }

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; }
    }
}
