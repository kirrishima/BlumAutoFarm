using Blum.Core;
using Blum.Models;
using Blum.Services;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

internal class Program
{
    private static readonly string GitHubApiUrl = "https://api.github.com/repos/kirrishima/BlumAutoFarm/releases/latest";
    private static Version CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version;

    private static async Task Main(string[] args)
    {
        TelegramSettings.TryParseConfig(true);

        AppDomain.CurrentDomain.ProcessExit += new EventHandler(FarmingService.OnProcessExit);
        Console.CancelKeyPress += new ConsoleCancelEventHandler(FarmingService.OnCancelKeyPress);

        if (args.Length == 0)
        {
            await ArgumentParser.ParseArgs(["start-farm"]);
            return;
        }
        await ArgumentParser.ParseArgs(args);
    }

    public static async Task PrintNewVersionIfAvailable()
    {
        try
        {
            var releaseInfo = await GetLatestReleaseInfo();
            Version otherVersion = new Version(releaseInfo.TagName);

            if (releaseInfo != null)
            {
                if (CurrentVersion.CompareTo(otherVersion) < 0)
                {
                    Console.WriteLine($"\nNew version available: {releaseInfo.TagName}");
                    Console.WriteLine($"Changelog: {releaseInfo.Body}");
                    Console.WriteLine($"Link: {releaseInfo.HtmlUrl}\n");
                }
            }
        }
        catch { };
    }

    private static async Task<ReleaseInfo> GetLatestReleaseInfo()
    {
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("User-Agent", $"Blum {CurrentVersion}");

            var response = await client.GetStringAsync(GitHubApiUrl);

            var res = JsonSerializer.Deserialize<ReleaseInfo>(response);

            var versionMatch = Regex.Match(res.TagName, @"\d+(\.\d+){1,3}");

            if (versionMatch.Success)
            {
                res.TagName = versionMatch.Value;
            }
            return res;
        }
    }

    private class ReleaseInfo
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }
    }
}