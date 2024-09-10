using System.Text.Json;
using System.Web;

namespace Blum.Core
{
    partial class BlumBot
    {
        private static string? ValidateGameId(object? gameId)
        {
            if (gameId is null)
                return null;

            if (gameId is string str)
                return str;
            else if (gameId is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.String)
                return jsonElement.GetString();
            else
                return null;
        }

        private static string SplitAndProcessURL(string URL)
        {
            string[] parts = URL.Split(new[] { "tgWebAppData=" }, StringSplitOptions.None);
            if (parts.Length > 1)
            {
                string dataPart = parts[1].Split(new[] { "&tgWebAppVersion" }, StringSplitOptions.None)[0];
                string decodedData = HttpUtility.UrlDecode(HttpUtility.UrlDecode(URL.Split("tgWebAppData=")[1].Split("&tgWebAppVersion")[0]));
                return decodedData;
            }
            return URL;
        }
    }
}
